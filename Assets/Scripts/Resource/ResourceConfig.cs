using UnityEngine;

namespace DPTB.Resource
{
    /// <summary>
    /// 资源系统配置数据。初始值、衰减、耦合、恢复量、耗电成本、失败阈值集中于此。
    /// 设计：数据驱动（data-driven）—— 策划调数值不动代码。
    /// 通过菜单 Assets ▸ Create ▸ DPTB ▸ Resource Config 创建实例。
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "DPTB/Resource Config")]
    public class ResourceConfig : ScriptableObject
    {
        [Header("初始值 (0-100)")]
        [Tooltip("开局电能。设计为唯一硬预算，初始 15（约等于 15%）")]
        public float initialPower = 15f;
        [Tooltip("开局氧气，满值 100")]
        public float initialOxygen = 100f;
        [Tooltip("开局精神，满值 100")]
        public float initialMental = 100f;
        [Tooltip("三种资源共用的上限值；条的满格 = 此值")]
        public float maxValue = 100f;

        [Header("随时间衰减 (每秒)")]
        [Tooltip("氧气每秒下降量。越大氧气掉得越快，需更频繁制氧")]
        public float oxygenDecayPerSecond = 0.5f;
        [Tooltip("精神每秒下降量（基础值，未触发低氧加速时）")]
        public float mentalDecayPerSecond = 0.3f;

        [Header("耦合：氧气过低加速精神下降")]
        [Tooltip("氧气低于此值时，精神进入加速下降状态")]
        public float lowOxygenThreshold = 30f;
        [Tooltip("低氧时精神下降速度的倍率（如 2.5 = 加速到 2.5 倍）")]
        public float lowOxygenMentalMultiplier = 2.5f;

        [Header("恢复量")]
        [Tooltip("制氧机交互一次恢复的氧气量（默认 100 = 直接回满）")]
        public float oxygenRestoreAmount = 100f;
        [Tooltip("神经电击器使用一次恢复的精神量")]
        public float mentalRestoreAmount = 60f;

        [Header("耗电成本")]
        [Tooltip("启动制氧循环消耗的电能")]
        public float costOxygen = 3f;
        [Tooltip("使用神经电击器消耗的电能")]
        public float costElectroshock = 2f;
        [Tooltip("电力分配终端解锁舱门消耗的电能")]
        public float costUnlock = 1f;
        [Tooltip("电脑终端广播消耗的电能（最贵，几乎宣判己死）")]
        public float costBroadcast = 10f;

        [Header("失败阈值 (供 GameManager 用)")]
        [Tooltip("电能低于此值即判定任务失败（进入失败/自杀流程）")]
        public float lowPowerFailThreshold = 10f;
    }
}
