using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Legacy loop-based spawn strategy: maintains a fixed number of alive enemies,
    /// respawning after a delay when one dies. Never completes (IsEncounterComplete = false).
    /// Encapsulates the original EnemySpawner behavior for backward compatibility.
    /// </summary>
    public class LoopSpawnStrategy : ISpawnStrategy
    {
        private readonly GameObject _enemyPrefab;
        private readonly int _maxAlive;
        private readonly int _initialSpawnCount;
        private readonly float _spawnInterval;

        private EnemySpawner _spawner;
        private int _aliveCount;
        private CancellationTokenSource _cts;

        public bool IsEncounterComplete => false; // 循环刷怪永不结束

        public LoopSpawnStrategy(GameObject enemyPrefab, int maxAlive, int initialSpawnCount, float spawnInterval)
        {
            _enemyPrefab = enemyPrefab;
            _maxAlive = maxAlive;
            _initialSpawnCount = initialSpawnCount;
            _spawnInterval = spawnInterval;
        }

        public void Initialize(EnemySpawner spawner)
        {
            _spawner = spawner;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            int toSpawn = Mathf.Min(_initialSpawnCount, _maxAlive);
            for (int i = 0; i < toSpawn; i++)
            {
                _spawner.SpawnFromPool(_enemyPrefab);
                _aliveCount++;
            }
        }

        public void OnEnemyDied(GameObject enemy)
        {
            _aliveCount--;

            if (_aliveCount < _maxAlive)
            {
                RespawnAfterDelay().Forget();
            }
        }

        public void Reset()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _aliveCount = 0;
        }

        private async UniTaskVoid RespawnAfterDelay()
        {
            try
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(_spawnInterval),
                    cancellationToken: _cts.Token);

                if (_aliveCount < _maxAlive)
                {
                    _spawner.SpawnFromPool(_enemyPrefab);
                    _aliveCount++;
                }
            }
            catch (System.OperationCanceledException) { }
        }
    }
}
