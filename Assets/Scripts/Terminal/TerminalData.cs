using UnityEngine;

namespace DPTB.Terminal
{
    /// <summary>
    /// 终端广播文本数据。<see cref="broadcastText"/> 为逐字汉字全文，
    /// <see cref="targetLetters"/> 与之等长，每个汉字对应一个需按下的大写字母
    /// （如「没」→ M）；用空格表示该位置无需输入（标点/空格自动显示）。
    /// 设计：数据驱动（data-driven）—— 文案与映射全在资产，逻辑不变。
    /// 菜单：Assets ▸ Create ▸ DPTB ▸ Terminal Data。
    /// </summary>
    [CreateAssetMenu(fileName = "TerminalData", menuName = "DPTB/Terminal Data")]
    public class TerminalData : ScriptableObject
    {
        [TextArea(2, 6)]
        [Tooltip("逐字汉字全文（不要换行；标点也算一个位置）")]
        public string broadcastText = "没有时间了";

        [TextArea(2, 6)]
        [Tooltip("与上面等长，每字对应大写字母；空格=无需输入自动显示")]
        public string targetLetters = "MYSJL";
    }
}
