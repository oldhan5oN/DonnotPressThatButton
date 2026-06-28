using System;
using System.Collections.Generic;
using UnityEngine;

namespace DPTB.UI
{
    /// <summary>
    /// 一条字幕的数据：主键 + 多句文案 + 打字速度 + 停留时长 + 优先级 + 是否只播一次。
    /// <see cref="texts"/> 按顺序逐句播放（一段话可分多句），整段作为一个不可分割的原子单元。
    /// 纯数据，逻辑无关（数据驱动 / data-driven）。
    /// </summary>
    [Serializable]
    public class SubtitleLine
    {
        [Tooltip("主键（与代码里的 SubtitleId 对应）")]
        public SubtitleId id = SubtitleId.None;

        [Tooltip("按顺序逐句播放；一段话可拆成多句，策划可随意增减")]
        public List<string> texts = new List<string>();

        [Min(0f), Tooltip("逐字显示的每字间隔（秒），越小越快")]
        public float charInterval = 0.06f;

        [Min(0f), Tooltip("打完后停留多久再淡出（秒）")]
        public float holdTime = 2.5f;

        [Tooltip("优先级：越大越优先；高优先级字幕可打断正在播放的低优先字幕")]
        public int priority = 0;

        [Tooltip("勾选后整局只播一次（如开场白、首次缺氧）")]
        public bool playOnce = false;
    }
}
