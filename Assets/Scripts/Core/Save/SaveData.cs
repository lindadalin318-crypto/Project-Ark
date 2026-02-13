using System;
using System.Collections.Generic;

namespace ProjectArk.Core.Save
{
    /// <summary>
    /// Root save data container for a single save slot.
    /// Serialized to JSON via JsonUtility.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        /// <summary> Schema version for migration support. </summary>
        public string SaveVersion = "1.0";

        /// <summary> ISO 8601 timestamp of when the save was created. </summary>
        public string Timestamp;

        /// <summary> Star Chart equipment state. </summary>
        public StarChartSaveData StarChart = new();

        /// <summary> Player inventory (owned items). </summary>
        public InventorySaveData Inventory = new();

        /// <summary> World progress (rooms, bosses, abilities, flags). </summary>
        public ProgressSaveData Progress = new();

        /// <summary> Player state at save time (HP, position, checkpoint). </summary>
        public PlayerStateSaveData PlayerState = new();
    }

    /// <summary>
    /// Star Chart loadout serialization.
    /// </summary>
    [Serializable]
    public class StarChartSaveData
    {
        public TrackSaveData PrimaryTrack = new();
        public TrackSaveData SecondaryTrack = new();
        public string LightSailID;
        public List<string> SatelliteIDs = new();
    }

    /// <summary>
    /// Single weapon track serialization (core + prism IDs).
    /// </summary>
    [Serializable]
    public class TrackSaveData
    {
        /// <summary> Equipped Star Core unique IDs (by DisplayName or UniqueID). </summary>
        public List<string> CoreIDs = new();

        /// <summary> Equipped Prism unique IDs. </summary>
        public List<string> PrismIDs = new();
    }

    /// <summary>
    /// Player's collected items.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public List<string> OwnedItemIDs = new();
    }

    /// <summary>
    /// World progress tracking for Metroidvania exploration.
    /// </summary>
    [Serializable]
    public class ProgressSaveData
    {
        /// <summary> Room IDs the player has visited (for minimap reveal). </summary>
        public List<string> VisitedRoomIDs = new();

        /// <summary> Boss IDs that have been defeated. </summary>
        public List<string> DefeatedBossIDs = new();

        /// <summary> Unlocked abilities / traversal upgrades. </summary>
        public List<string> UnlockedAbilityIDs = new();

        /// <summary> Current world progress stage (0 = initial, increments on boss defeats). </summary>
        public int WorldStage;

        /// <summary> Generic key-value flags for quest/story state. </summary>
        public List<SaveFlag> Flags = new();
    }

    /// <summary>
    /// Key-value pair for generic progress flags.
    /// (JsonUtility does not support Dictionary, so we use a list of pairs.)
    /// </summary>
    [Serializable]
    public class SaveFlag
    {
        public string Key;
        public bool Value;

        public SaveFlag() { }
        public SaveFlag(string key, bool value) { Key = key; Value = value; }
    }

    /// <summary>
    /// Player character state at save time.
    /// </summary>
    [Serializable]
    public class PlayerStateSaveData
    {
        public float CurrentHP;
        public float MaxHP;
        public string LastCheckpointID;
        public float PositionX;
        public float PositionY;
    }
}
