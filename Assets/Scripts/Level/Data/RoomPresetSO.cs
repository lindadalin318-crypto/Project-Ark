using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Room preset template defining default configuration for a room type.
    /// Used by RoomFactory to create fully-configured Room GameObjects in one click.
    /// </summary>
    [CreateAssetMenu(fileName = "New Room Preset", menuName = "ProjectArk/Level/Room Preset")]
    public class RoomPresetSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable name for this preset.")]
        [SerializeField] private string _presetName = "New Preset";

        [Tooltip("Brief description of this preset's intended use.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Room Configuration")]
        [Tooltip("Default room type for rooms created from this preset.")]
        [SerializeField] private RoomType _roomType = RoomType.Normal;

        [Tooltip("Default room size (width, height) in world units.")]
        [SerializeField] private Vector2 _defaultSize = new Vector2(20, 15);

        [Header("Spawn Points")]
        [Tooltip("Number of spawn points to create inside the room.")]
        [Min(0)]
        [SerializeField] private int _spawnPointCount = 4;

        [Header("Combat")]
        [Tooltip("If true, creates an ArenaController component (for Arena/Boss rooms).")]
        [SerializeField] private bool _includeArenaController;

        [Tooltip("If true, creates an EnemySpawner child object.")]
        [SerializeField] private bool _includeEnemySpawner;

        [Tooltip("Default EncounterSO to assign (optional). Can be changed later.")]
        [SerializeField] private EncounterSO _defaultEncounter;

        [Header("Visual")]
        [Tooltip("Optional thumbnail preview for the preset picker UI.")]
        [SerializeField] private Texture2D _thumbnail;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Preset name. </summary>
        public string PresetName => _presetName;

        /// <summary> Description. </summary>
        public string Description => _description;

        /// <summary> Default room type. </summary>
        public RoomType RoomTypeValue => _roomType;

        /// <summary> Default size. </summary>
        public Vector2 DefaultSize => _defaultSize;

        /// <summary> Number of spawn points. </summary>
        public int SpawnPointCount => _spawnPointCount;

        /// <summary> Whether to include ArenaController. </summary>
        public bool IncludeArenaController => _includeArenaController;

        /// <summary> Whether to include EnemySpawner. </summary>
        public bool IncludeEnemySpawner => _includeEnemySpawner;

        /// <summary> Default encounter configuration. </summary>
        public EncounterSO DefaultEncounter => _defaultEncounter;

        /// <summary> Thumbnail preview. </summary>
        public Texture2D Thumbnail => _thumbnail;
    }
}
