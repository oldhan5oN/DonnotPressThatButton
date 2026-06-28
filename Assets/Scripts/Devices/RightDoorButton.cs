using UnityEngine;
using DPTB.Game;

namespace DPTB.Devices
{
    /// <summary>
    /// 右侧舱门：直接与门交互即可解锁。
    /// 未解锁时按 F 弹出耗电确认弹窗（复用 <see cref="PowerConsumingDevice"/> 的「弹窗→扣电→效果」模板），
    /// 确认后扣 <c>costUnlock</c> 电解锁；解锁后再次按 F 开门跳入宇宙 → 自杀结局。
    /// 若勾选 <see cref="suicideOnUnlock"/>，则确认扣电后立即跳出（一步式）。
    /// 设计：模板方法（Template Method）复用耗电流程；门自身即「耗电设备」，无需外部终端中介。
    /// </summary>
    public class RightDoorButton : PowerConsumingDevice
    {
        [Header("舱门")]
        [SerializeField, Tooltip("未解锁时的提示（此交互会弹耗电确认窗）")]
        private string lockedPrompt = "解锁舱门";
        [SerializeField, Tooltip("是否已解锁（一般初始为否）")]
        private bool unlocked = false;
        [SerializeField, Tooltip("勾选：确认扣电后立即开门跳出（一步式）；不勾：解锁后需再按一次 F 开门（两步式）")]
        private bool suicideOnUnlock = false;

        // 未解锁显示「解锁舱门」，解锁后显示开门提示（基类 prompt 字段）
        public override string Prompt => unlocked ? prompt : lockedPrompt;

        protected override float Cost =>
            Res != null && Res.Config != null ? Res.Config.costUnlock : 0f;

        protected override void OnInteract(GameObject interactor)
        {
            if (unlocked)
            {
                OpenDoor();
                return;
            }
            // 未解锁：走基类「弹窗 → 扣电 → OnPowered」解锁流程
            base.OnInteract(interactor);
        }

        protected override void OnPowered()
        {
            unlocked = true;
            Debug.Log("舱门已解锁", this);
            // TODO(L8)：开锁音效
            if (suicideOnUnlock) OpenDoor();
        }

        private void OpenDoor()
        {
            Debug.Log("开启右侧舱门，跳入宇宙……", this);
            // TODO(L7)：开门动画 / 第一人称黑屏死亡演出
            GameManager.Instance?.ReportSuicide();
        }
    }
}
