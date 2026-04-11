using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.Core.Audio;

namespace ProjectArk.Level
{
    /// <summary>
    /// Orchestrator for Arena and Boss room combat encounters.
    /// Attach to the same GameObject as the Room component.
    /// 
    /// Flow: Player enters → lock doors → alarm SFX → delay → ask Room to start waves
    ///       → all waves cleared → victory delay → reward / clear notification.
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
        private bool _encounterActive;
        private CancellationTokenSource _encounterCts;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _room = GetComponent<Room>();
        }

        private void OnDestroy()
        {
            CancelEncounterFlow();
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Begin the arena encounter. Called through Room.ActivateEnemies().
        /// Does nothing if already active or room is Cleared.
        /// </summary>
        public void BeginEncounter()
        {
            if (_encounterActive) return;
            if (_room == null || _room.State == RoomState.Cleared) return;

            if (!_room.HasRoomOwnedEncounterSetup)
            {
                Debug.LogError($"[ArenaController] {_room.RoomID}: Room-owned encounter setup is incomplete. Ensure RoomSO Encounter and EnemySpawner are configured.");
                return;
            }

            CancelEncounterFlow();
            _encounterCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunEncounterSequence(_encounterCts.Token).Forget();
        }

        /// <summary>
        /// Called by Room when this room is no longer current.
        /// Cancels pending pre/post delays so the arena cannot keep advancing off-room.
        /// </summary>
        public void HandleRoomExit()
        {
            CancelEncounterFlow();
            _encounterActive = false;
        }

        /// <summary>
        /// Called by Room reset flows (death/respawn).
        /// </summary>
        public void ResetEncounter()
        {
            HandleRoomExit();
        }

        // ──────────────────── Encounter Sequence ────────────────────

        private async UniTaskVoid RunEncounterSequence(CancellationToken token)
        {
            _encounterActive = true;

            // 1. 锁门
            _room.LockAllDoors(DoorState.Locked_Combat);
            Debug.Log($"[ArenaController] {_room.RoomID}: Doors locked — encounter starting!");

            // 2. 播放警报音效
            PlaySFX(_alarmSFX);

            // 3. 等待预遭遇延迟
            if (_preEncounterDelay > 0f)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_preEncounterDelay),
                        cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    _encounterActive = false;
                    CompleteEncounterFlow();
                    return;
                }
            }

            // 4. 由 Room 统一启动 room-owned encounter
            if (!_room.StartRoomOwnedEncounter(HandleWavesCleared))
            {
                Debug.LogError($"[ArenaController] {_room.RoomID}: Failed to start room-owned encounter.");
                _room.UnlockCombatDoors();
                _encounterActive = false;
                CompleteEncounterFlow();
                return;
            }

            Debug.Log($"[ArenaController] {_room.RoomID}: Room-owned encounter started via ArenaController.");
        }

        private void HandleWavesCleared()
        {
            if (!_encounterActive)
            {
                return;
            }

            var token = _encounterCts != null ? _encounterCts.Token : destroyCancellationToken;
            RunPostClearSequence(token).Forget();
        }

        private async UniTaskVoid RunPostClearSequence(CancellationToken token)
        {
            Debug.Log($"[ArenaController] {_room.RoomID}: All waves cleared!");

            // 1. 等待清除后延迟（让最后的死亡动画/特效播完）
            if (_postClearDelay > 0f)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_postClearDelay),
                        cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    _encounterActive = false;
                    CompleteEncounterFlow();
                    return;
                }
            }

            // 2. 播放胜利音效
            PlaySFX(_victorySFX);

            // 3. 由 RoomManager 统一确权 room cleared 与 combat door unlock。
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.NotifyRoomCleared(_room);
            }
            else
            {
                Debug.LogError($"[ArenaController] {_room.RoomID}: RoomManager not found when clearing arena.");
                _room.UnlockCombatDoors();
            }

            // 4. 生成奖励（如有配置）
            SpawnReward();

            _encounterActive = false;
            CompleteEncounterFlow();
        }

        // ──────────────────── Reward ────────────────────

        private void SpawnReward()
        {
            if (_rewardPrefab == null) return;

            Vector3 spawnPos = transform.position + _rewardOffset;

            // Use PoolManager to avoid Instantiate in combat (architecture principle #4)
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                var pool = poolManager.GetPool(_rewardPrefab);
                pool.Get(spawnPos, Quaternion.identity);
                Debug.Log($"[ArenaController] {_room.RoomID}: Reward spawned (pooled) at {spawnPos}");
            }
            else
            {
                // Fallback: PoolManager not available — log error and use Instantiate as last resort
                Debug.LogError("[ArenaController] PoolManager not found. Falling back to Instantiate for reward spawn. " +
                               "Ensure PoolManager is registered in ServiceLocator.");
                Instantiate(_rewardPrefab, spawnPos, Quaternion.identity);
            }
        }

        // ──────────────────── Flow Helpers ────────────────────

        private void CancelEncounterFlow()
        {
            if (_encounterCts == null)
            {
                return;
            }

            _encounterCts.Cancel();
            _encounterCts.Dispose();
            _encounterCts = null;
        }

        private void CompleteEncounterFlow()
        {
            if (_encounterCts == null)
            {
                return;
            }

            _encounterCts.Dispose();
            _encounterCts = null;
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
