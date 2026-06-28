using UnityEngine;

namespace DPTB.Devices
{
    /// <summary>
    /// 神经电击器：扣 2% 电，恢复精神。
    /// 注：设计中的「先扣电激活 → 再点击电击把手」两段式交互后续可拆为独立把手交互件，
    /// 当前简化为确认即恢复。
    /// </summary>
    public class ElectroshockDevice : PowerConsumingDevice
    {
        protected override float Cost =>
            Res != null && Res.Config != null ? Res.Config.costElectroshock : 0f;

        protected override void OnPowered()
        {
            Res.RestoreMental();
            Debug.Log("神经电击器：精神回升", this);
            // TODO：电击把手两段式交互；L8 电击音效
        }
    }
}
