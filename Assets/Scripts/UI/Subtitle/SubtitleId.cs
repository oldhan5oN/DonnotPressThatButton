namespace DPTB.UI
{
    /// <summary>
    /// 字幕的类型安全主键。新增字幕时在此加一个枚举值，再到 SubtitleTable 资产里配文案。
    /// 用 enum 取代字符串 ID：编译期检查、IDE 自动补全、杜绝拼写错。
    /// </summary>
    public enum SubtitleId
    {
        None = 0,

        Intro,             // 开场旁白
        LowOxygen,         // 氧气过低（一条字幕内可分多句）
        LowMental,         // 精神低语（怂恿）
        PowerCritical,     // 电量告急
        ShortCircuitWarn,  // 短路警告
        BroadcastDone,     // 终端文本已可广播
        EnterFailed,       // 进入失败态
        SuicideLure,       // 自杀诱导
        BroadcastSuccess,  // 广播成功
    }
}
