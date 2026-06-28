using UnityEngine;
using UnityEngine.UI;
using DPTB.Interaction;
using DPTB.Player;

namespace DPTB.Terminal
{
    /// <summary>
    /// 电脑终端世界交互入口：按 F 打开终端面板（屏幕 + 虚拟键盘），
    /// 冻结玩法输入并释放光标以便点击键盘；关闭时还原。
    /// 设计：与 DevicePopupUI 同构的「模态」开关，后续可并入 UIManager（Mediator）。
    /// </summary>
    public class TerminalInteractable : InteractableBase
    {
        [Header("终端面板")]
        [SerializeField] private GameObject terminalPanel;
        [SerializeField] private TypingController controller;
        [SerializeField, Tooltip("面板上的关闭/退出按钮")]
        private Button closeButton;

        [Header("玩法输入冻结（留空自动查找）")]
        [SerializeField] private FirstPersonController player;
        [SerializeField] private InteractionRaycaster raycaster;

        private bool _open;

        private void Awake()
        {
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (raycaster == null) raycaster = FindFirstObjectByType<InteractionRaycaster>();
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (terminalPanel != null) terminalPanel.SetActive(false);
        }

        protected override void OnInteract(GameObject interactor) => Open();

        public void Open()
        {
            if (_open) return;
            _open = true;
            if (terminalPanel != null) terminalPanel.SetActive(true);
            if (controller != null) controller.StartSession();
            SetGameplayInput(false);
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            if (controller != null) controller.EndSession();
            if (terminalPanel != null) terminalPanel.SetActive(false);
            SetGameplayInput(true);
        }

        private void SetGameplayInput(bool on)
        {
            if (player != null) player.SetControlEnabled(on);
            if (raycaster != null) raycaster.enabled = on;
            FirstPersonController.SetCursorLocked(on);
        }
    }
}
