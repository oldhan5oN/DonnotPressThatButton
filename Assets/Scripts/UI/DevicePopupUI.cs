using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DPTB.Player;
using DPTB.Interaction;

namespace DPTB.UI
{
    /// <summary>
    /// 通用设备弹窗：标题 + 正文（含耗电提示）+ 确认 / 取消。
    /// 设备调用 <see cref="Show"/> 传入回调；确认即执行回调。
    /// 打开时冻结玩法输入并释放光标以便点击，关闭时还原。
    /// 设计：单例聚合的「模态面板」—— L5 会并入 UIManager 的面板栈（Mediator）。
    /// </summary>
    public class DevicePopupUI : MonoBehaviour
    {
        public static DevicePopupUI Instance { get; private set; }

        [Header("UI 引用")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("玩法输入冻结（留空自动查找）")]
        [SerializeField] private FirstPersonController player;
        [SerializeField] private InteractionRaycaster raycaster;

        private Action _onConfirm;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (raycaster == null) raycaster = FindFirstObjectByType<InteractionRaycaster>();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(Hide);

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>显示弹窗。onConfirm 在玩家点确认后触发。</summary>
        public void Show(string title, string body, Action onConfirm)
        {
            _onConfirm = onConfirm;
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (root != null) root.SetActive(true);
            SetGameplayInput(false);
        }

        /// <summary>关闭弹窗（取消，或确认后）。</summary>
        public void Hide()
        {
            _onConfirm = null;
            if (root != null) root.SetActive(false);
            SetGameplayInput(true);
        }

        private void HideImmediate()
        {
            _onConfirm = null;
            if (root != null) root.SetActive(false);
        }

        private void OnConfirmClicked()
        {
            Action cb = _onConfirm;
            Hide();            // 先还原输入再执行回调（回调可能再开新弹窗）
            cb?.Invoke();
        }

        private void SetGameplayInput(bool on)
        {
            if (player != null) player.SetControlEnabled(on);
            if (raycaster != null) raycaster.enabled = on;
            FirstPersonController.SetCursorLocked(on);
        }
    }
}
