using UnityEngine;

namespace DPTB.Game
{
    /// <summary>
    /// 终结态：广播成功 或 跳门自杀。停衰减、锁玩家、广播 GameEnded 事件，
    /// 由 UI / 死亡黑屏（L7）订阅完成收尾。
    /// </summary>
    public class GameOverState : IGameState
    {
        private readonly GameManager _ctx;

        public GameOverState(GameManager ctx) => _ctx = ctx;

        public void Enter()
        {
            _ctx.SetPhase(GamePhase.Over);

            if (_ctx.Resources != null) _ctx.Resources.SetDecayActive(false);
            _ctx.LockPlayer();

            // TODO(L7)：BroadcastSuccess → 电源渐暗收尾；Suicide → 黑屏死亡演出
            Debug.Log($"[GameManager] 游戏结束，结局：{_ctx.LastEnding}");
            _ctx.RaiseGameEnded(_ctx.LastEnding);
        }

        public void Tick() { }

        public void Exit() { }
    }
}
