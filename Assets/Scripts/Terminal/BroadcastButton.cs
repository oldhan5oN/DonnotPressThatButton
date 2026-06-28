using UnityEngine;
using DPTB.Interaction;
using DPTB.Resource;
using DPTB.Game;

namespace DPTB.Terminal
{
    /// <summary>
    /// 广播按钮柱：两段式交互。
    /// 第一次 F 打开玻璃罩；第二次 F 广播——需终端文本已完成，
    /// 扣 10% 电后触发广播成功结局（GameManager.ReportBroadcastSuccess）。
    /// </summary>
    public class BroadcastButton : InteractableBase
    {
        [Header("关联")]
        [SerializeField] private TypingController typing;
        [SerializeField, Tooltip("玻璃罩物体：激活=罩着，打开后隐藏")]
        private GameObject glassCover;

        [Header("提示文案")]
        [SerializeField] private string openCoverPrompt = "打开玻璃罩";
        [SerializeField] private string broadcastReadyPrompt = "广播（扣 10% 电）";
        [SerializeField] private string notReadyPrompt = "需先在终端完成广播文本";

        private bool _coverOpen;

        private bool Ready => typing != null && typing.CanBroadcast;

        public override string Prompt =>
            !_coverOpen ? openCoverPrompt : (Ready ? broadcastReadyPrompt : notReadyPrompt);

        protected override void OnInteract(GameObject interactor)
        {
            if (!_coverOpen)
            {
                _coverOpen = true;
                if (glassCover != null) glassCover.SetActive(false);
                Debug.Log("广播柱：玻璃罩已打开", this);
                return;
            }

            if (!Ready)
            {
                Debug.Log("广播柱：终端文本尚未完成，无法广播", this);
                return;
            }

            var res = ResourceSystem.Instance;
            float cost = res != null && res.Config != null ? res.Config.costBroadcast : 10f;
            if (res != null && res.TrySpendPower(cost))
            {
                Debug.Log("广播柱：广播成功，电源即将熄灭……", this);
                GameManager.Instance?.ReportBroadcastSuccess();
            }
            else
            {
                Debug.Log("广播柱：电量不足，无法广播", this);
            }
        }
    }
}
