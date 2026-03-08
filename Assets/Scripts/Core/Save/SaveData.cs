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
    /// Supports multi-slot format (Loadouts list) with backward-compatible legacy fields.
    /// </summary>
    [Serializable]
    public class StarChartSaveData
    {
        /// <summary> Multi-slot loadout data (new format, 3 slots). </summary>
        public List<LoadoutSlotSaveData> Loadouts = new();

        // --- Legacy single-slot fields (kept for migration from old saves) ---

        [Obsolete("Use Loadouts list instead. Kept for save migration only.")]
        public TrackSaveData PrimaryTrack = new();

        [Obsolete("Use Loadouts list instead. Kept for save migration only.")]
        public TrackSaveData SecondaryTrack = new();

        [Obsolete("Use Loadouts list instead. Kept for save migration only.")]
        public string LightSailID;

        [Obsolete("Use Loadouts list instead. Kept for save migration only.")]
        public List<string> SatelliteIDs = new();
    }

    /// <summary>
    /// Serialization data for a single loadout slot.
    /// </summary>
    [Serializable]
    public class LoadoutSlotSaveData
    {
        public TrackSaveData PrimaryTrack   = new();
        public TrackSaveData SecondaryTrack = new();
        public string LightSailID;

        /// <summary>
        /// Number of unlocked columns in the SAIL layer (1–4).
        /// Default 2 (initial state = 2 cols × 1 row). Old saves without this field will
        /// deserialize to 0 via JsonUtility; callers must clamp to ≥ 2.
        /// </summary>
        public int SailLayerCols = 2;

        /// <summary>
        /// Number of unlocked rows in the SAIL layer (1–4).
        /// Default 1. Old saves without this field will deserialize to 0; callers must clamp to ≥ 1.
        /// </summary>
        public int SailLayerRows = 1;

        /// <summary>
        /// Legacy slot-level satellite IDs. Kept for migration from old saves only.
        /// New saves store satellite IDs inside each TrackSaveData.
        /// </summary>
        [Obsolete("Satellites are now stored per-track inside TrackSaveData.SatelliteIDs. " +
                  "This field is kept for automatic migration of old saves only.")]
        public List<string> SatelliteIDs = new();
    }

    /// <summary>
    /// Single weapon track serialization (core + prism IDs + satellite IDs + unlocked column counts).
    /// </summary>
    [Serializable]
    public class TrackSaveData
    {
        /// <summary> Equipped Star Core unique IDs (by DisplayName or UniqueID). </summary>
        public List<string> CoreIDs = new();

        /// <summary> Equipped Prism unique IDs. </summary>
        public List<string> PrismIDs = new();

        /// <summary> Equipped Satellite unique IDs for this track (Per-Track). </summary>
        public List<string> SatelliteIDs = new();

        /// <summary>
        /// Number of unlocked columns in the Core layer (1–4).
        /// Default 2 (initial state = 2 cols × 1 row). Old saves without this field
        /// will deserialize to 0 via JsonUtility; callers must clamp to ≥ 2.
        /// </summary>
        public int CoreLayerCols = 2;

        /// <summary>
        /// Number of unlocked rows in the Core layer (1–4).
        /// Default 1. Old saves without this field will deserialize to 0; callers must clamp to ≥ 1.
        /// </summary>
        public int CoreLayerRows = 1;

        /// <summary>
        /// Number of unlocked columns in the Prism layer (1–4).
        /// Default 2 (initial state = 2 cols × 1 row). Old saves without this field
        /// will deserialize to 0 via JsonUtility; callers must clamp to ≥ 2.
        /// </summary>
        public int PrismLayerCols = 2;

        /// <summary>
        /// Number of unlocked rows in the Prism layer (1–4).
        /// Default 1. Old saves without this field will deserialize to 0; callers must clamp to ≥ 1.
        /// </summary>
        public int PrismLayerRows = 1;

        /// <summary>
        /// Number of unlocked columns in the SAT layer (1–4).
        /// Default 2 (initial state = 2 cols × 1 row). Old saves without this field
        /// will deserialize to 0 via JsonUtility; callers must clamp to ≥ 2.
        /// </summary>
        public int SatLayerCols = 2;

        /// <summary>
        /// Number of unlocked rows in the SAT layer (1–4).
        /// Default 1. Old saves without this field will deserialize to 0; callers must clamp to ≥ 1.
        /// </summary>
        public int SatLayerRows = 1;
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

        /// <summary> World clock elapsed time in seconds within current cycle. </summary>
        public float WorldClockTime;

        /// <summary> World clock completed cycle count. </summary>
        public int WorldClockCycle;

        /// <summary> Current world phase index (e.g., 0=Radiation, 1=Calm, 2=Storm, 3=Silence). </summary>
        public int CurrentPhaseIndex;

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
