using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Lightweight room metadata ScriptableObject.
    /// Only stores non-spatial data (ID, display name, floor level, map icon, pacing node type,
    /// legacy room type, encounter reference).
    /// Spatial data (bounds, doors, spawn points, tilemap) lives on the Room MonoBehaviour in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "New Room", menuName = "ProjectArk/Level/Room")]
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

        [Tooltip("Legacy gameplay room category — still used by current runtime encounter logic during migration.")]
        [SerializeField] private RoomType _type = RoomType.Normal;

        [Tooltip("Room pacing role in the explicit world graph / level grammar.")]
        [SerializeField] private RoomNodeType _nodeType = RoomNodeType.Transit;

        [Tooltip("If enabled, NodeType is derived from the legacy RoomType mapping for backward compatibility.")]
        [SerializeField] private bool _useLegacyTypeMapping = true;

        [Header("Map")]
        [Tooltip("Icon displayed on the minimap. Leave null for default.")]
        [SerializeField] private Sprite _mapIcon;

        [Header("Audio")]
        [Tooltip("Optional ambient/BGM music override for this room. Used for per-room/per-floor music crossfade.")]
        [SerializeField] private AudioClip _ambientMusic;

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

        /// <summary> Legacy room type kept for migration-safe runtime gameplay logic. </summary>
        public RoomType Type => _type;

        /// <summary> Room pacing node type used by WorldGraph and editor visualization. </summary>
        public RoomNodeType NodeType => _useLegacyTypeMapping ? MapLegacyTypeToNodeType(_type) : _nodeType;

        /// <summary> Explicit authored node type stored on the asset. </summary>
        public RoomNodeType ExplicitNodeType => _nodeType;

        /// <summary> Node type resolved purely from the legacy RoomType mapping. </summary>
        public RoomNodeType LegacyMappedNodeType => MapLegacyTypeToNodeType(_type);

        /// <summary> Whether NodeType is currently being derived from the legacy RoomType. </summary>
        public bool UseLegacyTypeMapping => _useLegacyTypeMapping;

        /// <summary> Minimap icon (nullable). </summary>
        public Sprite MapIcon => _mapIcon;

        /// <summary> Optional ambient music override (nullable). </summary>
        public AudioClip AmbientMusic => _ambientMusic;

        /// <summary> Enemy encounter configuration (nullable for non-combat rooms). </summary>
        public EncounterSO Encounter => _encounter;

        /// <summary> Whether this room has an enemy encounter configured. </summary>
        public bool HasEncounter => _encounter != null && _encounter.WaveCount > 0;

        /// <summary>
        /// Maps the legacy runtime-oriented RoomType to the new pacing-oriented RoomNodeType.
        /// This preserves existing room behavior while the project incrementally migrates authoring data.
        /// </summary>
        public static RoomNodeType MapLegacyTypeToNodeType(RoomType type)
        {
            switch (type)
            {
                case RoomType.Safe:
                    return RoomNodeType.Safe;
                case RoomType.Arena:
                    return RoomNodeType.Resolution;
                case RoomType.Boss:
                    return RoomNodeType.Boss;
                case RoomType.Corridor:
                    return RoomNodeType.Transit;
                case RoomType.Shop:
                    return RoomNodeType.Reward;
                case RoomType.Hub:
                    return RoomNodeType.Hub;
                case RoomType.Gate:
                    return RoomNodeType.Threshold;
                case RoomType.Normal:
                default:
                    return RoomNodeType.Transit;
            }
        }
    }
}
