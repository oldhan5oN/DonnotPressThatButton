using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DPTB.Player;

namespace DPTB.Interaction
{
    /// <summary>
    /// 屏幕中心射线交互探测器。每帧从相机正前方发射射线，
    /// 找到 <see cref="IInteractable"/> 则设为当前焦点，按 F（Interact）触发。
    /// 设计：观察者模式（Observer）—— 通过 <see cref="FocusChanged"/> 事件
    /// 通知准星/提示 UI，避免 UI 轮询。
    /// </summary>
    public class InteractionRaycaster : MonoBehaviour
    {
        [Header("射线")]
        [SerializeField, Tooltip("射线起点（玩家相机）。留空则自动从 FirstPersonController 取")]
        private Transform rayOrigin;

        [SerializeField] private float maxDistance = 3f;
        [SerializeField, Tooltip("可交互物所在层；默认全部")]
        private LayerMask interactableMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        /// <summary>焦点变化事件（参数为新焦点，null 表示无焦点）。</summary>
        public event Action<IInteractable> FocusChanged;

        private InputSystem_Actions _actions;
        private IInteractable _current;

        public IInteractable Current => _current;

        private void Awake()
        {
            _actions = new InputSystem_Actions();

            if (rayOrigin == null)
            {
                var fpc = GetComponentInParent<FirstPersonController>();
                if (fpc != null && fpc.CameraPivot != null) rayOrigin = fpc.CameraPivot;
                else if (Camera.main != null) rayOrigin = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
            _actions.Player.Interact.performed += OnInteractPerformed;
        }

        private void OnDisable()
        {
            _actions.Player.Interact.performed -= OnInteractPerformed;
            _actions.Player.Disable();
        }

        private void OnDestroy() => _actions.Dispose();

        private void Update() => DetectFocus();

        private void DetectFocus()
        {
            IInteractable found = null;
            if (rayOrigin != null &&
                Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit,
                                maxDistance, interactableMask, triggerInteraction))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (!ReferenceEquals(found, _current))
            {
                _current?.OnFocusExit();
                _current = found;
                _current?.OnFocusEnter();
                FocusChanged?.Invoke(_current);
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (_current != null && _current.CanInteract)
                _current.Interact(gameObject);
        }
    }
}
