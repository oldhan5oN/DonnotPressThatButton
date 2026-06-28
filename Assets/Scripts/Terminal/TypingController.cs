using System;
using System.Collections.Generic;
using UnityEngine;
using DPTB.Game;

namespace DPTB.Terminal
{
    /// <summary>
    /// 打字控制器（MVC 的 Controller）：接收键盘点击，按首字母映射校验。
    /// 每次按键推进一位：正确则亮起，错按则标 *（不阻断，继续往下打）；
    /// 自动跳过无需输入位；按下短路键 → 触发失败结局。
    ///
    /// 支持多段（多个 <see cref="TerminalData"/>）：按列表顺序逐段输入，
    /// 一段打完自动进入下一段。完成段数达到 <see cref="requiredCompletedCount"/>
    /// 即可广播（不必打完全部）。各段进度保存在控制器内，玩家中途离开再回来续打。
    ///
    /// 设计：MVC 风格 + 备忘录式进度持久化（会话间不重建状态）+ 迭代器式逐段推进 +
    /// 数据/阈值驱动（完成几段可广播由配置决定）。
    /// </summary>
    public class TypingController : MonoBehaviour
    {
        [Header("数据（按顺序逐段打字）")]
        [SerializeField, Tooltip("依次要输入的广播文本段；打完一段进入下一段")]
        private List<TerminalData> dataList = new List<TerminalData>();
        [SerializeField, Tooltip("完成多少段即可广播（默认 2，不必打完全部）")]
        private int requiredCompletedCount = 2;

        [Header("视图")]
        [SerializeField] private TerminalScreen screen;
        [SerializeField] private VirtualKeyboard keyboard;

        /// <summary>达到广播阈值时触发一次。</summary>
        public event Action Completed;
        /// <summary>每完成一段触发（参数：已完成段数）。</summary>
        public event Action<int> SegmentCompleted;

        /// <summary>已完成段数是否达到广播阈值。</summary>
        public bool CanBroadcast => CompletedCount >= requiredCompletedCount;
        /// <summary>是否所有段都已打完。</summary>
        public bool AllCompleted => _dataIndex >= _segments.Count;
        /// <summary>已完成段数。</summary>
        public int CompletedCount { get; private set; }
        /// <summary>是否存在错按位（供广播按钮按需校验，可选）。</summary>
        public bool HasErrors { get; private set; }

        /// <summary>单段进度（汉字序列 / 目标字母 / 各位状态 / 当前位 / 是否完成）。</summary>
        private class Segment
        {
            public readonly List<string> glyphs = new List<string>();
            public readonly List<char> targets = new List<char>(); // '\0' = 自动显示
            public readonly List<CharState> states = new List<CharState>();
            public int current;
            public bool completed;
        }

        private readonly List<Segment> _segments = new List<Segment>();
        private int _dataIndex;       // 当前正在输入的段
        private bool _built;          // 是否已构建（保证进度只初始化一次）
        private bool _broadcastRaised; // Completed 事件只发一次

        private Segment Cur => (_dataIndex >= 0 && _dataIndex < _segments.Count) ? _segments[_dataIndex] : null;

        private void OnEnable()
        {
            if (keyboard != null) keyboard.KeyPressed += OnKey;
        }

        private void OnDisable()
        {
            if (keyboard != null) keyboard.KeyPressed -= OnKey;
        }

        /// <summary>开启一次打字会话（终端面板打开时调用）。首次构建，之后恢复进度。</summary>
        public void StartSession()
        {
            if (!_built) BuildAll();
            if (keyboard != null) keyboard.BeginSession();
            Render(); // 恢复上次进度的显示
        }

        /// <summary>结束会话（关闭终端面板时调用）。仅停短路推进，进度保留。</summary>
        public void EndSession()
        {
            if (keyboard != null) keyboard.EndSession();
        }

