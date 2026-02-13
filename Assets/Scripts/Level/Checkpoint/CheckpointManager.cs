using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Manages checkpoint activation and respawn position tracking.
    /// Registers with ServiceLocator. Consumed by GameFlowManager (death/respawn)
    /// and Save system.
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Save")]
        [Tooltip("Save slot index used for auto-saving on checkpoint activation.")]
        [SerializeField] private int _saveSlot = 0;

        // ──────────────────── Runtime State ────────────────────

        private Checkpoint _activeCheckpoint;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> The currently active checkpoint (last activated). </summary>
        public Checkpoint ActiveCheckpoint => _activeCheckpoint;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Set the given checkpoint as the active respawn point.
        /// Called by Checkpoint.Activate(). Broadcasts LevelEvents, triggers save.
        /// </summary>
        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || checkpoint.Data == null) return;

            // Deactivate previous
            if (_activeCheckpoint != null && _activeCheckpoint != checkpoint)
            {
                _activeCheckpoint.SetActivated(false);
            }

            _activeCheckpoint = checkpoint;
            _activeCheckpoint.SetActivated(true);

            // Broadcast event
            LevelEvents.RaiseCheckpointActivated(checkpoint.Data.CheckpointID);

            // Persist
            SaveProgress(checkpoint);

            Debug.Log($"[CheckpointManager] Active checkpoint: {checkpoint.Data.CheckpointID}");
        }

        /// <summary>
        /// Get the world position to respawn at. Returns Vector3.zero if no checkpoint activated.
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            if (_activeCheckpoint != null)
                return _activeCheckpoint.SpawnPosition;

            Debug.LogWarning("[CheckpointManager] No active checkpoint! Returning Vector3.zero.");
            return Vector3.zero;
        }

        /// <summary>
        /// Get the Room containing the active checkpoint.
        /// Walks up the hierarchy to find the Room component.
        /// Returns null if no checkpoint or not parented under a Room.
        /// </summary>
        public Room GetCheckpointRoom()
        {
            if (_activeCheckpoint == null) return null;
            return _activeCheckpoint.GetComponentInParent<Room>();
        }

        // ──────────────────── Save Integration ────────────────────

        private void SaveProgress(Checkpoint checkpoint)
        {
            // Delegate to SaveBridge for centralized save (collects all subsystems)
            var saveBridge = ServiceLocator.Get<SaveBridge>();
            if (saveBridge != null)
            {
                saveBridge.SaveAll();
            }
            else
            {
                // Fallback: direct partial save if SaveBridge not available
                var data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
                data.PlayerState.LastCheckpointID = checkpoint.Data.CheckpointID;
                data.PlayerState.PositionX = checkpoint.SpawnPosition.x;
                data.PlayerState.PositionY = checkpoint.SpawnPosition.y;
                SaveManager.Save(data, _saveSlot);
            }
        }
    }
}
