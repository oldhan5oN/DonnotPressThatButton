using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace DPTB.Terminal
{
    /// <summary>每个字符位的状态。</summary>
    public enum CharState
    {
        Pending, // 未输入：低透明度提示
        Correct, // 正确：全亮
        Wrong    // 错按：显示红 *
    }

    /// <summary>
    /// 终端屏幕（视图）：用富文本渲染待打文本。
    /// 未输入=低透明度提示；正确=全亮；错按=红色 *。
    /// 设计：MVC 的 View —— 只负责呈现，不含校验逻辑。
    /// </summary>
    public class TerminalScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text textArea;
        [SerializeField, Range(0f, 1f), Tooltip("未输入汉字的提示透明度")]
        private float dimAlpha = 0.18f;
        [SerializeField] private string starColorHex = "#FF4040";

        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary>渲染整段。glyphs/states 等长；current 为当前待输入位（高亮提示用）。</summary>
        public void Render(IReadOnlyList<string> glyphs, IReadOnlyList<CharState> states, int current)
        {
            if (textArea == null) return;

            _sb.Clear();
            int dim = Mathf.RoundToInt(Mathf.Clamp01(dimAlpha) * 255f);
            string dimTag = $"<alpha=#{dim:X2}>";

            for (int i = 0; i < glyphs.Count; i++)
            {
                switch (states[i])
                {
                    case CharState.Correct:
                        _sb.Append("<alpha=#FF>").Append(glyphs[i]);
                        break;
                    case CharState.Wrong:
                        _sb.Append($"<alpha=#FF><color={starColorHex}>*</color>");
                        break;
                    default: // Pending
                        _sb.Append(dimTag).Append(glyphs[i]);
                        break;
                }
            }
            _sb.Append("<alpha=#FF>");
            textArea.text = _sb.ToString();
        }
    }
}
