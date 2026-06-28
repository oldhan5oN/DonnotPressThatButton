using System;
using System.Collections.Generic;
using UnityEngine;
using DPTB.Game;
using DPTB.Resource;
using DPTB.Terminal;

namespace DPTB.UI
{
    /// <summary>
    /// 字幕触发器（触发层 / 软引导的大脑）：订阅资源与游戏状态事件，
    /// 按 Inspector 配置的规则把对应字幕推给 <see cref="SubtitleSystem"/>。
    /// 规则数据驱动，新增/调整引导不动播放器（观察者 Observer + 开闭原则 OCP）。
    /// </summary>
    public class SubtitleTrigger : MonoBehaviour
    {
        private enum ResourceKind { Power, Oxygen, Mental }

        /// <summary>资源跌破阈值时触发一条字幕（带滞回，避免在阈值附近反复刷屏）。</summary>
        [Serializable]
        private class ThresholdRule
        {
            public ResourceKind kind = ResourceKind.Mental;
            [Range(0f, 1f), Tooltip("归一化值跌到此以下触发")]
            public float belowNormalized = 0.3f;
            public SubtitleId id = SubtitleId.None;
            [NonSerialized] public bool armed = true; // 是否处于可触发状态
        }

        /// <summary>进入某游戏阶段时触发一条字幕。</summary>
        [Serializable]
        private class PhaseRule
        {
            public GamePhase phase = GamePhase.Failed;
            public SubtitleId id = SubtitleId.None;
        }

        /// <summary>到达某结局时触发一条字幕（按结局类型精确区分，弥补 phase=Over 的歧义）。</summary>
        [Serializable]
        private class EndingRule
        {
            public EndingType ending = EndingType.BroadcastSuccess;
            public SubtitleId id = SubtitleId.None;
        }

        [Header("开场")]
        [SerializeField, Tooltip("游戏开始时播放的字幕；None 则不播")]
        private SubtitleId introOnStart = SubtitleId.Intro;

        [Header("资源阈值规则")]
        [SerializeField, Tooltip("重新武装所需的回升余量（滞回），防止在阈值抖动时反复触发")]
        private float rearmHysteresis = 0.05f;
        [SerializeField] private List<ThresholdRule> thresholdRules = new List<ThresholdRule>();

        [Header("阶段规则")]
        [SerializeField] private List<PhaseRule> phaseRules = new List<PhaseRule>();

        [Header("结局规则（按结局类型，区分广播成功/自杀）")]
        [SerializeField] private List<EndingRule> endingRules = new List<EndingRule>();

        [Header("终端")]
        [SerializeField, Tooltip("电脑终端打字控制器；达到广播条件(完成所需段数)时播字幕。留空则不接")]
        private TypingController typing;
        [SerializeField, Tooltip("终端解锁广播时播放的字幕；None 则不播")]
        private SubtitleId onBroadcastReady = SubtitleId.BroadcastDone;

        private ResourceSystem _res;
        private GameManager _game;

        private void Start()
        {
            _res = ResourceSystem.Instance;
            _game = GameManager.Instance;

            if (_res != null)
            {
                _res.PowerChanged += OnPower;
                _res.OxygenChanged += OnOxygen;
                _res.MentalChanged += OnMental;
            }
            if (_game != null)
            {
                _game.PhaseChanged += OnPhaseChanged;
                _game.GameEnded += OnGameEnded;
            }
            if (typing != null) typing.Completed += OnBroadcastReady;

            if (introOnStart != SubtitleId.None)
                SubtitleSystem.Instance?.Show(introOnStart);
        }

        private void OnDisable()
        {
            if (_res != null)
            {
                _res.PowerChanged -= OnPower;
                _res.OxygenChanged -= OnOxygen;
                _res.MentalChanged -= OnMental;
            }
            if (_game != null)
            {
                _game.PhaseChanged -= OnPhaseChanged;
                _game.GameEnded -= OnGameEnded;
            }
            if (typing != null) typing.Completed -= OnBroadcastReady;
        }

        private void OnPower(float n) => Evaluate(ResourceKind.Power, n);
        private void OnOxygen(float n) => Evaluate(ResourceKind.Oxygen, n);
        private void OnMental(float n) => Evaluate(ResourceKind.Mental, n);

        private void Evaluate(ResourceKind kind, float n)
        {
            foreach (var rule in thresholdRules)
            {
                if (rule == null || rule.kind != kind) continue;

                if (rule.armed && n <= rule.belowNormalized)
                {
                    rule.armed = false;
                    if (rule.id != SubtitleId.None) SubtitleSystem.Instance?.Show(rule.id);
                }
                else if (!rule.armed && n >= rule.belowNormalized + rearmHysteresis)
                {
                    rule.armed = true; // 回升越过阈值+滞回 → 重新武装，允许再次触发
                }
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            foreach (var rule in phaseRules)
            {
                if (rule != null && rule.phase == phase && rule.id != SubtitleId.None)
                    SubtitleSystem.Instance?.Show(rule.id);
            }
        }

        private void OnGameEnded(EndingType ending)
        {
            foreach (var rule in endingRules)
            {
                if (rule != null && rule.ending == ending && rule.id != SubtitleId.None)
                    SubtitleSystem.Instance?.Show(rule.id);
            }
        }

        private void OnBroadcastReady()
        {
            if (onBroadcastReady != SubtitleId.None)
                SubtitleSystem.Instance?.Show(onBroadcastReady);
        }
    }
}
