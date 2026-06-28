using System;
using UnityEngine;
using DPTB.Resource;
using DPTB.Player;

namespace DPTB.Game
{
    /// <summary>
    /// 游戏流程总控。持有状态机，对外提供「结局上报入口」，并广播阶段/结局事件。
    /// 设计：状态模式（State）—— 复杂的「进行/失败/终结」流转封装进各状态对象；
    /// 设备、终端只调用 ReportXxx 上报，不关心内部状态切换细节（迪米特法则）。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private ResourceSystem resources;
        [SerializeField, Tooltip("终结时锁定其输入；留空则尝试自动查找")]
        private FirstPersonController player;

        /// <summary>阶段变化（进行/失败/终结）。</summary>
        public event Action<GamePhase> PhaseChanged;
        /// <summary>游戏终结，携带结局类型。</summary>
        public event Action<EndingType> GameEnded;

        private readonly GameStateMachine _machine = new GameStateMachine();

        public GamePhase Phase { get; private set; }
        public FailureCause LastFailureCause { get; private set; }
        public EndingType LastEnding { get; private set; }
        public ResourceSystem Resources => resources;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (resources == null) resources = ResourceSystem.Instance;
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => _machine.ChangeState(new PlayingState(this));

        private void Update() => _machine.Tick();

        // ── 结局上报入口（设备 / 终端 / 状态调用）────────────────

        /// <summary>电脑终端广播完成 → 成功结局。</summary>
        public void ReportBroadcastSuccess()
        {
            if (Phase != GamePhase.Playing) return;
            LastEnding = EndingType.BroadcastSuccess;
            _machine.ChangeState(new GameOverState(this));
        }

        /// <summary>终端短路键被触发 → 进入失败态。</summary>
        public void ReportShortCircuit()
        {
            if (Phase != GamePhase.Playing) return;
            LastFailureCause = FailureCause.ShortCircuit;
            _machine.ChangeState(new FailedState(this));
        }

        /// <summary>电能跌破阈值 → 进入失败态（由 PlayingState 调用）。</summary>
        public void ReportPowerDepleted()
        {
            if (Phase != GamePhase.Playing) return;
            LastFailureCause = FailureCause.PowerDepleted;
            _machine.ChangeState(new FailedState(this));
        }

        /// <summary>开右门跳入宇宙 → 自杀结局。进行中或失败态均可触发。</summary>
        public void ReportSuicide()
        {
            if (Phase == GamePhase.Over) return;
            LastEnding = EndingType.Suicide;
            _machine.ChangeState(new GameOverState(this));
        }

        // ── 供状态对象回调 ─────────────────────────────────────

        internal void SetPhase(GamePhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        internal void RaiseGameEnded(EndingType ending) => GameEnded?.Invoke(ending);

        internal void LockPlayer()
        {
            if (player != null) player.SetControlEnabled(false);
        }
    }
}
