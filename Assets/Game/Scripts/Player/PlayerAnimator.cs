using UnityEngine;
using ActionArena.Core;

namespace ActionArena.Player
{
    /// <summary>
    /// 玩家动画驱动。把逻辑层（速度/攻击/受击/死亡）映射为 Animator 参数。
    /// 参数名（Speed/Grounded/Attack/AttackIndex/Hit/Dead）必须与 Animator Controller 一致。
    /// 用 StringToHash 缓存 id，避免每帧字符串查找。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Tooltip("移动速度归一化上限，需与 PlayerController._moveSpeed 保持一致")]
        [SerializeField] private float _maxMoveSpeed = 6f;

        private Animator _animator;
        private int _speedHash, _groundedHash, _attackHash, _attackIndexHash, _hitHash, _deadHash;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _speedHash       = Animator.StringToHash("Speed");
            _groundedHash    = Animator.StringToHash("Grounded");
            _attackHash      = Animator.StringToHash("Attack");
            _attackIndexHash = Animator.StringToHash("AttackIndex");
            _hitHash         = Animator.StringToHash("Hit");
            _deadHash        = Animator.StringToHash("Dead");
        }

        // 订阅玩家死亡事件播死亡动画（解耦：PlayerHealth 不直接引用动画层）
        private void OnEnable()  => EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        private void OnDisable() => EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        private void OnPlayerDied(PlayerDiedEvent _) => PlayDie();

        public void SetMoveSpeed(float worldSpeed)
            => _animator.SetFloat(_speedHash, Mathf.Clamp01(worldSpeed / Mathf.Max(0.0001f, _maxMoveSpeed)));

        public void SetGrounded(bool grounded) => _animator.SetBool(_groundedHash, grounded);

        public void PlayAttack(int comboIndex)
        {
            _animator.SetInteger(_attackIndexHash, comboIndex);
            _animator.SetTrigger(_attackHash);
        }

        public void PlayHit() => _animator.SetTrigger(_hitHash);
        public void PlayDie() => _animator.SetBool(_deadHash, true);
    }
}
