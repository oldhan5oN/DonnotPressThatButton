namespace DPTB.Game
{
    /// <summary>游戏总阶段。</summary>
    public enum GamePhase
    {
        Playing,   // 进行中
        Failed,    // 失败态（未终结：精神回满、可读日记、等待自杀）
        Over       // 已终结（广播成功 或 跳门自杀）
    }

    /// <summary>结局类型（终结时分发给 UI / 死亡演出）。</summary>
    public enum EndingType
    {
        None,
        BroadcastSuccess, // 广播成功：电源渐暗、好结局
        Suicide           // 开右门跳入宇宙
    }

    /// <summary>失败原因。</summary>
    public enum FailureCause
    {
        None,
        PowerDepleted, // 电能跌破阈值
        ShortCircuit   // 终端短路键被触发
    }
}
