using UnityEngine;

namespace ActionArena.Core
{
    // ===== 游戏流程 =====

    public struct GameStateChangedEvent : IGameEvent
    {
        public GameState NewState;
    }

    public struct ScoreChangedEvent : IGameEvent
    {
        public int Score;
    }

    public struct WaveChangedEvent : IGameEvent
    {
        public int WaveIndex;        // 当前波次（从 1 开始计数给 UI 显示）
        public int EnemiesRemaining; // 本波剩余敌人
    }

    // ===== 战斗 / 生命 =====

    public struct HealthChangedEvent : IGameEvent
    {
        public GameObject Target;   // 谁的血量变了（玩家或敌人）
        public float Current;       // 当前血量
        public float Max;           // 最大血量
        public bool IsPlayer;       // 玩家=true，敌人=false
    }

    public struct PlayerDiedEvent : IGameEvent { }

    /// <summary>一次命中落地。HitStop / CameraShake / VFXPool / FloatingText 订阅它做出打击反馈。</summary>
    public struct HitLandedEvent : IGameEvent
    {
        public Vector3 HitPoint;        // 命中世界坐标（粒子/飘字生成点）
        public Vector3 HitNormal;       // 命中法线（粒子飞溅方向）
        public float Damage;
        public bool Killed;             // 这次命中是否致死
        public bool HeavyHit;           // 重击/终结技 → 更强屏震与顿帧
    }

    public struct EnemyKilledEvent : IGameEvent
    {
        public Vector3 Position;
        public int ScoreReward;
    }
}
