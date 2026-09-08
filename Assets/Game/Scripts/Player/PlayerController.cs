using UnityEngine;
using ActionArena.Core;
using ActionArena.Input;

namespace ActionArena.Player
{
    /// <summary>
    /// 玩家角色 3C：移动 / 朝向 / 重力。基于 CharacterController，相机相对方向移动。
    /// 跳跃、冲刺留作第 2 周扩展；本版聚焦「能稳定跑动 + 朝向相机方向」。
    ///
    /// 攻击/受击时由 PlayerCombat 把 <see cref="CanMove"/> 置 false 暂时冻结移动。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _rotationSmoothTime = 0.08f;
        [SerializeField] private float _gravity = -25f;

        [Header("引用")]
        [SerializeField] private GameInput _input;
        [SerializeField] private PlayerAnimator _animator;
        [Tooltip("留空则自动取 Camera.main")]
        [SerializeField] private Transform _cameraTransform;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private bool _grounded;

        /// <summary>是否允许移动（攻击/受击时冻结）。</summary>
        public bool CanMove { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            bool playing = GameManager.HasInstance && GameManager.Instance.IsPlaying;
            Vector2 moveInput = (playing && CanMove) ? _input.Move : Vector2.zero;

            Vector3 moveDir = ComputeMoveDirection(moveInput);
            Vector3 horizontal = moveDir * _moveSpeed;

            ApplyGravity();
            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.0001f)
                RotateTowards(moveDir);

            // 把「期望水平速度」喂给动画（0~_moveSpeed），由 PlayerAnimator 映射成 Speed 参数
            _animator?.SetMoveSpeed(horizontal.magnitude);
            _animator?.SetGrounded(_grounded);
        }

        /// <summary>把输入向量映射到相机朝向的世界方向（压平 Y）。</summary>
        private Vector3 ComputeMoveDirection(Vector2 input)
        {
            if (_cameraTransform == null || input.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Vector3 forward = Vector3.Scale(_cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 right = Vector3.Scale(_cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            Vector3 dir = forward * input.y + right * input.x;
            return Vector3.ClampMagnitude(dir, 1f);
        }

        private void RotateTowards(Vector3 dir)
        {
            float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float smoothed = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetYaw, ref _rotationVelocity, _rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothed, 0f);
        }

        private void ApplyGravity()
        {
            _grounded = _controller.isGrounded;
            if (_grounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // 贴地，避免累计下落
            _verticalVelocity += _gravity * Time.deltaTime;
        }
    }
}
