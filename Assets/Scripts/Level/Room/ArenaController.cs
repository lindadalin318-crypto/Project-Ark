using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level
{
    /// <summary>
    /// Orchestrator for Arena and Boss room combat encounters.
    /// Attach to the same GameObject as the Room component.
    /// 
    /// Flow: Player enters → lock doors → alarm SFX → delay → start waves
    ///       → all waves cleared → unlock doors → victory SFX → optional reward drop → mark Cleared.
    /// 
    /// Re-entering a Cleared arena does not retrigger the encounter.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class ArenaController : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Timing")]
        [Tooltip("Delay (seconds) between door lock and wave spawn start.")]
        [SerializeField] private float _preEncounterDelay = 1.5f;

        [Tooltip("Delay (seconds) after encounter clear before unlocking doors.")]
        [SerializeField] private float _postClearDelay = 1.0f;

        [Header("Audio")]
        [Tooltip("SFX played when doors lock and encounter begins.")]
        [SerializeField] private AudioClip _alarmSFX;

        [Tooltip("SFX played when encounter is cleared.")]
        [SerializeField] private AudioClip _victorySFX;

        [Header("Rewards")]
        [Tooltip("Optional reward prefab spawned at room center after clear.")]
        [SerializeField] private GameObject _rewardPrefab;

        [Tooltip("Spawn position offset from room center for the reward.")]
        [SerializeField] private Vector3 _rewardOffset = Vector3.zero;

        // ──────────────────── Cached References ────────────────────

        private Room _room;
        private EnemySpawner _spawner;
        private WaveSpawnStrategy _waveStrategy;
        private bool _encounterActive;

        // ──────────────────── Events ────────────────────

        /// <summary> Fired when the arena encounter begins (doors locked). </summary>
        public event Action OnEncounterStarted;

        /// <summary> Fired when the arena encounter is cleared. </summary>
        public event Action OnEncounterCleared;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _room = GetComponent<Room>();
            _spawner = GetComponentInChildren<EnemySpawner>(true);

            if (_spawner == null)
            {
                Debug.LogError($"[ArenaController] {gameObject.name}: No EnemySpawner found in children!");
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Begin the arena encounter. Called by RoomManager.EnterRoom() or externally.
        /// Does nothing if already active or room is Cleared.
        /// </summary>
        public void BeginEncounter()
        {
            if (_encounterActive) return;
            if (_room.State == RoomState.Cleared) return;
            if (_room.Data == null || !_room.Data.HasEncounter) return;
            if (_spawner == null) return;

            RunEncounterSequence().Forget();
        }

        // ──────────────────── Encounter Sequence ────────────────────

        private async UniTaskVoid RunEncounterSequence()
        {
            _encounterActive = true;

            // 1. 锁门
            _room.LockAllDoors(DoorState.Locked_Combat);
            Debug.Log($"[ArenaController] {_room.RoomID}: Doors locked — encounter starting!");

            // 2. 播放警报音效
            PlaySFX(_alarmSFX);

            // 3. 广播遭遇开始事件
            OnEncounterStarted?.Invoke();

            // 4. 等待预遭遇延迟
            if (_preEncounterDelay > 0f)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_preEncounterDelay),
                        cancellationToken: destroyCancellationToken);
                }
                catch (OperationCanceledException) { return; }
            }

            // 5. 创建 WaveSpawnStrategy 并启动
            _waveStrategy = new WaveSpawnStrategy(_room.Data.Encounter);
            _waveStrategy.OnEncounterComplete += HandleWavesCleared;

            _spawner.SetStrategy(_waveStrategy);
            _spawner.StartStrategy();

            Debug.Log($"[ArenaController] {_room.RoomID}: Waves spawning...");
        }

        private void HandleWavesCleared()
        {
            // 取消订阅
            if (_waveStrategy != null)
            {
                _waveStrategy.OnEncounterComplete -= HandleWavesCleared;
                _waveStrategy = null;
            }

            RunPostClearSequence().Forget();
        }

        private async UniTaskVoid RunPostClearSequence()
        {
            Debug.Log($"[ArenaController] {_room.RoomID}: All waves cleared!");

            // 1. 等待清除后延迟（让最后的死亡动画/特效播完）
            if (_postClearDelay > 0f)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_postClearDelay),
                        cancellationToken: destroyCancellationToken);
                }
                catch (OperationCanceledException) { return; }
            }

            // 2. 解锁门
            _room.UnlockCombatDoors();
            Debug.Log($"[ArenaController] {_room.RoomID}: Doors unlocked.");

            // 3. 播放胜利音效
            PlaySFX(_victorySFX);

            // 4. 标记房间为 Cleared
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.NotifyRoomCleared(_room);
            }

            // 5. 生成奖励（如有配置）
            SpawnReward();

            // 6. 广播遭遇清除事件
            OnEncounterCleared?.Invoke();

            _encounterActive = false;
        }

        // ──────────────────── Reward ────────────────────

        private void SpawnReward()
        {
            if (_rewardPrefab == null) return;

            Vector3 spawnPos = transform.position + _rewardOffset;
            Instantiate(_rewardPrefab, spawnPos, Quaternion.identity);

            Debug.Log($"[ArenaController] {_room.RoomID}: Reward spawned at {spawnPos}");
        }

        // ──────────────────── Audio Helper ────────────────────

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null)
            {
                audio.PlaySFX2D(clip);
            }
        }
    }
}
