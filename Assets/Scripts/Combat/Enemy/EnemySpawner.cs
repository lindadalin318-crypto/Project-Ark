using System.Collections;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Manages enemy spawning through object pooling.
    /// Controls spawn timing, alive count, and spawn point selection.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        // ──────────────────── Inspector ────────────────────
        [Header("Enemy Configuration")]
        [Tooltip("The enemy prefab to spawn.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Spawn Points")]
        [Tooltip("Transform positions where enemies can spawn.")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Spawn Settings")]
        [Tooltip("Maximum number of enemies alive at once.")]
        [SerializeField] private int _maxAlive = 3;

        [Tooltip("Time between respawns after an enemy dies (seconds).")]
        [SerializeField] private float _spawnInterval = 5f;

        [Tooltip("Number of enemies to spawn immediately on Start.")]
        [SerializeField] private int _initialSpawnCount = 1;

        [Header("Pool Settings")]
        [Tooltip("Number of instances to pre-warm in the pool.")]
        [SerializeField] private int _poolPrewarmCount = 5;

        [Tooltip("Maximum pool capacity.")]
        [SerializeField] private int _poolMaxSize = 10;

        // ──────────────────── Runtime State ────────────────────
        private GameObjectPool _pool;
        private int _aliveCount;
        private int _nextSpawnIndex;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] No enemy prefab assigned!");
                enabled = false;
                return;
            }

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("[EnemySpawner] No spawn points assigned!");
                enabled = false;
                return;
            }

            // Create the object pool
            _pool = new GameObjectPool(_enemyPrefab, transform, _poolPrewarmCount, _poolMaxSize);

            // Perform initial spawns
            int toSpawn = Mathf.Min(_initialSpawnCount, _maxAlive);
            for (int i = 0; i < toSpawn; i++)
            {
                SpawnEnemy();
            }
        }

        // ──────────────────── Spawning ────────────────────

        /// <summary>
        /// Spawns a single enemy from the pool at the next spawn point.
        /// </summary>
        public void SpawnEnemy()
        {
            if (_aliveCount >= _maxAlive) return;

            // Select spawn point (round-robin)
            Transform spawnPoint = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
            _nextSpawnIndex++;

            Vector3 position = spawnPoint.position;

            // Get enemy from pool (IPoolable.OnGetFromPool is called automatically)
            GameObject enemy = _pool.Get(position, Quaternion.identity);

            // Reset brain to start fresh from IdleState at new position
            var brain = enemy.GetComponent<EnemyBrain>();
            if (brain != null)
            {
                brain.ResetBrain(position);
            }

            // Subscribe to death event for respawn management
            // NOTE: EnemyEntity.OnReturnToPool clears all event subscribers,
            // so we must re-subscribe every time we get from pool.
            var entity = enemy.GetComponent<EnemyEntity>();
            if (entity != null)
            {
                entity.OnDeath += () => OnEnemyDied(enemy);
            }

            _aliveCount++;

            Debug.Log($"[EnemySpawner] Spawned enemy at {position}. Alive: {_aliveCount}/{_maxAlive}");
        }

        // ──────────────────── Death Handling ────────────────────

        private void OnEnemyDied(GameObject enemy)
        {
            _aliveCount--;

            Debug.Log($"[EnemySpawner] Enemy died. Alive: {_aliveCount}/{_maxAlive}");

            // Schedule respawn if below max
            if (_aliveCount < _maxAlive)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(_spawnInterval);

            // Double check we still need to spawn
            if (_aliveCount < _maxAlive)
            {
                SpawnEnemy();
            }
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
