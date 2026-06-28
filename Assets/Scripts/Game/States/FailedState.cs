using UnityEngine;

namespace DPTB.Game
{
    /// <summary>
    /// 失败态（未终结）：停止衰减、精神回满，玩家可从容读日记，
    /// 字幕怂恿玩家开右门自杀。等待 ReportSuicide 进入终结。
    /// </summary>
    public class FailedState : IGameState
    {
        private readonly GameManager _ctx;

        public FailedState(GameManager ctx) => _ctx = ctx;

        public void Enter()
        {
            _ctx.SetPhase(GamePhase.Failed);

            var res = _ctx.Resources;
            if (res != null)
            {
                res.SetDecayActive(false); // 停衰减
                res.RecoverMentalFull();    // 精神回满，可安心读日记
            }

            // TODO(L5 字幕)：推送「怂恿自杀」字幕队列
            Debug.Log($"[GameManager] 进入失败态，原因：{_ctx.LastFailureCause}。等待玩家自杀。");
        }

        public void Tick() { }

        public void Exit() { }
    }
}
