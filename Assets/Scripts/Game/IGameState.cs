namespace DPTB.Game
{
    /// <summary>
    /// 游戏状态契约。设计：状态模式（State）—— 每个具体状态自管进入/逐帧/退出行为。
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
