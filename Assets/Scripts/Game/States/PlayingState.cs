using UnityEngine;

namespace DPTB.Game
{
    /// <summary>
    /// 进行中：资源正常衰减；逐帧监测电能是否跌破失败阈值。
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly GameManager _ctx;

        public PlayingState(GameManager ctx) => _ctx = ctx;

        public void Enter()
        {
            _ctx.SetPhase(GamePhase.Playing);
            if (_ctx.Resources != null) _ctx.Resources.SetDecayActive(true);
        }

        public void Tick()
        {
            var res = _ctx.Resources;
            if (res == null || res.Config == null) return;

            // 低电失败：电能跌破阈值
            if (res.Power < res.Config.lowPowerFailThreshold)
                _ctx.ReportPowerDepleted();
        }

        public void Exit() { }
    }
}
