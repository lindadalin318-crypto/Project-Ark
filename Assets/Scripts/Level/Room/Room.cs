using System;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level
{
    /// <summary>
    /// Runtime representation of a single room in the level.
    /// Handles player entry/exit detection via trigger collider,
    /// holds references to doors and spawn points, and manages room state.
    /// 
    /// Spatial data (bounds, doors, spawn points) lives here on the scene GameObject.
    /// Non-spatial metadata (ID, type, encounter config) lives on the referenced RoomSO.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Room : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Data")]
        [Tooltip("Lightweight metadata SO (ID, name, type, encounter). Drag from Assets/_Data/Level/Rooms/.")]
        [SerializeField] private RoomSO _data;

        [Header("Camera")]
        [Tooltip("Collider defining camera confiner bounds. Use a PolygonCollider2D (non-trigger) on a child object.")]
        [SerializeField] private Collider2D _confinerBounds;

        [Header("Spawn Points")]
        [Tooltip("Positions where enemies can be spawned in this room.")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Variants")]
        [Tooltip("Optional room variants for different world phases (e.g., day/night configs). Leave empty if room doesn't change.")]
        [SerializeField] private RoomVariantSO[] _variants;

        [Tooltip("Environment child GameObjects corresponding to variant indices. Index 0 = first child, etc. Disabled/enabled by variant switching.")]
        [SerializeField] private GameObject[] _variantEnvironments;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player. Used to filter OnTriggerEnter2D.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private RoomState _state = RoomState.Undiscovered;
        private Door[] _doors;
        private EnemySpawner _spawner;
        private WaveSpawnStrategy _waveStrategy;
        private OpenEncounterTrigger[] _openEncounters;
        private DestroyableObject[] _destroyables;
        private RoomVariantSO _activeVariant; // currently active variant (null = default)

        // ──────────────────── Events ────────────────────

        /// <summary> Fired when the player physically enters this room's trigger. </summary>
        public event Action<Room> OnPlayerEntered;

        /// <summary> Fired when the player physically exits this room's trigger. </summary>
        public event Action<Room> OnPlayerExited;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Room metadata SO. </summary>
        public RoomSO Data => _data;

        /// <summary> Unique room ID (from SO). </summary>
        public string RoomID => _data != null ? _data.RoomID : gameObject.name;

        /// <summary> Legacy room type (from SO). </summary>
        public RoomType Type => _data != null ? _data.Type : RoomType.Normal;

        /// <summary> Pacing node type (from SO). </summary>
        public RoomNodeType NodeType => _data != null ? _data.NodeType : RoomNodeType.Transit;

        /// <summary> Camera confiner bounds collider. </summary>
        public Collider2D ConfinerBounds => _confinerBounds;

        /// <summary> Enemy spawn points in this room. </summary>
        public Transform[] SpawnPoints => _spawnPoints;

        /// <summary> All doors belonging to this room. </summary>
        public Door[] Doors => _doors;

        /// <summary> All destroyable objects in this room. </summary>
        public DestroyableObject[] Destroyables => _destroyables;

        /// <summary> Current runtime state. </summary>
        public RoomState State => _state;

        // ──────────────────── Lifecycle ────────────────────

        private const string NAVIGATION_ROOT_NAME = "Navigation";
        private const string ENCOUNTERS_ROOT_NAME = "Encounters";
        private const string ELEMENTS_ROOT_NAME = "Elements";
        private const string LEGACY_SPAWN_POINTS_ROOT_NAME = "SpawnPoints";
        private const string NAMED_SPAWN_POINT_PREFIX = "SpawnPoint_";

        private void Awake()
        {
            CollectSceneReferences();

            // Validate trigger collider
            var boxCollider = GetComponent<BoxCollider2D>();
            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[Room] {gameObject.name}: BoxCollider2D was not set as trigger. Auto-fixed.");
            }

            // Validate data
            if (_data == null)
            {
                Debug.LogError($"[Room] {gameObject.name}: RoomSO reference is missing! Assign a RoomSO asset.");
            }
        }

        private void CollectSceneReferences()
        {
            _doors = CollectComponentsFromPreferredRoot<Door>(NAVIGATION_ROOT_NAME);
            _spawner = FindComponentInPreferredRoot<EnemySpawner>(ENCOUNTERS_ROOT_NAME);
            _openEncounters = CollectComponentsFromPreferredRoot<OpenEncounterTrigger>(ENCOUNTERS_ROOT_NAME);
            _destroyables = CollectComponentsFromPreferredRoot<DestroyableObject>(ELEMENTS_ROOT_NAME);
            _spawnPoints = CollectSpawnPoints();
        }

        private T[] CollectComponentsFromPreferredRoot<T>(string rootName) where T : Component
        {
            var preferredRoot = transform.Find(rootName);
            if (preferredRoot != null)
            {
                var preferredComponents = preferredRoot.GetComponentsInChildren<T>(true);
                if (preferredComponents != null && preferredComponents.Length > 0)
                {
                    return preferredComponents;
                }
            }

            var fallbackComponents = GetComponentsInChildren<T>(true);
            return fallbackComponents ?? Array.Empty<T>();
        }

        private T FindComponentInPreferredRoot<T>(string rootName) where T : Component
        {
            var preferredRoot = transform.Find(rootName);
            if (preferredRoot != null)
            {
                var preferredComponent = preferredRoot.GetComponentInChildren<T>(true);
                if (preferredComponent != null)
                {
                    return preferredComponent;
                }
            }

            return GetComponentInChildren<T>(true);
        }

        private Transform[] CollectSpawnPoints()
        {
            var encounterSpawnRoot = transform.Find($"{ENCOUNTERS_ROOT_NAME}/{LEGACY_SPAWN_POINTS_ROOT_NAME}");
            var encounterSpawnPoints = CollectDirectChildTransforms(encounterSpawnRoot);
            if (encounterSpawnPoints.Length > 0)
            {
                return encounterSpawnPoints;
            }

            var legacySpawnRoot = transform.Find(LEGACY_SPAWN_POINTS_ROOT_NAME);
            var legacySpawnPoints = CollectDirectChildTransforms(legacySpawnRoot);
            if (legacySpawnPoints.Length > 0)
            {
                return legacySpawnPoints;
            }

            var navigationRoot = transform.Find(NAVIGATION_ROOT_NAME);
            if (navigationRoot != null)
            {
                var navigationSpawnPoints = new System.Collections.Generic.List<Transform>();
                CollectNamedSpawnPointsRecursive(navigationRoot, navigationSpawnPoints);
                if (navigationSpawnPoints.Count > 0)
                {
                    return navigationSpawnPoints.ToArray();
                }
            }

            return _spawnPoints ?? Array.Empty<Transform>();
        }

        private static Transform[] CollectDirectChildTransforms(Transform root)
        {
            if (root == null || root.childCount == 0)
            {
                return Array.Empty<Transform>();
            }

            var results = new Transform[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                results[i] = root.GetChild(i);
            }

            return results;
        }

        private static void CollectNamedSpawnPointsRecursive(Transform root, System.Collections.Generic.List<Transform> results)
        {
            if (root == null || results == null) return;

            if (root.name.StartsWith(NAMED_SPAWN_POINT_PREFIX, StringComparison.Ordinal))
            {
                results.Add(root);
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectNamedSpawnPointsRecursive(root.GetChild(i), results);
            }
        }

        private void Start()
        {
            // Subscribe to phase changes for variant support
            if (_variants != null && _variants.Length > 0)
            {
                LevelEvents.OnPhaseChanged += HandlePhaseChanged;

                // Apply initial variant from current phase
                var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
                if (phaseManager != null && phaseManager.CurrentPhaseIndex >= 0)
                {
                    ApplyVariantForPhase(phaseManager.CurrentPhaseIndex);
                }
            }
        }

        private void OnDestroy()
        {
            if (_variants != null && _variants.Length > 0)
            {
                LevelEvents.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            OnPlayerEntered?.Invoke(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            OnPlayerExited?.Invoke(this);
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── State Management ────────────────────

        /// <summary>
        /// Update room state. Called by RoomManager.
        /// </summary>
        public void SetState(RoomState newState)
        {
            if (_state == newState) return;

            var oldState = _state;
            _state = newState;

            Debug.Log($"[Room] {RoomID}: {oldState} → {newState}");
        }

        // ──────────────────── Enemy Activation ────────────────────

        /// <summary>
        /// Activate enemies in this room. If EncounterSO is configured and room is not cleared,
        /// creates a WaveSpawnStrategy and starts it via EnemySpawner.
        /// Otherwise falls back to activating pre-placed child enemies.
        /// Called by RoomManager when player enters the room.
        /// </summary>
        public void ActivateEnemies()
        {
            // 波次刷怪模式：有 EncounterSO（含变体覆盖）且房间未清除且有 Spawner
            var encounter = ActiveEncounter;
            if (encounter != null && _state != RoomState.Cleared && _spawner != null)
            {
                StartWaveEncounter(encounter);
                return;
            }

            // 回退模式：激活预放置的敌人 GameObject
            ActivatePreplacedEnemies();
        }

        /// <summary>
        /// Start a wave-based encounter using the given EncounterSO data.
        /// Supports variant override encounters.
        /// </summary>
        private void StartWaveEncounter(EncounterSO encounter)
        {
            _waveStrategy = new WaveSpawnStrategy(encounter);
            _waveStrategy.OnEncounterComplete += HandleEncounterComplete;

            _spawner.SetStrategy(_waveStrategy);
            _spawner.StartStrategy();

            Debug.Log($"[Room] {RoomID}: Wave encounter started ({encounter.WaveCount} waves)");
        }

        /// <summary>
        /// Fallback: activate pre-placed enemy GameObjects under spawn point transforms.
        /// </summary>
        private void ActivatePreplacedEnemies()
        {
            foreach (var sp in _spawnPoints)
            {
                if (sp == null) continue;
                for (int i = 0; i < sp.childCount; i++)
                {
                    sp.GetChild(i).gameObject.SetActive(true);
                }
            }
        }

        private void HandleEncounterComplete()
        {
            // 清理事件订阅
            if (_waveStrategy != null)
            {
                _waveStrategy.OnEncounterComplete -= HandleEncounterComplete;
            }

            // 通知 RoomManager 房间已清除
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.NotifyRoomCleared(this);
            }

            Debug.Log($"[Room] {RoomID}: Encounter complete → Room cleared!");
        }

        /// <summary>
        /// Deactivate all enemies in this room (disable their GameObjects).
        /// Called by RoomManager when player leaves the room.
        /// </summary>
        public void DeactivateEnemies()
        {
            foreach (var sp in _spawnPoints)
            {
                if (sp == null) continue;
                for (int i = 0; i < sp.childCount; i++)
                {
                    sp.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Reset all enemies in this room to their initial state.
        /// Used by death/respawn system to repopulate the room.
        /// Deactivates all enemies, resets spawner strategy, resets room state, then reactivates.
        /// </summary>
        public void ResetEnemies()
        {
            // 先全部关闭
            DeactivateEnemies();

            // 重置 spawner 策略状态
            if (_spawner != null)
            {
                _spawner.ResetSpawner();
            }

            // 清理旧的 WaveSpawnStrategy 事件
            if (_waveStrategy != null)
            {
                _waveStrategy.OnEncounterComplete -= HandleEncounterComplete;
                _waveStrategy = null;
            }

            // 重置所有 OpenEncounterTrigger
            if (_openEncounters != null)
            {
                foreach (var openEnc in _openEncounters)
                {
                    if (openEnc != null) openEnc.ResetEncounter();
                }
            }

            // 重置房间战斗状态（如果已清除，恢复到 Entered 以允许再次战斗）
            if (_state == RoomState.Cleared)
            {
                _state = RoomState.Entered;
            }

            // 解锁战斗锁定的门
            UnlockCombatDoors();

            // 重新激活
            ActivateEnemies();

            Debug.Log($"[Room] {RoomID}: Enemies reset.");
        }

        // ──────────────────── Variant Support ────────────────────

        private void HandlePhaseChanged(int phaseIndex, string phaseName)
        {
            ApplyVariantForPhase(phaseIndex);
        }

        private void ApplyVariantForPhase(int phaseIndex)
        {
            if (_variants == null || _variants.Length == 0) return;

            RoomVariantSO matchingVariant = null;
            foreach (var variant in _variants)
            {
                if (variant != null && variant.IsActiveInPhase(phaseIndex))
                {
                    matchingVariant = variant;
                    break;
                }
            }

            if (matchingVariant == _activeVariant) return;

            _activeVariant = matchingVariant;

            // 切换环境子物体
            ApplyVariantEnvironment(matchingVariant);

            Debug.Log($"[Room] {RoomID}: Variant switched to '{(matchingVariant != null ? matchingVariant.VariantName : "Default")}' for phase {phaseIndex}");
        }

        private void ApplyVariantEnvironment(RoomVariantSO variant)
        {
            if (_variantEnvironments == null || _variantEnvironments.Length == 0) return;

            int targetIndex = variant != null ? variant.EnvironmentIndex : -1;

            for (int i = 0; i < _variantEnvironments.Length; i++)
            {
                if (_variantEnvironments[i] != null)
                {
                    _variantEnvironments[i].SetActive(i == targetIndex);
                }
            }
        }

        /// <summary>
        /// Returns the currently active encounter: variant override if available, else the default from RoomSO.
        /// </summary>
        public EncounterSO ActiveEncounter
        {
            get
            {
                if (_activeVariant != null && _activeVariant.OverrideEncounter != null)
                    return _activeVariant.OverrideEncounter;
                return _data != null ? _data.Encounter : null;
            }
        }

        // ──────────────────── Door Helpers ────────────────────

        /// <summary>
        /// Lock all doors in this room with the specified state.
        /// Used by Arena/Boss rooms on player entry.
        /// </summary>
        public void LockAllDoors(DoorState lockState)
        {
            if (_doors == null) return;
            foreach (var door in _doors)
            {
                if (door != null) door.SetState(lockState);
            }
        }

        /// <summary>
        /// Unlock all combat-locked doors in this room.
        /// Called when room is cleared.
        /// </summary>
        public void UnlockCombatDoors()
        {
            if (_doors == null) return;
            foreach (var door in _doors)
            {
                if (door != null && door.CurrentState == DoorState.Locked_Combat)
                {
                    door.SetState(DoorState.Open);
                }
            }
        }
    }
}
