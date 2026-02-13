using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level
{
    /// <summary>
    /// Wave-based spawn strategy driven by EncounterSO data.
    /// Spawns enemies wave-by-wave; each wave starts after the previous wave is fully cleared.
    /// Fires OnEncounterComplete when all waves are defeated.
    /// </summary>
    public class WaveSpawnStrategy : ISpawnStrategy
    {
        private readonly EncounterSO _encounter;
        private EnemySpawner _spawner;

        private int _currentWaveIndex;
        private int _aliveInWave;
        private bool _isComplete;
        private CancellationTokenSource _cts;

        /// <summary> Fired when all waves are defeated. </summary>
        public event Action OnEncounterComplete;

        public bool IsEncounterComplete => _isComplete;

        public WaveSpawnStrategy(EncounterSO encounter)
        {
            _encounter = encounter;
        }

        public void Initialize(EnemySpawner spawner)
        {
            _spawner = spawner;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            _currentWaveIndex = 0;
            _isComplete = false;
            SpawnCurrentWave().Forget();
        }

        public void OnEnemyDied(GameObject enemy)
        {
            _aliveInWave--;

            if (_aliveInWave <= 0 && !_isComplete)
            {
                _currentWaveIndex++;
                if (_currentWaveIndex >= _encounter.WaveCount)
                {
                    // 全部波次完成
                    _isComplete = true;
                    Debug.Log("[WaveSpawnStrategy] All waves cleared!");
                    OnEncounterComplete?.Invoke();
                }
                else
                {
                    // 进入下一波
                    SpawnCurrentWave().Forget();
                }
            }
        }

        public void Reset()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _currentWaveIndex = 0;
            _aliveInWave = 0;
            _isComplete = false;
        }

        private async UniTaskVoid SpawnCurrentWave()
        {
            if (_encounter.Waves == null || _currentWaveIndex >= _encounter.WaveCount)
                return;

            var wave = _encounter.Waves[_currentWaveIndex];

            // 波次间延迟
            if (wave.DelayBeforeWave > 0f)
            {
                try
                {
                    Debug.Log($"[WaveSpawnStrategy] Wave {_currentWaveIndex + 1}/{_encounter.WaveCount} " +
                              $"starting in {wave.DelayBeforeWave}s...");

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(wave.DelayBeforeWave),
                        cancellationToken: _cts.Token);
                }
                catch (OperationCanceledException) { return; }
            }

            // 统计本波总数
            _aliveInWave = wave.TotalEnemyCount;

            Debug.Log($"[WaveSpawnStrategy] Spawning wave {_currentWaveIndex + 1}/{_encounter.WaveCount} " +
                      $"({_aliveInWave} enemies)");

            // 逐 entry 生成
            if (wave.Entries != null)
            {
                foreach (var entry in wave.Entries)
                {
                    if (entry.EnemyPrefab == null)
                    {
                        Debug.LogWarning("[WaveSpawnStrategy] Null prefab in wave entry, skipping.");
                        _aliveInWave -= entry.Count;
                        continue;
                    }

                    for (int i = 0; i < entry.Count; i++)
                    {
                        _spawner.SpawnFromPool(entry.EnemyPrefab);
                    }
                }
            }

            // 安全检查：如果本波实际没有生成任何敌人，直接推进
            if (_aliveInWave <= 0)
            {
                _currentWaveIndex++;
                if (_currentWaveIndex >= _encounter.WaveCount)
                {
                    _isComplete = true;
                    OnEncounterComplete?.Invoke();
                }
                else
                {
                    SpawnCurrentWave().Forget();
                }
            }
        }
    }
}
