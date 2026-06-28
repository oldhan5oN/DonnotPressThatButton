using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DPTB.Terminal
{
    /// <summary>
    /// 虚拟键盘（视图）：绑定你按背景图手摆好的按键（每个挂 <see cref="KeyboardKey"/>）。
    /// 接管点击发出 <see cref="KeyPressed"/>；会话进行中随时间把若干输入键标记为短路（染色），
    /// 按下短路键由控制器判失败。
    /// 设计：MVC 的 View —— 只产出输入事件与短路呈现，不做校验。布局完全由摆放决定。
    /// </summary>
    public class VirtualKeyboard : MonoBehaviour
    {
        [Header("短路（随时间）")]
        [SerializeField] private float firstShortCircuitDelay = 20f;
        [SerializeField] private float shortCircuitInterval = 15f;
        [SerializeField] private int maxShortCircuitKeys = 5;

        public event Action<char> KeyPressed;

        // char → 输入键（仅参与打字/短路的字母键）
        private readonly Dictionary<char, KeyboardKey> _inputKeys = new Dictionary<char, KeyboardKey>();
        private readonly HashSet<char> _short = new HashSet<char>();

        private bool _active;
        private float _timer;

        private void Awake() => CollectKeys();

        private void CollectKeys()
        {
            _inputKeys.Clear();

            var keys = GetComponentsInChildren<KeyboardKey>(true);
            foreach (var k in keys)
            {
                char c = k.Letter; // 每次迭代独立变量，闭包捕获安全
                if (k.Button != null)
                {
                    if (k.IsInputKey)
                        k.Button.onClick.AddListener(() => KeyPressed?.Invoke(c));
                    // 非输入键（空格/功能键）不发打字事件
                }

                if (k.IsInputKey && !_inputKeys.ContainsKey(c))
                {
                    _inputKeys[c] = k;
                    k.ResetVisual();
                }
            }

            if (_inputKeys.Count == 0)
                Debug.LogWarning("VirtualKeyboard：未找到任何输入键（子物体需挂 KeyboardKey 并填字母）", this);
        }

        /// <summary>开始一次打字会话：重置短路、启动计时。</summary>
        public void BeginSession()
        {
            ResetShorts();
            _active = true;
            _timer = firstShortCircuitDelay;
        }

        /// <summary>结束会话：停止短路推进。</summary>
        public void EndSession() => _active = false;

        public bool IsShortCircuited(char c) => _short.Contains(char.ToUpperInvariant(c));

        private void Update()
        {
            if (!_active || _short.Count >= maxShortCircuitKeys) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                AddRandomShortCircuit();
                _timer = shortCircuitInterval;
            }
        }

        private void AddRandomShortCircuit()
        {
            var candidates = new List<char>();
            foreach (var kv in _inputKeys)
                if (!_short.Contains(kv.Key)) candidates.Add(kv.Key);
            if (candidates.Count == 0) return;

            char picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            _short.Add(picked);
            _inputKeys[picked].SetShortCircuited(true);
        }

        private void ResetShorts()
        {
            _short.Clear();
            foreach (var kv in _inputKeys) kv.Value.ResetVisual();
        }
    }
}
