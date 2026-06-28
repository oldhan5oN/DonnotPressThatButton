namespace DPTB.Game
{
    /// <summary>
    /// 极简状态机：持有当前状态，负责 Exit→切换→Enter，并逐帧 Tick。
    /// 设计：状态模式（State）的上下文容器。
    /// </summary>
    public class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void ChangeState(IGameState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick() => Current?.Tick();
    }
}
