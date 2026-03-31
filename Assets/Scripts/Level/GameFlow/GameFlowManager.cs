using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Core.Save;
using ProjectArk.Ship;
using ProjectArk.Heat;

namespace ProjectArk.Level
{
    /// <summary>
    /// Top-level game flow manager handling death, respawn, and scene-level transitions.
    /// Subscribes to ShipHealth.OnDeath and orchestrates the full death→respawn sequence.
    /// 
    /// Place on a persistent manager GameObject. Delegates all fade effects to DoorTransitionController.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Death Timing")]
        [Tooltip("Fade to black duration on death (seconds).")]
        [SerializeField] private float _deathFadeDuration = 0.5f;

        [Tooltip("How long to stay on black screen before respawn (milliseconds).")]
        [SerializeField] private int _deathHoldMs = 1000;

        [Tooltip("Fade in duration after respawn (seconds).")]
        [SerializeField] private float _respawnFadeDuration = 0.5f;

        [Header("Audio")]
        [Tooltip("Sound effect played on player death.")]
        [SerializeField] private AudioClip _deathSFX;

        [Tooltip("Sound effect played on respawn.")]
        [SerializeField] private AudioClip _respawnSFX;

        [Header("Save")]
        [Tooltip("Save slot index.")]
        [SerializeField] private int _saveSlot = 0;

        // ──────────────────── Runtime State ────────────────────

        private CancellationTokenSource _respawnCts;
        private bool _isRespawning;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            // Load saved progress on scene start (distributes to all subsystems)
            var saveBridge = ServiceLocator.Get<SaveBridge>();
            if (saveBridge != null)
            {
                saveBridge.LoadAll();
            }

            // Subscribe to player death
            var health = ServiceLocator.Get<ShipHealth>();
            if (health != null)
            {
                health.OnDeath += HandlePlayerDeath;
            }
            else
            {
                Debug.LogWarning("[GameFlowManager] ShipHealth not found — death/respawn won't work.");
            }
        }

        private void OnDestroy()
        {
            CancelRespawn();

            var health = ServiceLocator.Get<ShipHealth>();
            if (health != null)
                health.OnDeath -= HandlePlayerDeath;

            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Death Handler ────────────────────

        private void HandlePlayerDeath()
        {
            if (_isRespawning) return;
            ExecuteRespawnSequence().Forget();
        }

        // ──────────────────── Respawn Sequence ────────────────────

        private async UniTaskVoid ExecuteRespawnSequence()
        {
            CancelRespawn();
            _respawnCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _respawnCts.Token, destroyCancellationToken);
            var token = linkedCts.Token;

            _isRespawning = true;

            try
            {
                // ── 1. Disable player input ──
                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                    inputHandler.enabled = false;

                // ── 2. Death SFX ──
                var audio = ServiceLocator.Get<AudioManager>();
                if (audio != null && _deathSFX != null)
                    audio.PlaySFX2D(_deathSFX);

                // ── 3. Fade to black ──
                // Require DoorTransitionController for fade — no silent fallback.
                var fadeController = ServiceLocator.TryGet<DoorTransitionController>();
                if (fadeController == null || fadeController.FadeImage == null)
                {
                    Debug.LogError("[GameFlowManager] DoorTransitionController (or its FadeImage) not found. " +
                                   "Death fade will be skipped. Ensure DoorTransitionController is registered.");
                }
                else
                {
                    await fadeController.FadeOutAsync(_deathFadeDuration, token);
                }

                // ── 4. Hold on black screen ──
                await UniTask.Delay(_deathHoldMs, cancellationToken: token);

                // ── 5. Get respawn position ──
                var checkpointManager = ServiceLocator.Get<CheckpointManager>();
                var roomManager = ServiceLocator.Get<RoomManager>();

                Vector3 respawnPos = Vector3.zero;
                Room checkpointRoom = null;

                if (checkpointManager != null)
                {
                    respawnPos = checkpointManager.GetRespawnPosition();
                    checkpointRoom = checkpointManager.GetCheckpointRoom();
                }
                else
                {
                    Debug.LogWarning("[GameFlowManager] No CheckpointManager found. Respawning at origin.");
                }

                // ── 6. Teleport player ──
                if (inputHandler != null)
                {
                    inputHandler.transform.position = respawnPos;

                    // 清零速度（防止残留惯性）
                    var rb = inputHandler.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                }

                // ── 7. Switch to checkpoint room ──
                if (roomManager != null && checkpointRoom != null)
                {
                    roomManager.EnterRoom(checkpointRoom);
                    ServiceLocator.TryGet<CameraDirector>()?.ClearAllTriggers();
                }

                // ── 8. Reset player state ──
                var health = ServiceLocator.Get<ShipHealth>();
                if (health != null) health.ResetHealth();

                var heat = ServiceLocator.Get<HeatSystem>();
                if (heat != null) heat.ResetHeat();

                // ── 9. Reset current room enemies ──
                if (roomManager != null && roomManager.CurrentRoom != null)
                {
                    roomManager.CurrentRoom.ResetEnemies();
                }

                // ── 10. Respawn SFX ──
                if (audio != null && _respawnSFX != null)
                    audio.PlaySFX2D(_respawnSFX);

                // ── 11. Fade in ──
                var fadeControllerIn = ServiceLocator.TryGet<DoorTransitionController>();
                if (fadeControllerIn != null && fadeControllerIn.FadeImage != null)
                {
                    await fadeControllerIn.FadeInAsync(_respawnFadeDuration, token);
                }
                else
                {
                    Debug.LogError("[GameFlowManager] DoorTransitionController (or its FadeImage) not found. " +
                                   "Respawn fade-in will be skipped.");
                }

                // ── 12. Re-enable input ──
                if (inputHandler != null)
                    inputHandler.enabled = true;

                // ── 13. Save ──
                var saveBridgeRef = ServiceLocator.Get<SaveBridge>();
                if (saveBridgeRef != null)
                {
                    saveBridgeRef.SaveAll();
                }
                else
                {
                    var saveData = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
                    SaveManager.Save(saveData, _saveSlot);
                }

                Debug.Log("[GameFlowManager] Respawn complete.");
            }
            catch (System.OperationCanceledException)
            {
                // Respawn cancelled — restore clean state
                var fadeControllerCancel = ServiceLocator.TryGet<DoorTransitionController>();
                if (fadeControllerCancel?.FadeImage != null)
                {
                    fadeControllerCancel.FadeImage.color = new Color(0f, 0f, 0f, 0f);
                    fadeControllerCancel.FadeImage.raycastTarget = false;
                }

                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                    inputHandler.enabled = true;
            }
            finally
            {
                _isRespawning = false;
                linkedCts.Dispose();
            }
        }

        // ──────────────────── Cancellation ────────────────────

        private void CancelRespawn()
        {
            if (_respawnCts != null)
            {
                _respawnCts.Cancel();
                _respawnCts.Dispose();
                _respawnCts = null;
            }
        }
    }
}
