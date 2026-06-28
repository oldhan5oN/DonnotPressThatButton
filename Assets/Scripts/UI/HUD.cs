using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DPTB.Resource;

namespace DPTB.UI
{
    /// <summary>
    /// HUD：电能 / 氧气 / 精神三条资源条 + 可选百分比文本。
    /// 订阅 ResourceSystem 的归一化事件实时刷新。
    /// 设计：观察者模式（Observer）订阅端 —— 不轮询、与数值逻辑解耦。
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] private ResourceSystem resources;

        [Header("填充条 (Image, Image Type = Filled)")]
        [SerializeField] private Image powerFill;
        [SerializeField] private Image oxygenFill;
        [SerializeField] private Image mentalFill;

        [Header("数值文本 (可选)")]
        [SerializeField] private TMP_Text powerLabel;
        [SerializeField] private TMP_Text oxygenLabel;
        [SerializeField] private TMP_Text mentalLabel;

        private void Start()
        {
            if (resources == null) resources = ResourceSystem.Instance;
            if (resources == null)
            {
                Debug.LogError("HUD: 找不到 ResourceSystem", this);
                return;
            }

            resources.PowerChanged += OnPower;
            resources.OxygenChanged += OnOxygen;
            resources.MentalChanged += OnMental;

            // 订阅前 ResourceSystem.Awake 已广播过初始值，这里主动拉取一次补上
            OnPower(resources.PowerNormalized);
            OnOxygen(resources.OxygenNormalized);
            OnMental(resources.MentalNormalized);
        }

        private void OnDestroy()
        {
            if (resources == null) return;
            resources.PowerChanged -= OnPower;
            resources.OxygenChanged -= OnOxygen;
            resources.MentalChanged -= OnMental;
        }

        private void OnPower(float n) => Apply(powerFill, powerLabel, n);
        private void OnOxygen(float n) => Apply(oxygenFill, oxygenLabel, n);
        private void OnMental(float n) => Apply(mentalFill, mentalLabel, n);

        private static void Apply(Image fill, TMP_Text label, float normalized)
        {
            if (fill != null) fill.fillAmount = normalized;
            if (label != null) label.text = label.name + ": " + Mathf.RoundToInt(normalized * 100f) + "%";
        }
    }
}
