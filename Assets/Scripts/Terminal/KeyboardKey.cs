using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DPTB.Terminal
{
    /// <summary>
    /// 单个键盘按键：声明代表字母，并自管「常态 / 悬停 / 短路」三种提色。
    /// 按钮通常做成透明（背景图已有字母），靠悬停微亮、短路变红来反馈。
    /// 优先级：短路 &gt; 悬停 &gt; 常态。
    /// 注意：按钮的 Image 需勾 RaycastTarget（即使透明）才能收到悬停/点击。
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class KeyboardKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("按键")]
        [SerializeField, Tooltip("此键代表的字母（单个，如 A）。空格/功能键可留空")]
        private string letter = "";
        [SerializeField, Tooltip("是否参与打字与短路。空格/功能键取消勾选")]
        private bool inputKey = true;

        [Header("提色（按钮通常透明）")]
        [SerializeField, Tooltip("常态颜色，一般 alpha=0 透明")]
        private Color baseColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField, Tooltip("鼠标悬停时颜色，alpha 稍高一点")]
        private Color hoverColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField, Tooltip("短路时颜色，明显的红")]
        private Color shortCircuitColor = new Color(0.9f, 0.2f, 0.2f, 0.55f);

        private Button _button;
        private Image _image;
        private bool _hovered;
        private bool _shorted;

        public char Letter => string.IsNullOrEmpty(letter) ? '\0' : char.ToUpperInvariant(letter[0]);
        public bool IsInputKey => inputKey && Letter != '\0';
        public Button Button => _button != null ? _button : (_button = GetComponent<Button>());
        public Image Image => _image != null ? _image : (_image = GetComponent<Image>());

        private void Awake()
        {
            // 关掉 Button 自带的 Color Tint 过渡，否则它会用 Normal Color(默认不透明白)
            // 覆盖我们设置的透明色。点击/悬停事件不受影响。
            if (Button != null) Button.transition = Selectable.Transition.None;
            Refresh();
        }
        private void OnEnable() => Refresh();

        /// <summary>设为/取消短路（VirtualKeyboard 调用）。</summary>
        public void SetShortCircuited(bool on) { _shorted = on; Refresh(); }

        /// <summary>重置视觉到常态（会话开始时调用）。</summary>
        public void ResetVisual() { _hovered = false; _shorted = false; Refresh(); }

        public void OnPointerEnter(PointerEventData e) { _hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _hovered = false; Refresh(); }

        // 点击后清掉悬停并取消选中，避免按钮卡在高亮态（移开不恢复透明）
        public void OnPointerClick(PointerEventData e)
        {
            _hovered = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Refresh();
        }

        private void Refresh()
        {
            if (Image == null) return;
            if (_shorted) Image.color = shortCircuitColor;
            else if (_hovered) Image.color = hoverColor;
            else Image.color = baseColor;
        }

        // 编辑期：拖上组件时尝试用子 TMP 文本自动填字母
        private void Reset()
        {
            var label = GetComponentInChildren<TMP_Text>();
            if (label != null && label.text != null && label.text.Trim().Length == 1)
                letter = label.text.Trim().ToUpperInvariant();
        }
    }
}
