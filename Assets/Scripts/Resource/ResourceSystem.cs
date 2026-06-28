using System;
using UnityEngine;

namespace DPTB.Resource
{
    /// <summary>
    /// 资源系统：电能 / 氧气 / 精神 的单一数据源（Single Source of Truth）。
    /// - 时间驱动衰减（氧气低 → 精神加速下降）。
    /// - <see cref="TrySpendPower"/> 统一耗电入口，所有设备走此 API。
    /// - 数值变化通过事件广播（Observer），HUD/字幕/特效订阅，避免轮询耦合。
    /// 设计：观察者模式（Observer）+ 轻量单例（Service Locator）。
    /// </summary>
    public class ResourceSystem : MonoBehaviour
    {
        public static ResourceSystem Instance { get; private set; }

        [SerializeField] private ResourceConfig config;

        // 事件参数均为归一化值 0..1，便于 UI/特效直接使用
        public event Action<float> PowerChanged;
        public event Action<float> OxygenChanged;
        public event Action<float> MentalChanged;
        /// <summary>电量不足以支付一次消耗时触发（供 UI 提示「电量不足」）。</summary>
        public event Action PowerSpendFailed;

        private float _power, _oxygen, _mental;
        private bool _decayActive = true;

        public float Power => _power;
        public float Oxygen => _oxygen;
        public float Mental => _mental;
        public float Max => config != null ? config.maxValue : 100f;
        public ResourceConfig Config => config;

        public float PowerNormalized => _power / Max;
        public float OxygenNormalized => _oxygen / Max;
        public float MentalNormalized => _mental / Max;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            ResetToInitial();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>重置为配置初始值。</summary>
        public void ResetToInitial()
        {
            if (config == null) { Debug.LogError("ResourceSystem: 未指定 ResourceConfig", this); return; }
            _power = config.initialPower;
            _oxygen = config.initialOxygen;
            _mental = config.initialMental;
            RaiseAll();
        }

        private void Update()
        {
            if (!_decayActive || config == null) return;
            float dt = Time.deltaTime;

            // 氧气随时间下降
            if (_oxygen > 0f) SetOxygen(_oxygen - config.oxygenDecayPerSecond * dt);

            // 精神随时间下降；氧气过低时加速（耦合规则）
            float mentalRate = config.mentalDecayPerSecond;
            if (_oxygen <= config.lowOxygenThreshold)
                mentalRate *= config.lowOxygenMentalMultiplier;
            if (_mental > 0f) SetMental(_mental - mentalRate * dt);
        }

        // ── 耗电 API（所有设备统一入口）──────────────────────────

        /// <summary>尝试消耗电能。够则扣除返回 true；不够则触发 PowerSpendFailed 返回 false。</summary>
        public bool TrySpendPower(float cost)
        {
            if (_power < cost) { PowerSpendFailed?.Invoke(); return false; }
            SetPower(_power - cost);
            return true;
        }

        /// <summary>制氧机：恢复氧气。</summary>
        public void RestoreOxygen()
        {
            if (config != null) SetOxygen(_oxygen + config.oxygenRestoreAmount);
        }

        /// <summary>电击器：恢复精神。</summary>
        public void RestoreMental()
        {
            if (config != null) SetMental(_mental + config.mentalRestoreAmount);
        }

        // ── 状态控制（供 L4 状态机调用）──────────────────────────

        /// <summary>开关时间衰减。失败/成功态可停衰减。</summary>
        public void SetDecayActive(bool active) => _decayActive = active;

        /// <summary>失败态：精神拉满（玩家可安心读日记）。</summary>
        public void RecoverMentalFull() => SetMental(Max);

        // ── 内部 setter：clamp + 仅在变化时广播 ─────────────────

        private void SetPower(float v)
        {
            v = Mathf.Clamp(v, 0f, Max);
            if (Mathf.Approximately(v, _power)) return;
            _power = v;
            PowerChanged?.Invoke(PowerNormalized);
        }

        private void SetOxygen(float v)
        {
            v = Mathf.Clamp(v, 0f, Max);
            if (Mathf.Approximately(v, _oxygen)) return;
            _oxygen = v;
            OxygenChanged?.Invoke(OxygenNormalized);
        }

        private void SetMental(float v)
        {
            v = Mathf.Clamp(v, 0f, Max);
            if (Mathf.Approximately(v, _mental)) return;
            _mental = v;
            MentalChanged?.Invoke(MentalNormalized);
        }

        private void RaiseAll()
        {
            PowerChanged?.Invoke(PowerNormalized);
            OxygenChanged?.Invoke(OxygenNormalized);
            MentalChanged?.Invoke(MentalNormalized);
        }
    }
}
