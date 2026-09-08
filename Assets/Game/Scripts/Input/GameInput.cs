using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionArena.Input
{
    /// <summary>
    /// 统一输入读取层。基于新 Input System，但按键在代码内默认绑定，
    /// 因此无需手动创建 .inputactions 资产即可运行（想改键直接改这里的 binding 字符串）。
    ///
    /// 挂在玩家根节点上，PlayerController 等通过引用读取其每帧属性。
    /// </summary>
    public class GameInput : MonoBehaviour
    {
        // 默认按键（可在此调整）
        private const string KMoveBindings =
            "2DVector(up=<Keyboard>/w,down=<Keyboard>/s,left=<Keyboard>/a,right=<Keyboard>/d)";

        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _jumpAction;
        private InputAction _pauseAction;

        /// <summary>移动输入（归一化前的原始向量，幅度 0~1）。</summary>
        public Vector2 Move { get; private set; }

        public bool AttackPressedThisFrame { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool PausePressedThisFrame { get; private set; }

        private void Awake()
        {
            _moveAction = new InputAction("Move");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _attackAction = new InputAction("Attack", binding: "<Mouse>/leftButton");
            _jumpAction   = new InputAction("Jump",   binding: "<Keyboard>/space");
            _pauseAction  = new InputAction("Pause",  binding: "<Keyboard>/escape");
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _attackAction.Enable();
            _jumpAction.Enable();
            _pauseAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _attackAction.Disable();
            _jumpAction.Disable();
            _pauseAction.Disable();
        }

        private void Update()
        {
            Move = _moveAction.ReadValue<Vector2>();
            AttackPressedThisFrame = _attackAction.WasPressedThisFrame();
            JumpPressedThisFrame   = _jumpAction.WasPressedThisFrame();
            PausePressedThisFrame  = _pauseAction.WasPressedThisFrame();
        }
    }
}
