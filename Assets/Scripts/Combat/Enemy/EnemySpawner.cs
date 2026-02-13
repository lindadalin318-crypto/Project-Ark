using System;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Context class for enemy spawning. Delegates actual spawn logic to an ISpawnStrategy.
    /// Provides shared infrastructure: spawn point selection, pool access, affix application.
    /// 
    /// Default behavior: if no strategy is injected, uses LoopSpawnStrategy (legacy mode).
    /// Arena/Boss rooms inject WaveSpawnStrategy at runtime via SetStrategy().
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        // ──────────────────── Inspector (Legacy / Fallback) ────────────────────
        [Header("Legacy Spawn Config (used if no strategy is injected)")]
        [Tooltip("The enemy prefab to spawn in legacy loop mode.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Spawn Points")]
        [Tooltip("Transform positions where enemies can spawn.")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Legacy Loop Settings")]
        [Tooltip("Maximum number of enemies alive at once (legacy loop mode).")]
        [SerializeField] private int _maxAlive = 3;

        [Tooltip("Time between respawns after an enemy dies (legacy loop mode).")]
        [SerializeField] private float _spawnInterval = 5f;

        [Tooltip("Number of enemies to spawn immediately on Start (legacy loop mode).")]
        [SerializeField] private int _initialSpawnCount = 1;

        [Header("Elite / Affix Settings")]
        [Tooltip("Optional: affix pool for creating elite enemies. Leave empty to disable.")]
        [SerializeField] private EnemyAffixSO[] _possibleAffixes;

        [Tooltip("Chance (0-1) that a spawned enemy becomes elite.")]
        [Range(0f, 1f)]
        [SerializeField] private float _eliteChance = 0f;

        [Tooltip("Maximum number of affixes an elite can receive.")]
        [SerializeField] [Min(1)] private int _maxAffixCount = 1;

        [Header("Pool Settings")]
        [Tooltip("Pre-warm count for the default enemy pool (legacy mode).")]
        [SerializeField] private int _poolPrewarmCount = 5;

        [Tooltip("Maximum pool capacity for the default enemy pool (legacy mode).")]
        [SerializeField] private int _poolMaxSize = 10;

        // ──────────────────── Runtime State ────────────────────
        private ISpawnStrategy _strategy;
        private int _nextSpawnIndex;
        private GameObjectPool _legacyPool; // 仅 legacy 模式使用

        // ──────────────────── Events ────────────────────

        /// <summary>
        /// Fired when the current strategy reports the encounter is complete.
        /// Used by Room/ArenaController to trigger room clear.
        /// </summary>
        public event Action OnEncounterComplete;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Available spawn point transforms. </summary>
        public Transform[] SpawnPoints => _spawnPoints;

        /// <summary> Whether the active strategy has completed its encounter. </summary>
        public bool IsComplete => _strategy != null && _strategy.IsEncounterComplete;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            // 如果没有外部注入策略，回退到 legacy loop 模式
            if (_strategy == null)
            {
                if (_enemyPrefab == null)
                {
                    Debug.LogWarning("[EnemySpawner] No enemy prefab and no strategy. Spawner idle.");
                    return;
                }

                if (_spawnPoints == null || _spawnPoints.Length == 0)
                {
                    Debug.LogError("[EnemySpawner] No spawn points assigned!");
                    enabled = false;
                    return;
                }

                // 为 legacy prefab 创建专属池
                _legacyPool = new GameObjectPool(_enemyPrefab, transform, _poolPrewarmCount, _poolMaxSize);

                var loop = new LoopSpawnStrategy(_enemyPrefab, _maxAlive, _initialSpawnCount, _spawnInterval);
                SetStrategy(loop);
                _strategy.Start();
            }
        }

        // ──────────────────── Strategy Injection ────────────────────

        /// <summary>
        /// Inject a spawn strategy at runtime. Call before Start() or manually call strategy.Start().
        /// Typically called by Room.ActivateEnemies() for wave-based encounters.
        /// </summary>
        public void SetStrategy(ISpawnStrategy strategy)
        {
            // 清理旧策略
            _strategy?.Reset();

            _strategy = strategy;
            _strategy.Initialize(this);
            _nextSpawnIndex = 0;

            Debug.Log($"[EnemySpawner] Strategy set: {strategy.GetType().Name}");
        }

        /// <summary>
        /// Start the current strategy. Called externally after SetStrategy() when
        /// the caller needs to control timing (e.g., ArenaController waits for lock animation).
        /// </summary>
        public void StartStrategy()
        {
            if (_strategy == null)
            {
                Debug.LogError("[EnemySpawner] Cannot start — no strategy assigned.");
                return;
            }
            _strategy.Start();
        }

        // ──────────────────── Public Spawn API ────────────────────

        /// <summary>
        /// Spawn a single enemy from the pool at the next available spawn point.
        /// Called by ISpawnStrategy implementations.
        /// </summary>
        /// <param name="prefab">The enemy prefab to spawn.</param>
        /// <returns>The spawned enemy GameObject.</returns>
        public GameObject SpawnFromPool(GameObject prefab)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("[EnemySpawner] No spawn points!");
                return null;
            }

            // 获取生成位置（轮询）
            Transform spawnPoint = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
            _nextSpawnIndex++;
            Vector3 position = spawnPoint.position;

            // 从对象池获取实例
            var pool = GetOrCreatePool(prefab);
            GameObject enemy = pool.Get(position, Quaternion.identity);

            // 重置 Brain
            var brain = enemy.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.ResetBrain(position);
            }

            // 尝试应用精英词缀
            TryApplyAffixes(enemy);

            // 订阅死亡事件（对象池回收会清空事件，所以每次都需重新订阅）
            var entity = enemy.GetComponent<EnemyEntity>();
            if (entity != null)
            {
                entity.OnDeath += () => HandleEnemyDeath(enemy);
            }

            Debug.Log($"[EnemySpawner] Spawned {prefab.name} at {position}");
            return enemy;
        }

        // ──────────────────── Pool Management ────────────────────

        private GameObjectPool GetOrCreatePool(GameObject prefab)
        {
            // 如果是 legacy prefab 且有专属池，复用
            if (prefab == _enemyPrefab && _legacyPool != null)
                return _legacyPool;

            // 否则通过 PoolManager 获取/创建（WaveSpawnStrategy 会使用多种 Prefab）
            return PoolManager.Instance.GetPool(prefab, _poolPrewarmCount, _poolMaxSize);
        }

        // ──────────────────── Affix Application ────────────────────

        private void TryApplyAffixes(GameObject enemy)
        {
            if (_possibleAffixes == null || _possibleAffixes.Length == 0) return;
            if (_eliteChance <= 0f) return;

            if (UnityEngine.Random.value > _eliteChance) return;

            var controller = enemy.GetComponent<EnemyAffixController>();
            if (controller == null)
                controller = enemy.AddComponent<EnemyAffixController>();

            controller.ClearAffixes();

            int numAffixes = Mathf.Min(_maxAffixCount, _possibleAffixes.Length);
            var usedIndices = new System.Collections.Generic.HashSet<int>();

            for (int i = 0; i < numAffixes; i++)
            {
                int idx;
                int attempts = 0;
                do
                {
                    idx = UnityEngine.Random.Range(0, _possibleAffixes.Length);
                    attempts++;
                }
                while (usedIndices.Contains(idx) && attempts < 20);

                if (usedIndices.Contains(idx)) break;
                usedIndices.Add(idx);

                if (_possibleAffixes[idx] != null)
                    controller.ApplyAffix(_possibleAffixes[idx]);
            }
        }

        // ──────────────────── Death Handling ────────────────────

        private void HandleEnemyDeath(GameObject enemy)
        {
            Debug.Log($"[EnemySpawner] Enemy died: {enemy.name}");

            _strategy?.OnEnemyDied(enemy);

            // 检查策略是否完成
            if (_strategy != null && _strategy.IsEncounterComplete)
            {
                OnEncounterComplete?.Invoke();
            }
        }

        // ──────────────────── Reset ────────────────────

        /// <summary>
        /// Reset the spawner and its strategy. Used by Room.ResetEnemies() for respawn.
        /// </summary>
        public void ResetSpawner()
        {
            _strategy?.Reset();
            _nextSpawnIndex = 0;
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_spawnPoints == null) return;

            Gizmos.color = Color.green;
            foreach (var point in _spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
#endif
    }
}
