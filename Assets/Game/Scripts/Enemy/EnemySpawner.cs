using UnityEngine;
using UnityEngine.AI;

namespace ActionArena.Enemy
{
    /// <summary>
    /// 敌人工厂：在指定位置生成敌人并注入玩家目标。
    /// <see cref="Waves.WaveManager"/> 调用它刷怪；后续若敌人也走对象池，只需改本类。
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("玩家 Transform（敌人追击目标）")]
        [SerializeField] private Transform _player;

        public void SetPlayer(Transform player) => _player = player;

        private void Start()
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
                else Debug.LogWarning("[EnemySpawner] 未找到 Tag=Player 的物体，敌人将缺少追击目标。");
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject enemy = Instantiate(prefab, position, rotation);
            if (enemy.TryGetComponent(out EnemyController ctrl))
                ctrl.Initialize(_player);
            return enemy;
        }

        /// <summary>
        /// 在以 center 为圆心、radius 为半径的圆环上随机生成（避免重叠堆在一点）。
        /// 落点必须满足两点：(1) 在 NavMesh 上；(2) <b>能寻路到玩家(center)</b>。
        /// 只验 (1) 不够——地面比围墙区域大时，NavMesh 会烘到墙外，墙外虽是蓝色但被墙挡住、
        /// 敌人走不进来也打不到，会导致本波永远清不完（卡死）。故用 CalculatePath 验证可达性。
        /// </summary>
        public GameObject SpawnOnRing(GameObject prefab, Vector3 center, float radius)
        {
            // 1. 指定半径的圆环上找"在 NavMesh 上 且 能寻路到玩家"的点
            if (TryFindReachable(center, radius, out Vector3 spawn))
                return Spawn(prefab, spawn, Quaternion.identity);

            // 2. 玩家贴墙 / 场地偏小：缩小半径再试（更可能落在墙内）
            if (TryFindReachable(center, radius * 0.5f, out spawn))
                return Spawn(prefab, spawn, Quaternion.identity);

            // 3. 兜底：玩家身边必有 NavMesh（保证至少出怪，避免空波）
            if (NavMesh.SamplePosition(center, out NavMeshHit near, 3f, NavMesh.AllAreas))
                return Spawn(prefab, near.position, Quaternion.identity);

            // 4. 极端兜底：原样生成
            Vector2 d = Random.insideUnitCircle.normalized;
            return Spawn(prefab, center + new Vector3(d.x, 0f, d.y) * radius, Quaternion.identity);
        }

        /// <summary>
        /// 在圆环上随机取点，找第一个"在 NavMesh 上 且 能完整寻路到 center"的位置。
        /// 返回是否找到；找到则 spawn 为该世界坐标。
        /// </summary>
        private bool TryFindReachable(Vector3 center, float radius, out Vector3 spawn)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                Vector3 pos = center + new Vector3(dir.x, 0f, dir.y) * radius;

                // 必须落在 NavMesh 上
                if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    continue;

                // 且必须能寻路到玩家（墙外的点虽蓝色，但路径会被墙截断成 Partial）
                var path = new NavMeshPath();
                if (NavMesh.CalculatePath(hit.position, center, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete)
                {
                    spawn = hit.position;
                    return true;
                }
            }
            spawn = center;
            return false;
        }
    }
}