        /// <summary>彻底重置打字进度（新周目时调用，可选）。</summary>
        public void ResetProgress()
        {
            _built = false;
            _segments.Clear();
            CompletedCount = 0;
            HasErrors = false;
            _dataIndex = 0;
            _broadcastRaised = false;
        }

        private void BuildAll()
        {
            _segments.Clear();
            CompletedCount = 0;
            HasErrors = false;
            _dataIndex = 0;
            _broadcastRaised = false;
            _built = true;

            if (dataList == null || dataList.Count == 0)
            {
                Debug.LogError("TypingController：未指定任何 TerminalData", this);
                return;
            }

            foreach (var d in dataList)
            {
                if (d == null) continue;
                _segments.Add(BuildSegment(d));
            }

            // 进入首段：跳过开头自动位 / 空段
            var first = Cur;
            if (first != null) AdvanceWithinSegment(first);
        }

        private Segment BuildSegment(TerminalData data)
        {
            var seg = new Segment();
            string text = data.broadcastText ?? string.Empty;
            string letters = data.targetLetters ?? string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                char glyph = text[i];
                char letter = i < letters.Length ? letters[i] : ' ';
                bool auto = char.IsWhiteSpace(glyph) || letter == ' ' || letter == '\0';

                seg.glyphs.Add(glyph.ToString());
                seg.targets.Add(auto ? '\0' : char.ToUpperInvariant(letter));
                seg.states.Add(CharState.Pending);
            }

            if (text.Length != letters.Length)
                Debug.LogWarning($"TerminalData「{data.name}」：文本({text.Length})与字母({letters.Length})长度不一致，超出位按自动处理", this);

            return seg;
        }

        private void OnKey(char c)
        {
            if (AllCompleted) return;

            // 短路键：唯一会中断输入的情况 —— 即按即失败
            if (keyboard != null && keyboard.IsShortCircuited(c))
            {
                Fail();
                return;
            }

            var seg = Cur;
            if (seg == null || seg.current >= seg.glyphs.Count) return;

            // 正确则亮起，错按则标 *；无论对错都推进一位（不阻断）
            if (char.ToUpperInvariant(c) == seg.targets[seg.current])
            {
                seg.states[seg.current] = CharState.Correct;
            }
            else
            {
                seg.states[seg.current] = CharState.Wrong;
                HasErrors = true;
            }
            seg.current++;
            AdvanceWithinSegment(seg);
            Render();
        }

        /// <summary>跳过段内自动位；若该段打完则结算并进入下一段（链式跳过空段）。</summary>
        private void AdvanceWithinSegment(Segment seg)
        {
            while (seg.current < seg.glyphs.Count && seg.targets[seg.current] == '\0')
            {
                seg.states[seg.current] = CharState.Correct;
                seg.current++;
            }
            if (seg.current >= seg.glyphs.Count)
                CompleteSegment(seg);
        }

        private void CompleteSegment(Segment seg)
        {
            if (seg.completed) return;
            seg.completed = true;
            CompletedCount++;
            SegmentCompleted?.Invoke(CompletedCount);
            Debug.Log($"终端：完成第 {CompletedCount}/{_segments.Count} 段", this);

            if (!_broadcastRaised && CanBroadcast)
            {
                _broadcastRaised = true;
                Completed?.Invoke();
                Debug.Log($"终端：已达广播条件（完成 {CompletedCount} 段），可前往广播柱", this);
            }

            // 推进到下一段，并跳过其开头自动位 / 空段
            _dataIndex++;
            var next = Cur;
            if (next != null) AdvanceWithinSegment(next);
        }

        private void Fail()
        {
            Debug.Log("终端：按下短路键，系统毁坏！", this);
            EndSession();
            GameManager.Instance?.ReportShortCircuit();
        }

        private void Render()
        {
            if (screen == null) return;
            // 正在输入则显示当前段；全部完成则定格最后一段
            Segment seg = Cur ?? (_segments.Count > 0 ? _segments[_segments.Count - 1] : null);
            if (seg == null) return;
            screen.Render(seg.glyphs, seg.states, seg.current);
        }
    }
}
