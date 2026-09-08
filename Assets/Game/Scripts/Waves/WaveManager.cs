using System.Collections;
using UnityEngine;
using ActionArena.Core;
using ActionArena.Enemy;

namespace ActionArena.Waves
{
    /// <summary>
    /// 波次管理器：按顺序推进 <see cref="_waves"/>，调度 <see cref="EnemySpawner"/> 生成敌人，
    /// 通过订阅 <see cref="EnemyKilledEvent"/> 追踪存活数；本波生成完毕且存活为 0 时进入下一波。
    /// 进入 Playing 状态自动开始；离开则停止。
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private WaveConfig[] _waves;
        [SerializeField] private EnemySpawner _spawner;
        [Tooltip("生成圆心（留空则跟随玩家位置环绕生成）")]
        [SerializeField] private Transform _spawnCenter;
        [SerializeField] private float _spawnRadius = 8f;
        [SerializeField] private float _delayBetweenWaves = 2f;
        [Tooltip("所有波次打完后是否从头循环")]
        [SerializeField] private bool _loop = true;

        public int CurrentWave { get; private set; }       // 从 1 开始（给 UI 显示）
        public int EnemiesRemaining { get; private set; }

        private int _waveIndex;
        private int _pendingSpawn;   // 本波尚未生成的数量（用于判定"本波是否全部生成完"）
        private bool _waveActive;
        private Coroutine _spawnCo;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.NewState == GameState.Playing)
            {
                _waveIndex = 0;
                CurrentWave = 0;
                StartNextWave();
            }
            else
            {
                if (_spawnCo != null) StopCoroutine(_spawnCo);
                _spawnCo = null;
                _waveActive = false;
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            if (!_waveActive) return;
            EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
            BroadcastWave();

            // 本波全部生成完（pendingSpawn==0）且场上无存活 → 进入下一波
            if (EnemiesRemaining == 0 && _pendingSpawn == 0)
            {
                _waveActive = false;
                StartCoroutine(DelayedNextWave());
            }
        }

        private void StartNextWave()
        {
            if (_waves == null || _waves.Length == 0)
            {
                Debug.LogWarning("[WaveManager] 未配置任何波次。");
                return;
            }

            if (_waveIndex >= _waves.Length)
            {
                if (_loop) _waveIndex = 0;
                else
                {
                    Debug.Log("[WaveManager] 所有波次完成！");
                    return;
                }
            }

            WaveConfig wave = _waves[_waveIndex];
            CurrentWave = _waveIndex + 1;
            _waveActive = true;

            int total = 0;
            foreach (var g in wave.Groups) total += g.Count;
            _pendingSpawn = total;
            EnemiesRemaining = total;
            BroadcastWave();

            _spawnCo = StartCoroutine(SpawnWave(wave));
        }

        private IEnumerator SpawnWave(WaveConfig wave)
        {
            Vector3 center = ResolveCenter();
            foreach (var group in wave.Groups)
            {
                if (group == null || group.Prefab == null) continue;
                for (int i = 0; i < group.Count; i++)
                {
                    _spawner.SpawnOnRing(group.Prefab, center, _spawnRadius);
                    _pendingSpawn--;
                    if (group.SpawnInterval > 0f)
                        yield return new WaitForSeconds(group.SpawnInterval);
                }
            }
            _spawnCo = null;
        }

        private IEnumerator DelayedNextWave()
        {
            yield return new WaitForSeconds(_delayBetweenWaves);
            _waveIndex++;
            StartNextWave();
        }

        private Vector3 ResolveCenter()
        {
            if (_spawnCenter != null) return _spawnCenter.position;
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform.position : transform.position;
        }

        private void BroadcastWave()
        {
            EventBus.Raise(new WaveChangedEvent { WaveIndex = CurrentWave, EnemiesRemaining = EnemiesRemaining });
        }
    }
}
