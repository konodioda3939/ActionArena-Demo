using UnityEngine;

namespace ActionArena.Combat
{
    /// <summary>
    /// 一次伤害的完整描述。由 <see cref="HitBox"/> 在攻击命中帧产生，
    /// 传给目标 <see cref="HurtBox"/>，再由其转发到对应 Health 组件。
    /// 用 struct 避免每帧 GC。
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;          // 伤害值
        public Vector3 Knockback;     // 击退向量（方向×力度）
        public Vector3 HitPoint;      // 命中世界坐标（特效/飘字生成点）
        public Vector3 HitNormal;     // 命中表面法线（粒子飞溅方向）
        public bool Heavy;            // 重击 → 更强屏震 / 更长顿帧
        public GameObject Source;     // 伤害来源（归属、击退方向参考）
    }
}
