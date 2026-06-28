using UnityEngine;
using DPTB.Interaction;
using DPTB.Resource;
using DPTB.UI;

namespace DPTB.Devices
{
    /// <summary>
    /// 耗电设备抽象基类：交互弹窗 →（确认）TrySpendPower → 成功执行效果 / 失败提示。
    /// 设计：模板方法（Template Method）—— 固定「弹窗-扣电-分支」流程，
    /// 子类只填 <see cref="Cost"/> 与 <see cref="OnPowered"/>。成本统一取自 ResourceConfig（数据驱动）。
    /// </summary>
    public abstract class PowerConsumingDevice : InteractableBase
    {
        [Header("设备弹窗")]
        [SerializeField] protected string popupTitle = "设备";
        [SerializeField, TextArea] protected string popupBody = "确认操作？";

        protected ResourceSystem Res => ResourceSystem.Instance;

        /// <summary>本次操作的耗电量（子类从 config 取对应字段）。</summary>
        protected abstract float Cost { get; }

        protected override void OnInteract(GameObject interactor)
        {
            if (DevicePopupUI.Instance == null)
            {
                Debug.LogError("场景缺少 DevicePopupUI，无法弹窗", this);
                return;
            }
            string body = $"{popupBody}\n\n消耗电能：{Cost:0}%";
            DevicePopupUI.Instance.Show(popupTitle, body, Confirm);
        }

        private void Confirm()
        {
            if (Res == null) { Debug.LogError("缺少 ResourceSystem", this); return; }
            if (Res.TrySpendPower(Cost)) OnPowered();
            else OnInsufficientPower();
        }

        /// <summary>扣电成功后的设备效果。</summary>
        protected abstract void OnPowered();

        /// <summary>电量不足时的处理（默认打日志，可重写做 UI 提示）。</summary>
        protected virtual void OnInsufficientPower()
            => Debug.Log($"{name}：电量不足，无法操作", this);
    }
}
