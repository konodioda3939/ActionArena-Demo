namespace ActionArena.Combat
{
    /// <summary>
    /// 可被攻击目标的统一接口。PlayerHealth / EnemyHealth 实现，
    /// 让 <see cref="HitBox"/> 不需要知道具体是玩家还是敌人。
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(DamageInfo info);
    }
}
