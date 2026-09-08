using UnityEngine;
using ActionArena.Core;
using ActionArena.Input;
using ActionArena.Combat;

namespace ActionArena.Player
{
    /// <summary>
    /// 玩家战斗：读取攻击输入，驱动攻击时序（起手 → 命中窗口 → 收招）。
    /// 命中窗口内启用 <see cref="HitBox"/>；攻击期间冻结移动以保证打击感。
    ///
    /// 第 1 周：单段攻击；连击 / 输入缓冲 / 取消窗口留到第 2 周。
    /// 命中时序先用定时器（代码自包含）；第 2 周可改由动画事件精准驱动。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameInput _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private PlayerAnimator _animator;
        [SerializeField] private HitBox _hitBox;

        [Header("攻击时序（秒）")]
        [SerializeField] private float _attackDuration = 0.5f;     // 整段攻击时长
        [SerializeField] private float _hitEnableDelay = 0.12f;    // 起手多久后开启命中盒
        [SerializeField] private float _hitActiveDuration = 0.18f; // 命中盒持续时长

        private bool _attacking;
        private float _attackStartTime;

        private void Update()
        {
            if (!(GameManager.HasInstance && GameManager.Instance.IsPlaying)) return;

            if (_attacking)
            {
                AdvanceAttack();
                return;
            }

            if (_input.AttackPressedThisFrame)
                BeginAttack();
        }

        private void BeginAttack()
        {
            _attacking = true;
            _attackStartTime = Time.time;
            _controller.CanMove = false;
            _animator?.PlayAttack(comboIndex: 0);
        }

        private void AdvanceAttack()
        {
            float t = Time.time - _attackStartTime;
            float hitEnd = _hitEnableDelay + _hitActiveDuration;

            if (t >= _hitEnableDelay && t < hitEnd)
            {
                if (_hitBox != null && !_hitBox.IsActive) _hitBox.Enable();
            }
            else if (t >= hitEnd)
            {
                if (_hitBox != null && _hitBox.IsActive) _hitBox.Disable();
            }

            if (t >= _attackDuration)
            {
                _attacking = false;
                _controller.CanMove = true;
                if (_hitBox != null && _hitBox.IsActive) _hitBox.Disable();
            }
        }

        // ===== 动画事件接口（第 2 周用动画事件替代定时器时调用） =====
        public void AnimEvent_EnableHit() => _hitBox?.Enable();
        public void AnimEvent_DisableHit() => _hitBox?.Disable();
    }
}
