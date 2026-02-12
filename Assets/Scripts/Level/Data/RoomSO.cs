using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Lightweight room metadata ScriptableObject.
    /// Only stores non-spatial data (ID, display name, floor level, map icon, room type, encounter reference).
    /// Spatial data (bounds, doors, spawn points, tilemap) lives on the Room MonoBehaviour in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "New Room", menuName = "Project Ark/Level/Room")]
    public class RoomSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this room. Used by save system (VisitedRoomIDs) and map.")]
        [SerializeField] private string _roomID;

        [Tooltip("Display name shown on the map and UI.")]
        [SerializeField] private string _displayName;

        [Header("Structure")]
        [Tooltip("Floor level: 0 = surface, -1 = underground, -2 = deep underground, etc.")]
        [SerializeField] private int _floorLevel;

        [Tooltip("Room category — determines gameplay behavior (door locking, encounter logic).")]
        [SerializeField] private RoomType _type = RoomType.Normal;

        [Header("Map")]
        [Tooltip("Icon displayed on the minimap. Leave null for default.")]
        [SerializeField] private Sprite _mapIcon;

        [Header("Combat")]
        [Tooltip("Enemy wave configuration for this room. Null for non-combat rooms.")]
        [SerializeField] private EncounterSO _encounter;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Unique room identifier (save/map key). </summary>
        public string RoomID => _roomID;

        /// <summary> Display name for UI/map. </summary>
        public string DisplayName => _displayName;

        /// <summary> Floor level (0=surface, negative=underground). </summary>
        public int FloorLevel => _floorLevel;

        /// <summary> Room type (Normal/Arena/Boss/Safe). </summary>
        public RoomType Type => _type;

        /// <summary> Minimap icon (nullable). </summary>
        public Sprite MapIcon => _mapIcon;

        /// <summary> Enemy encounter configuration (nullable for non-combat rooms). </summary>
        public EncounterSO Encounter => _encounter;

        /// <summary> Whether this room has an enemy encounter configured. </summary>
        public bool HasEncounter => _encounter != null && _encounter.WaveCount > 0;
    }
}
