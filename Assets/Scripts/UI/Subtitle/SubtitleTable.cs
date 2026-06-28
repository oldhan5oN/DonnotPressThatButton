using System.Collections.Generic;
using UnityEngine;

namespace DPTB.UI
{
    /// <summary>
    /// 字幕总表（单个大表 SO）：所有字幕集中在一个资产里，按 <see cref="SubtitleId"/> 查行。
    /// 设计：注册表模式（Registry）—— 懒构建 id→line 字典做 O(1) 查询；
    /// 数据与逻辑分离，策划只改资产不动代码。
    /// 菜单：Assets ▸ Create ▸ DPTB ▸ Subtitle Table。
    /// </summary>
    [CreateAssetMenu(fileName = "SubtitleTable", menuName = "DPTB/Subtitle Table")]
    public class SubtitleTable : ScriptableObject
    {
        [SerializeField] private List<SubtitleLine> lines = new List<SubtitleLine>();

        private Dictionary<SubtitleId, SubtitleLine> _map;

        /// <summary>按主键取字幕，未配置返回 null。</summary>
        public SubtitleLine Get(SubtitleId id)
        {
            if (_map == null) Build();
            return _map.TryGetValue(id, out var line) ? line : null;
        }

        private void Build()
        {
            _map = new Dictionary<SubtitleId, SubtitleLine>();
            foreach (var l in lines)
            {
                if (l == null || l.id == SubtitleId.None) continue;
                if (_map.ContainsKey(l.id))
                    Debug.LogWarning($"SubtitleTable：重复的字幕 id「{l.id}」，后者覆盖前者", this);
                _map[l.id] = l;
            }
        }

        // 资产被加载/编辑后清缓存，下次 Get 重建（保证 Inspector 改动即时生效）
        private void OnEnable() => _map = null;
    }
}
