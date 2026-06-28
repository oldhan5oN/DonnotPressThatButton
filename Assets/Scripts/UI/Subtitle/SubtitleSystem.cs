using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace DPTB.UI
{
    /// <summary>
    /// 字幕播放器（播放层）：单条居中浮现 —— 打字机逐字显示、停留、淡出。
    /// 对外只有 Show / Clear，不懂电量/精神等业务（单一职责 SRP）。
    /// 队列按优先级排序；高优先级字幕可打断正在播放的低优先字幕。
    /// 设计：轻量单例（Service Locator）+ 协程打字机 + 优先级队列。
    /// </summary>
    public class SubtitleSystem : MonoBehaviour
    {
        public static SubtitleSystem Instance { get; private set; }

        [Header("视图")]
        [SerializeField, Tooltip("字幕文本（TMP）")] private TMP_Text label;
        [SerializeField, Tooltip("用于淡入淡出的 CanvasGroup")] private CanvasGroup group;

        [Header("数据")]
        [SerializeField, Tooltip("字幕总表资产")] private SubtitleTable table;

        [Header("呈现")]
        [SerializeField, Min(0f), Tooltip("淡入时长（秒）")] private float fadeIn = 0.2f;
        [SerializeField, Min(0f), Tooltip("淡出时长（秒）")] private float fadeOut = 0.4f;
        [SerializeField, Tooltip("中文标点处停顿的倍数（让语气更自然）")]
        private float punctuationPause = 3f;
        [SerializeField, Min(0f), Tooltip("同一条字幕内，多句之间清屏停顿（秒）")]
        private float betweenSentencesGap = 0.3f;
        [SerializeField, Min(0f), Tooltip("交互提示显示多久后自动消失（秒）；0=常驻不消失")]
        private float promptDuration = 2.5f;

        // 优先级队列：靠前的优先播
        private readonly List<SubtitleLine> _queue = new List<SubtitleLine>();
        private readonly HashSet<SubtitleId> _played = new HashSet<SubtitleId>();
        private readonly StringBuilder _sb = new StringBuilder();
        private const string Punctuation = "，。、；：！？…—,.!?";

        private Coroutine _routine;
        private bool _playing;
        private int _currentPriority;
        private string _promptText = "";   // 常驻交互提示（低优先，字幕占用时让位）
        private Coroutine _promptTimer;     // 提示自动消失计时

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (group != null) group.alpha = 0f;
            if (label != null) label.text = "";
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── 对外 API ──────────────────────────────────────────────

        /// <summary>按主键播放字幕（从总表取行）。</summary>
        public void Show(SubtitleId id)
        {
            if (table == null) { Debug.LogWarning("SubtitleSystem：未指定 SubtitleTable", this); return; }
            Show(table.Get(id));
        }

        /// <summary>播放一条字幕（直接给数据）。</summary>
        public void Show(SubtitleLine line)
        {
            if (line == null || line.texts == null || line.texts.Count == 0) return;
            if (line.playOnce && _played.Contains(line.id)) return;
            if (line.playOnce) _played.Add(line.id);

            // 正在播且新条优先级更高 → 打断，立即插到队首
            if (_playing && line.priority > _currentPriority)
            {
                if (_routine != null) StopCoroutine(_routine);
                _playing = false;
                _queue.Insert(0, line);
                StartNext();
                return;
            }

            InsertByPriority(line);
            if (!_playing) StartNext();
        }

        /// <summary>清空当前与排队字幕，恢复交互提示（若有）。</summary>
        public void Clear()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _queue.Clear();
            _playing = false;
            ApplyPrompt(); // 字幕清空，回到交互提示态
        }

        /// <summary>重置「只播一次」记录（新周目调用）。</summary>
        public void ResetPlayedOnce() => _played.Clear();

        // ── 交互提示通道（与字幕共用同一个 text，字幕优先）─────────

        /// <summary>是否正被字幕占用（正在播或仍有排队）。</summary>
        public bool IsBusy => _playing || _queue.Count > 0;

        /// <summary>登记常驻交互提示。字幕占用时仅缓存、不显示；空闲时立即显示。</summary>
        public void SetPrompt(string text)
        {
            _promptText = text ?? "";
            if (!_playing) ApplyPrompt();
        }

        /// <summary>清除交互提示。</summary>
        public void ClearPrompt()
        {
            _promptText = "";
            if (!_playing) ApplyPrompt();
        }

        // 把 label 设为当前提示文本（仅在字幕空闲时调用）
        private void ApplyPrompt()
        {
            StopPromptTimer();

            bool has = !string.IsNullOrEmpty(_promptText);
            if (label != null) label.text = has ? _promptText : "";
            if (group != null) group.alpha = has ? 1f : 0f;

            // 显示后开始计时，到点自动消失（promptDuration<=0 则常驻）
            if (has && promptDuration > 0f)
                _promptTimer = StartCoroutine(PromptHideAfter(promptDuration));
        }

        private IEnumerator PromptHideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            yield return Fade(0f, fadeOut);
            if (label != null) label.text = "";
            _promptText = "";   // 已消费：避免字幕播完后又重现
            _promptTimer = null;
        }

        private void StopPromptTimer()
        {
            if (_promptTimer != null) StopCoroutine(_promptTimer);
            _promptTimer = null;
        }

        // ── 内部 ─────────────────────────────────────────────────

        private void InsertByPriority(SubtitleLine line)
        {
            int i = 0;
            while (i < _queue.Count && _queue[i].priority >= line.priority) i++;
            _queue.Insert(i, line);
        }

        private void StartNext()
        {
            if (_queue.Count == 0)
            {
                _playing = false;
                ApplyPrompt(); // 字幕播完，恢复当前仍有效的交互提示（若有）
                return;
            }
            var line = _queue[0];
            _queue.RemoveAt(0);
            StopPromptTimer(); // 字幕接管显示，停掉提示计时
            _routine = StartCoroutine(PlayRoutine(line));
        }

        private IEnumerator PlayRoutine(SubtitleLine line)
        {
            _playing = true;
            _currentPriority = line.priority;

            if (label != null) label.text = "";
            yield return Fade(1f, fadeIn);

            // 逐句播放（一段话可分多句，整段作为原子单元，中途不被打断）
            var texts = line.texts;
            for (int s = 0; s < texts.Count; s++)
            {
                string text = texts[s];
                if (string.IsNullOrEmpty(text)) continue;

                // 句间清屏停顿（第一句前 label 已空，跳过）
                if (s > 0)
                {
                    if (label != null) label.text = "";
                    if (betweenSentencesGap > 0f) yield return new WaitForSeconds(betweenSentencesGap);
                }

                // 打字机逐字
                _sb.Clear();
                for (int i = 0; i < text.Length; i++)
                {
                    _sb.Append(text[i]);
                    if (label != null) label.text = _sb.ToString();

                    float wait = line.charInterval;
                    if (Punctuation.IndexOf(text[i]) >= 0) wait *= punctuationPause;
                    if (wait > 0f) yield return new WaitForSeconds(wait);
                }

                if (line.holdTime > 0f) yield return new WaitForSeconds(line.holdTime);
            }

            yield return Fade(0f, fadeOut);

            _routine = null;
            StartNext();
        }

        private IEnumerator Fade(float target, float duration)
        {
            if (group == null) yield break;
            float start = group.alpha;
            if (duration <= 0f) { group.alpha = target; yield break; }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
