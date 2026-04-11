using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level
{
    /// <summary>
    /// Open encounter trigger (Minishoot "EncounterOpen" equivalent).
    /// 
    /// Unlike ArenaController (封门清算), this component:
    /// - Does NOT lock doors when the encounter starts
    /// - Spawns enemies when the player enters the trigger zone
    /// - Despawns remaining enemies after the player leaves (with a grace period)
    /// - Can be re-triggered if the player re-enters and the encounter wasn't fully cleared
    /// - Marks as cleared only when ALL enemies are defeated
    /// 
    /// Place as a child of a Room, or as a standalone trigger zone within a room.
    /// Requires its own BoxCollider2D (trigger) separate from the Room trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class OpenEncounterTrigger : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Encounter Data")]
        [Tooltip("Encounter SO defining waves. Should have Mode = Open.")]
        [SerializeField] private EncounterSO _encounter;

        [Header("Spawner")]
        [Tooltip("EnemySpawner to use for spawning. If null, searches children.")]
        [SerializeField] private EnemySpawner _spawner;

        [Header("Timing")]
        [Tooltip("Grace period (seconds) after player leaves before despawning enemies. " +
                 "Gives the player time to re-engage without losing progress.")]
        [SerializeField] private float _exitGracePeriod = 3f;

        [Header("Behavior")]
        [Tooltip("If true, enemies persist even after player leaves (no despawn). " +
                 "Only cleared by defeating all enemies.")]
        [SerializeField] private bool _persistAfterExit;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private bool _playerInZone;
        private bool _isActive;      // encounter is currently running
        private bool _isCleared;     // all enemies defeated — permanent until room reset
        private WaveSpawnStrategy _waveStrategy;
        private CancellationTokenSource _exitCts;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Whether the encounter is currently active (enemies spawned). </summary>
        public bool IsActive => _isActive;

        /// <summary> Whether all enemies have been cleared. </summary>
        public bool IsCleared => _isCleared;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Auto-find spawner if not assigned
            if (_spawner == null)
            {
                _spawner = GetComponentInChildren<EnemySpawner>(true);
            }

            // Validate trigger collider
            var boxCollider = GetComponent<BoxCollider2D>();
            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[OpenEncounterTrigger] {gameObject.name}: BoxCollider2D was not set as trigger. Auto-fixed.");
            }

            // Validate references
            if (_encounter == null)
            {
                Debug.LogError($"[OpenEncounterTrigger] {gameObject.name}: EncounterSO not assigned!");
            }

            if (_spawner == null)
            {
                Debug.LogError($"[OpenEncounterTrigger] {gameObject.name}: No EnemySpawner found!");
            }
        }

        private void OnDestroy()
        {
            CancelExitTimer();
            CleanupStrategy();
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;

            _playerInZone = true;
            CancelExitTimer();

            if (!_isCleared && !_isActive)
            {
                ActivateEncounter();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;

            _playerInZone = false;

            if (_isActive && !_persistAfterExit)
            {
                StartExitGracePeriod();
            }
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Encounter Control ────────────────────

        private void ActivateEncounter()
        {
            if (_encounter == null || _spawner == null) return;
            if (_isCleared) return;

            CleanupStrategy();
            _isActive = true;

            // Create and start wave strategy
            _waveStrategy = new WaveSpawnStrategy(_encounter);
            _waveStrategy.OnEncounterComplete += HandleEncounterComplete;

            _spawner.SetStrategy(_waveStrategy);
            _spawner.StartStrategy();

            Debug.Log($"[OpenEncounterTrigger] {gameObject.name}: Open encounter activated " +
                      $"({_encounter.WaveCount} waves)");
        }

        private void DeactivateEncounter()
        {
            if (!_isActive) return;

            _isActive = false;

            // Stop future waves and despawn active enemies.
            if (_spawner != null)
            {
                _spawner.StopAndDespawnActiveEnemies();
            }

            CleanupStrategy();

            Debug.Log($"[OpenEncounterTrigger] {gameObject.name}: Open encounter deactivated " +
                      $"(player left zone)");
        }

        private void HandleEncounterComplete()
        {
            _isCleared = true;
            _isActive = false;

            CleanupStrategy();

            // OpenEncounterTrigger only owns its local zone lifecycle.
            // Whole-room cleared authority stays in Room/RoomManager.
            Debug.Log($"[OpenEncounterTrigger] {gameObject.name}: Open encounter cleared!");
        }

        // ──────────────────── Exit Grace Period ────────────────────

        private void StartExitGracePeriod()
        {
            CancelExitTimer();
            _exitCts = new CancellationTokenSource();

            RunExitTimer(_exitCts.Token).Forget();
        }

        private async UniTaskVoid RunExitTimer(CancellationToken token)
        {
            try
            {
                int delayMs = Mathf.RoundToInt(_exitGracePeriod * 1000f);
                await UniTask.Delay(delayMs, cancellationToken: token);

                // Grace period expired and player still not in zone
                if (!_playerInZone && _isActive)
                {
                    DeactivateEncounter();
                }
            }
            catch (OperationCanceledException)
            {
                // Player re-entered or object destroyed — expected
            }
        }

        private void CancelExitTimer()
        {
            if (_exitCts != null)
            {
                _exitCts.Cancel();
                _exitCts.Dispose();
                _exitCts = null;
            }
        }

        // ──────────────────── Cleanup ────────────────────

        private void CleanupStrategy()
        {
            if (_waveStrategy != null)
            {
                _waveStrategy.OnEncounterComplete -= HandleEncounterComplete;
                _waveStrategy.Dispose();
                _waveStrategy = null;
            }
        }

        // ──────────────────── Reset (for death/respawn) ────────────────────

        /// <summary>
        /// Called when the parent room is no longer current.
        /// Force-stops the local encounter lifecycle without upgrading it to whole-room clear.
        /// </summary>
        public void HandleRoomExit()
        {
            CancelExitTimer();
            _playerInZone = false;
            DeactivateEncounter();
        }

        /// <summary>
        /// Reset the encounter to initial state. Called by Room.ResetEnemies() or GameFlowManager.
        /// </summary>
        public void ResetEncounter()
        {
            HandleRoomExit();
            _isCleared = false;

            Debug.Log($"[OpenEncounterTrigger] {gameObject.name}: Encounter reset.");
        }
    }
}
