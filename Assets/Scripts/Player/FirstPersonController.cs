using UnityEngine;
using UnityEngine.InputSystem;

namespace DPTB.Player
{
    /// <summary>
    /// 第一人称控制器：基于 CharacterController 的 WASD 移动 + 鼠标视角。
    /// 消费自动生成的 InputSystem_Actions（Player map）的 Move / Look。
    /// 死亡 / 暂停 / 打开 UI 时通过 <see cref="SetControlEnabled"/> 锁输入。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("玩家视角相机（应为本物体的子物体）。也是屏幕中心射线的发射源。")]
        [SerializeField] private Transform cameraPivot;

        [Header("移动")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("视角")]
        [Tooltip("鼠标灵敏度（度/像素）。设置面板会改写此值。")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [Tooltip("俯仰角上下限，防止翻头。")]
        [SerializeField] private float pitchClamp = 80f;
        [SerializeField] private bool invertY = false;

        [Header("启动")]
        [SerializeField] private bool lockCursorOnStart = true;

        private CharacterController _controller;
        private InputSystem_Actions _actions;

        // 视角状态
        private float _pitch;          // 累计俯仰角（绕相机 X 轴）
        private float _verticalVel;    // 竖直速度（重力）

        // 输入开关：UI / 暂停 / 死亡时置 false
        private bool _controlEnabled = true;

        /// <summary>对外暴露视角相机的 Transform，供交互射线等系统使用。</summary>
        public Transform CameraPivot => cameraPivot;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _actions = new InputSystem_Actions();

            if (cameraPivot == null)
            {
                // 兜底：尝试用子物体上的相机
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
        }

        private void OnEnable() => _actions.Player.Enable();

        private void OnDisable() => _actions.Player.Disable();

        private void OnDestroy() => _actions.Dispose();

        private void Start()
        {
            if (lockCursorOnStart) SetCursorLocked(true);
            // 初始化俯仰角为相机当前角度，避免起手跳变
            if (cameraPivot != null)
                _pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
        }

        private void Update()
        {
            if (!_controlEnabled) return;

            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            Vector2 look = _actions.Player.Look.ReadValue<Vector2>();

            float yawDelta = look.x * lookSensitivity;
            float pitchDelta = look.y * lookSensitivity * (invertY ? 1f : -1f);

            // 左右转 → 旋转身体（绕世界 Y 轴）
            transform.Rotate(Vector3.up, yawDelta, Space.World);

            // 上下看 → 旋转相机（绕本地 X 轴），并 clamp
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -pitchClamp, pitchClamp);
            if (cameraPivot != null)
            {
                Vector3 e = cameraPivot.localEulerAngles;
                e.x = _pitch;
                cameraPivot.localEulerAngles = e;
            }
        }

        private void HandleMove()
        {
            Vector2 input = _actions.Player.Move.ReadValue<Vector2>();
            // 相对玩家朝向的水平移动
            Vector3 move = (transform.right * input.x + transform.forward * input.y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            move *= moveSpeed;

            // 重力
            if (_controller.isGrounded && _verticalVel < 0f)
                _verticalVel = -2f; // 贴地小负值，保证 isGrounded 稳定
            _verticalVel += gravity * Time.deltaTime;

            Vector3 velocity = move + Vector3.up * _verticalVel;
            _controller.Move(velocity * Time.deltaTime);
        }

        private static float NormalizePitch(float euler)
        {
            // 把 0..360 的欧拉角转成 -180..180
            return euler > 180f ? euler - 360f : euler;
        }

        // ── 对外接口 ───────────────────────────────────────────────

        /// <summary>锁/解锁移动与视角输入。UI 打开、暂停、死亡时调用。</summary>
        public void SetControlEnabled(bool enabled)
        {
            _controlEnabled = enabled;
            // 锁输入时同时把竖直速度清零，避免恢复时累积下坠
            if (!enabled) _verticalVel = 0f;
        }

        /// <summary>设置鼠标灵敏度（设置面板调用）。</summary>
        public void SetLookSensitivity(float value) => lookSensitivity = Mathf.Max(0.001f, value);

        /// <summary>锁定/释放鼠标光标。打开 UI 时释放以便点击屏幕键盘。</summary>
        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
