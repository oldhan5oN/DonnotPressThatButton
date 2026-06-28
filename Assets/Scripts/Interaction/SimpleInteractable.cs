using UnityEngine;
using UnityEngine.Events;

namespace DPTB.Interaction
{
    /// <summary>
    /// 通用测试交互物：交互时打日志并触发 UnityEvent。
    /// 用于在正式设备脚本完成前验证射线/提示链路（丢一个带 Collider 的方块上即可）。
    /// </summary>
    public class SimpleInteractable : InteractableBase
    {
        [SerializeField] private UnityEvent onInteract;

        protected override void OnInteract(GameObject interactor)
        {
            Debug.Log($"[Interact] 触发：{name}（{Prompt}）", this);
            onInteract?.Invoke();
        }
    }
}
