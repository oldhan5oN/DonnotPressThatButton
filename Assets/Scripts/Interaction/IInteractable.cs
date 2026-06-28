using UnityEngine;

namespace DPTB.Interaction
{
    /// <summary>
    /// 可交互物契约。任何能被玩家「对准按 F」的对象都实现它。
    /// 设计：面向接口编程 / 开闭原则（OCP）—— 交互射线只依赖此接口，
    /// 新增设备类型无需改动 InteractionRaycaster。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>准星对准时显示的提示文字（如「按 F 阅读日记」）。</summary>
        string Prompt { get; }

        /// <summary>当前是否可交互（电量不足、已用过等情况可返回 false）。</summary>
        bool CanInteract { get; }

        /// <summary>执行交互。interactor 为发起者（通常是玩家）。</summary>
        void Interact(GameObject interactor);

        /// <summary>准星进入（用于高亮、轮廓等表现）。</summary>
        void OnFocusEnter();

        /// <summary>准星离开。</summary>
        void OnFocusExit();
    }
}
