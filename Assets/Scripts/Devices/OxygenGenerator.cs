using UnityEngine;

namespace DPTB.Devices
{
    /// <summary>制氧机：扣 3% 电，氧气回升至安全值。</summary>
    public class OxygenGenerator : PowerConsumingDevice
    {
        protected override float Cost =>
            Res != null && Res.Config != null ? Res.Config.costOxygen : 0f;

        protected override void OnPowered()
        {
            Res.RestoreOxygen();
            Debug.Log("制氧机：启动制氧循环，氧气回升", this);
            // TODO(L8)：制氧音效；气压表 UI 变化
        }
    }
}
