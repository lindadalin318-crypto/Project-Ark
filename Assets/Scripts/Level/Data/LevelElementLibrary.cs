using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Library of level elements that can be placed in the level designer.
    /// Contains prefabs for walls, crates, doors, checkpoints, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelElementLibrary", menuName = "ProjectArk/Level/Level Element Library")]
    public class LevelElementLibrary : ScriptableObject
    {
        [Header("Room Elements")]
        [Tooltip("Prefab for a room template (contains Room component, collider, etc.)")]
        [SerializeField] private GameObject _roomTemplate;

        [Header("Wall Elements")]
        [Tooltip("Prefab for a basic wall tile")]
        [SerializeField] private GameObject _wallBasic;

        [Tooltip("Prefab for a corner wall tile")]
        [SerializeField] private GameObject _wallCorner;

        [Header("Prop Elements")]
        [Tooltip("Prefab for a wooden crate")]
        [SerializeField] private GameObject _crateWooden;

        [Tooltip("Prefab for a metal crate")]
        [SerializeField] private GameObject _crateMetal;

        [Header("Door Elements")]
        [Tooltip("Prefab for a basic door (contains Door component)")]
        [SerializeField] private GameObject _doorBasic;

        [Header("Checkpoint Elements")]
        [Tooltip("Prefab for a checkpoint (contains Checkpoint component)")]
        [SerializeField] private GameObject _checkpoint;

        [Header("Spawn Points")]
        [Tooltip("Prefab for a player spawn point")]
        [SerializeField] private GameObject _playerSpawnPoint;

        [Tooltip("Prefab for an enemy spawn point")]
        [SerializeField] private GameObject _enemySpawnPoint;

        [Header("Hazard Elements")]
        [Tooltip("Prefab for a generic hazard (spikes, etc.)")]
        [SerializeField] private GameObject _hazard;

        // ──────────────────── Public Properties ────────────────────

        public GameObject RoomTemplate => _roomTemplate;
        public GameObject WallBasic => _wallBasic;
        public GameObject WallCorner => _wallCorner;
        public GameObject CrateWooden => _crateWooden;
        public GameObject CrateMetal => _crateMetal;
        public GameObject DoorBasic => _doorBasic;
        public GameObject Checkpoint => _checkpoint;
        public GameObject PlayerSpawnPoint => _playerSpawnPoint;
        public GameObject EnemySpawnPoint => _enemySpawnPoint;
        public GameObject Hazard => _hazard;

        // ──────────────────── Public Methods ────────────────────

        /// <summary>
        /// Gets the prefab for a given element type.
        /// </summary>
        public GameObject GetPrefabForType(ScaffoldElementType type)
        {
            return type switch
            {
                ScaffoldElementType.Wall => _wallBasic,
                ScaffoldElementType.WallCorner => _wallCorner,
                ScaffoldElementType.CrateWooden => _crateWooden,
                ScaffoldElementType.CrateMetal => _crateMetal,
                ScaffoldElementType.Door => _doorBasic,
                ScaffoldElementType.Checkpoint => _checkpoint,
                ScaffoldElementType.PlayerSpawn => _playerSpawnPoint,
                ScaffoldElementType.EnemySpawn => _enemySpawnPoint,
                ScaffoldElementType.Hazard => _hazard,
                _ => null
            };
        }
    }
}
