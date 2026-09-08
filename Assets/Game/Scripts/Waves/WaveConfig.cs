using UnityEngine;

namespace ActionArena.Waves
{
    /// <summary>
    /// 单波次配置（ScriptableObject）。在 Project 窗口右键 → ActionArena → Wave Config 创建。
    /// 一个 WaveConfig = 一波；WaveManager 按顺序推进多个 WaveConfig。
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "ActionArena/Wave Config", order = 0)]
    public class WaveConfig : ScriptableObject
    {
        [System.Serializable]
        public class EnemyGroup
        {
            [Tooltip("敌人 Prefab（需带 EnemyController + EnemyHealth）")]
            public GameObject Prefab;
            [Min(1)] public int Count = 5;
            [Min(0.1f)] public float SpawnInterval = 0.6f; // 同组内连续生成的间隔
        }

        [Tooltip("本波包含的敌人组（可混合多种敌人）")]
        public EnemyGroup[] Groups = { };
    }
}
