using UnityEngine;
using ActionArena.Core;
using ActionArena.Combat;

namespace ActionArena.Player
{
    /// <summary>
    /// 玩家生命值。接收 <see cref="DamageInfo"/>，扣血、维护无敌帧、广播血量/死亡事件。
    /// 死亡时通知 GameManager 进入 GameOver 并广播 PlayerDiedEvent（动画层订阅播死亡）。
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _invincibleDuration = 0.6f;
        [Tooltip("可选：受击时播放受击动画（不填则只扣血不播动画）")]
        [SerializeField] private PlayerAnimator _animator;

        public float Current { get; private set; }
        public float Max => _maxHealth;
        public bool IsDead { get; private set; }
        public bool IsInvincible => Time.time - _lastHitTime < _invincibleDuration;

        private float _lastHitTime = -999f;

        private void Awake() => Current = _maxHealth;
        private void Start() => BroadcastHealth();

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || IsInvincible) return;
            _lastHitTime = Time.time;

            Current = Mathf.Max(0f, Current - info.Amount);
            BroadcastHealth();

            if (Current <= 0f) Die();
            else _animator?.PlayHit();
        }

        private void Die()
        {
            IsDead = true;
            GameManager.Instance?.GameOver();
            EventBus.Raise(new PlayerDiedEvent());
        }

        private void BroadcastHealth()
        {
            EventBus.Raise(new HealthChangedEvent
            {
                Target = gameObject,
                Current = Current,
                Max = Max,
                IsPlayer = true
            });
        }
    }
}
