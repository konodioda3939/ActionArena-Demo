using System.Collections.Generic;
using UnityEngine;
using ActionArena.Core;

namespace ActionArena.Combat
{
    /// <summary>
    /// 命中盒：挂在攻击者的武器/拳头上（trigger collider）。
    /// 攻击者（PlayerCombat / EnemyController）在挥击窗口内调用 Enable/Disable。
    ///
    /// 一次攻击对同一目标只命中一次（<see cref="_hitTargets"/> 去重），
    /// 命中后向 EventBus 广播 <see cref="HitLandedEvent"/> 供打击反馈层订阅。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour
    {
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _knockbackForce = 6f;
        [SerializeField] private bool _heavy = false;
        [Tooltip("命中是否触发打击反馈（顿帧/屏震/粒子）。玩家攻击建议开，敌人攻击按需。")]
        [SerializeField] private bool _triggerFeedback = true;

        private Collider _collider;
        private IDamageable _owner; // 本命中盒的攻击者自身，命中时排除，避免自己打自己
        private readonly HashSet<IDamageable> _hitTargets = new();

        public bool IsActive => _collider != null && _collider.enabled;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;

            // 缓存主人（向上找 IDamageable），命中时排除自己，防止挥拳扫到自身 HurtBox 自杀。
            _owner = GetComponentInParent<IDamageable>();

            // Unity 规则：Trigger 事件至少要有一方带 Rigidbody 才会触发。
            // 武器/拳头通常没挂，这里自动补一个 kinematic 的，免去手动配置踩坑。
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public void Enable()
        {
            _hitTargets.Clear();
            _collider.enabled = true;
        }

        public void Disable() => _collider.enabled = false;

        private void OnTriggerEnter(Collider other) => TryHit(other);

        private void TryHit(Collider other)
        {
            if (!_collider.enabled) return;

            var hurtBox = other.GetComponentInParent<HurtBox>();
            if (hurtBox == null || hurtBox.Damageable == null) return;

            IDamageable target = hurtBox.Damageable;
            if (target.IsDead || _hitTargets.Contains(target)) return;
            if (target == _owner) return; // 不打自己（防止挥拳扫到自身 HurtBox 自杀）
            _hitTargets.Add(target);

            Vector3 dir = (hurtBox.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            var info = new DamageInfo
            {
                Amount = _damage,
                Heavy = _heavy,
                Source = gameObject,
                HitPoint = other.ClosestPointOnBounds(transform.position),
                HitNormal = dir,
                Knockback = dir * _knockbackForce,
            };
            target.TakeDamage(info);

            if (_triggerFeedback)
                EventBus.Raise(new HitLandedEvent
                {
                    HitPoint = info.HitPoint,
                    HitNormal = info.HitNormal,
                    Damage = _damage,
                    Killed = target.IsDead,
                    HeavyHit = _heavy,
                });
        }
    }
}
