using System;

namespace ProjectArk.Core
{
    /// <summary>
    /// Global static level events. Lives in Core so any assembly
    /// (Level, UI, Save, etc.) can subscribe/publish without circular dependencies.
    /// Mirrors CombatEvents pattern.
    /// </summary>
    public static class LevelEvents
    {
        // ──────────────────── Room Events ────────────────────

        /// <summary>
        /// Broadcast when the player enters a new room.
        /// Params: roomID.
        /// Published by RoomManager, consumed by UI/Save/AudioManager.
        /// </summary>
        public static event Action<string> OnRoomEntered;

        public static void RaiseRoomEntered(string roomID)
        {
            OnRoomEntered?.Invoke(roomID);
        }

        /// <summary>
        /// Broadcast when the player exits a room.
        /// Params: roomID.
        /// Published by RoomManager.
        /// </summary>
        public static event Action<string> OnRoomExited;

        public static void RaiseRoomExited(string roomID)
        {
            OnRoomExited?.Invoke(roomID);
        }

        /// <summary>
        /// Broadcast when all enemies in a room are defeated.
        /// Params: roomID.
        /// Published by RoomManager, consumed by Door (unlock Locked_Combat).
        /// </summary>
        public static event Action<string> OnRoomCleared;

        public static void RaiseRoomCleared(string roomID)
        {
            OnRoomCleared?.Invoke(roomID);
        }

        // ──────────────────── Boss Events ────────────────────

        /// <summary>
        /// Broadcast when a boss is defeated.
        /// Params: bossID.
        /// Published by BossController (future), consumed by WorldProgressManager / Door / Save.
        /// </summary>
        public static event Action<string> OnBossDefeated;

        public static void RaiseBossDefeated(string bossID)
        {
            OnBossDefeated?.Invoke(bossID);
        }

        // ──────────────────── Checkpoint Events ────────────────────

        /// <summary>
        /// Broadcast when a checkpoint is activated.
        /// Params: checkpointID.
        /// Published by CheckpointManager, consumed by Save/UI.
        /// </summary>
        public static event Action<string> OnCheckpointActivated;

        public static void RaiseCheckpointActivated(string checkpointID)
        {
            OnCheckpointActivated?.Invoke(checkpointID);
        }

        // ──────────────────── Map Events ────────────────────

        /// <summary>
        /// Broadcast when a room is visited for the first time (distinct from OnRoomEntered which fires every entry).
        /// Params: roomID.
        /// Published by MinimapManager, consumed by UI/Save.
        /// </summary>
        public static event Action<string> OnRoomFirstVisit;

        public static void RaiseRoomFirstVisit(string roomID)
        {
            OnRoomFirstVisit?.Invoke(roomID);
        }

        /// <summary>
        /// Broadcast when the player changes floor level.
        /// Params: newFloorLevel.
        /// Published by RoomManager, consumed by MinimapManager/MapPanel.
        /// </summary>
        public static event Action<int> OnFloorChanged;

        public static void RaiseFloorChanged(int newFloor)
        {
            OnFloorChanged?.Invoke(newFloor);
        }

        // ──────────────────── World Progress Events ────────────────────

        /// <summary>
        /// Broadcast when the world progress stage changes (irreversible milestone).
        /// Params: newStage index.
        /// Published by WorldProgressManager, consumed by Door/WorldEventTrigger/Save.
        /// </summary>
        public static event Action<int> OnWorldStageChanged;

        public static void RaiseWorldStageChanged(int newStage)
        {
            OnWorldStageChanged?.Invoke(newStage);
        }
    }
}
