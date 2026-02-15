using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;
using ProjectArk.Ship;

namespace ProjectArk.Level
{
    /// <summary>
    /// Centralized save data collection helper. Gathers <see cref="PlayerSaveData"/>
    /// from all subsystems and delegates to <see cref="SaveManager"/>.
    /// 
    /// Called by CheckpointManager, GameFlowManager, and WorldProgressManager
    /// instead of each system individually building partial save data.
    /// 
    /// Registers with ServiceLocator so any system can trigger a save.
    /// </summary>
    public class SaveBridge : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Save")]
        [Tooltip("Save slot index.")]
        [SerializeField] private int _saveSlot = 0;

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
        /// Collect the full player state from all subsystems and save to disk.
        /// </summary>
        public void SaveAll()
        {
            var data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();

            CollectProgressData(data);
            CollectPlayerState(data);

            SaveManager.Save(data, _saveSlot);

            Debug.Log("[SaveBridge] Full save completed.");
        }

        /// <summary>
        /// Load saved data and distribute to all subsystems.
        /// Called by GameFlowManager on scene start.
        /// </summary>
        public void LoadAll()
        {
            var data = SaveManager.Load(_saveSlot);
            if (data == null)
            {
                Debug.Log("[SaveBridge] No save data found — starting fresh.");
                return;
            }

            DistributeProgressData(data);
            DistributePlayerState(data);

            Debug.Log("[SaveBridge] Full load completed.");
        }

        // ──────────────────── Collect (Write to save data) ────────────────────

        private void CollectProgressData(PlayerSaveData data)
        {
            // MinimapManager → visited rooms
            var minimap = ServiceLocator.Get<MinimapManager>();
            if (minimap != null)
            {
                data.Progress.VisitedRoomIDs.Clear();
                foreach (var roomID in minimap.GetVisitedRoomIDs())
                {
                    data.Progress.VisitedRoomIDs.Add(roomID);
                }
            }

            // KeyInventory → flags
            var keyInventory = ServiceLocator.Get<KeyInventory>();
            if (keyInventory != null)
            {
                keyInventory.WriteToSaveData(data.Progress);
            }

            // WorldProgressManager → defeated bosses + world stage
            var worldProgress = ServiceLocator.Get<WorldProgressManager>();
            if (worldProgress != null)
            {
                data.Progress.DefeatedBossIDs.Clear();
                foreach (var bossID in worldProgress.DefeatedBossIDs)
                {
                    data.Progress.DefeatedBossIDs.Add(bossID);
                }
                data.Progress.WorldStage = worldProgress.CurrentWorldStage;
            }

            // WorldClock → time state
            var worldClock = ServiceLocator.Get<WorldClock>();
            if (worldClock != null)
            {
                data.Progress.WorldClockTime = worldClock.CurrentTime;
                data.Progress.WorldClockCycle = worldClock.CycleCount;
            }

            // WorldPhaseManager → current phase
            var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
            if (phaseManager != null)
            {
                data.Progress.CurrentPhaseIndex = phaseManager.CurrentPhaseIndex;
            }
        }

        private void CollectPlayerState(PlayerSaveData data)
        {
            // ShipHealth → HP
            var shipHealth = ServiceLocator.Get<ShipHealth>();
            if (shipHealth != null)
            {
                data.PlayerState.CurrentHP = shipHealth.CurrentHP;
                data.PlayerState.MaxHP = shipHealth.MaxHP;
            }

            // CheckpointManager → checkpoint + position
            var checkpoint = ServiceLocator.Get<CheckpointManager>();
            if (checkpoint != null && checkpoint.ActiveCheckpoint != null &&
                checkpoint.ActiveCheckpoint.Data != null)
            {
                data.PlayerState.LastCheckpointID = checkpoint.ActiveCheckpoint.Data.CheckpointID;
                data.PlayerState.PositionX = checkpoint.ActiveCheckpoint.SpawnPosition.x;
                data.PlayerState.PositionY = checkpoint.ActiveCheckpoint.SpawnPosition.y;
            }
        }

        // ──────────────────── Distribute (Read from save data) ────────────────────

        private void DistributeProgressData(PlayerSaveData data)
        {
            // MinimapManager → visited rooms
            var minimap = ServiceLocator.Get<MinimapManager>();
            if (minimap != null && data.Progress.VisitedRoomIDs != null)
            {
                minimap.ImportVisitedRooms(data.Progress.VisitedRoomIDs);
            }

            // KeyInventory → flags
            var keyInventory = ServiceLocator.Get<KeyInventory>();
            if (keyInventory != null)
            {
                keyInventory.ReadFromSaveData(data.Progress);
            }

            // WorldProgressManager loads its own data in Start() — no need to push here

            // WorldClock → restore time state
            var worldClock = ServiceLocator.Get<WorldClock>();
            if (worldClock != null)
            {
                worldClock.SetTimeExact(data.Progress.WorldClockTime, data.Progress.WorldClockCycle);
            }

            // WorldPhaseManager → restore phase (will be re-evaluated from clock time, but set for immediate accuracy)
            var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
            if (phaseManager != null && data.Progress.CurrentPhaseIndex >= 0)
            {
                phaseManager.SetPhaseIndex(data.Progress.CurrentPhaseIndex);
            }
        }

        private void DistributePlayerState(PlayerSaveData data)
        {
            // ShipHealth → HP (restore to saved HP)
            // Note: ShipHealth.ResetHealth() resets to max; we may need a SetHP method.
            // For now, log a note — the actual HP restore happens in GameFlowManager's respawn.
            Debug.Log($"[SaveBridge] Loaded player state: HP={data.PlayerState.CurrentHP}/{data.PlayerState.MaxHP}, " +
                      $"Checkpoint={data.PlayerState.LastCheckpointID}");
        }
    }
}
