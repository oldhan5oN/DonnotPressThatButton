using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DPTB.UI;

namespace DPTB.Interaction
{
    /// <summary>
    /// 交互提示 UI 绑定器：订阅 <see cref="InteractionRaycaster.FocusChanged"/>，
    /// 焦点可交互时显示提示文字并高亮准星，否则隐藏。
    /// 文本与字幕共用同一个 text：提示登记给 <see cref="SubtitleSystem"/>，
    /// 字幕占用时自动让位（被挤掉），字幕播完再恢复。
    /// 设计：观察者模式（Observer）订阅端 + 依赖倒置（DIP，依赖字幕显示服务）。
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private InteractionRaycaster raycaster;
        [SerializeField, Tooltip("回退用的独立提示文本；若场景有 SubtitleSystem 则共用其字幕 text，此项可留空")]
        private TMP_Text promptText;
        [SerializeField, Tooltip("准星图形，可选；焦点时变色")]
        private Graphic crosshair;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color focusColor = new Color(1f, 0.85f, 0.3f);

        private void Reset()
        {
            raycaster = FindFirstObjectByType<InteractionRaycaster>();
        }

        private void OnEnable()
        {
            if (raycaster != null) raycaster.FocusChanged += OnFocusChanged;
            OnFocusChanged(raycaster != null ? raycaster.Current : null);
        }

        private void OnDisable()
        {
            if (raycaster != null) raycaster.FocusChanged -= OnFocusChanged;
        }

        private void OnFocusChanged(IInteractable target)
        {
            bool show = target != null && target.CanInteract;
            string prompt = show ? target.Prompt : string.Empty;

            var subs = SubtitleSystem.Instance;
            if (subs != null)
            {
                // 与字幕共用 text：登记/清除提示，字幕占用时由 SubtitleSystem 自动让位
                if (show) subs.SetPrompt(prompt);
                else subs.ClearPrompt();
            }
            else if (promptText != null)
            {
                // 回退：无字幕系统时用独立文本
                promptText.text = prompt;
                promptText.enabled = show;
            }

            if (crosshair != null)
                crosshair.color = show ? focusColor : normalColor;
        }
    }
}
