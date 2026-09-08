using UnityEngine;

namespace ActionArena.Combat
{
    /// <summary>
    /// 受伤盒：挂在可被攻击的角色身上（trigger collider，通常比命中盒大一圈）。
    /// 收到 HitBox 触发后，把伤害转发给同体的 <see cref="IDamageable"/>（PlayerHealth/EnemyHealth）。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HurtBox : MonoBehaviour
    {
        private IDamageable _damageable;

        /// <summary>本受伤盒对应的可伤害目标（可能为 null：父级缺 Health 组件）。</summary>
        public IDamageable Damageable => _damageable;

        private void Awake()
        {
            EnsureTriggerCollider();
            _damageable = GetComponentInParent<IDamageable>();
            if (_damageable == null)
                Debug.LogWarning($"[HurtBox] {name} 未在父级找到 IDamageable（请挂 PlayerHealth / EnemyHealth）。");
        }

        /// <summary>
        /// 找一个普通 Collider 设为 trigger。
        /// 注意 CharacterController 继承自 Collider 但不能设 trigger（会报 "A Character Controller cannot be a trigger"），
        /// 所以要跳过它；若物体上只有 CharacterController，则自动补一个 trigger 胶囊。
        /// </summary>
        private void EnsureTriggerCollider()
        {
            foreach (var c in GetComponents<Collider>())
            {
                if (!(c is CharacterController))
                {
                    c.isTrigger = true;
                    return;
                }
            }
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.radius = 0.4f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            Debug.LogWarning($"[HurtBox] {name} 上没有可设 trigger 的 Collider（仅有 CharacterController），已自动补 CapsuleCollider。");
        }
    }
}
