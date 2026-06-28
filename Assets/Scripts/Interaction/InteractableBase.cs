using UnityEngine;

namespace DPTB.Interaction
{
    /// <summary>
    /// 可交互物抽象基类。封装「能否交互 → 执行」的固定流程，
    /// 子类只需实现 <see cref="OnInteract"/> 的具体行为。
    /// 设计：模板方法（Template Method）—— Interact() 是模板，OnInteract() 是钩子。
    /// 所有设备（制氧机、电击器、终端、日记本、门…）都继承它。
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("交互")]
        [SerializeField, Tooltip("准星对准时显示的提示文字")]
        protected string prompt = "交互";

        [SerializeField, Tooltip("是否可交互；运行时可由逻辑开关")]
        protected bool interactable = true;

        public virtual string Prompt => prompt;

        public virtual bool CanInteract => interactable && isActiveAndEnabled;

        /// <summary>模板方法：固定「校验 → 执行」流程，子类不应重写。</summary>
        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            OnInteract(interactor);
        }

        /// <summary>子类实现的具体交互行为（弹 UI、扣电、播音效等）。</summary>
        protected abstract void OnInteract(GameObject interactor);

        public virtual void OnFocusEnter() { }
        public virtual void OnFocusExit() { }

        /// <summary>运行时开关可交互状态。</summary>
        public void SetInteractable(bool value) => interactable = value;
    }
}
