using UnityEngine;
using ActionArena.Core;
using ActionArena.Combat;

namespace ActionArena.Enemy
{
    /// <summary>
    /// 敌人生命值。实现 <see cref="IDamageable"/>，死亡时加分并广播 <see cref="EnemyKilledEvent"/>
    /// （WaveManager 订阅以判定本波是否清空；VFX 订阅以播放死亡特效）。
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 50f;
        [SerializeField] private int _scoreReward = 100;
        [SerializeField] private float _destroyDelay = 2.5f; // 播完死亡动画再销毁

        public float Current { get; private set; }
        public float Max => _maxHealth;
        public bool IsDead { get; private set; }

        private void Awake() => Current = _maxHealth;

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            Current = Mathf.Max(0f, Current - info.Amount);

            EventBus.Raise(new HealthChangedEvent
            {
                Target = gameObject, Current = Current, Max = Max, IsPlayer = false
            });

            if (Current <= 0f) Die();
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            GameManager.Instance?.AddScore(_scoreReward);
            EventBus.Raise(new EnemyKilledEvent { Position = transform.position, ScoreReward = _scoreReward });

            // 禁用所有碰撞，避免死后仍挡路或被继续命中
            foreach (var col in GetComponents<Collider>())
                col.enabled = false;

            Destroy(gameObject, _destroyDelay);
        }
    }
}
