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
        private ArenaController _arenaController;
        private WaveSpawnStrategy _waveStrategy;
        private Action _roomOwnedEncounterComplete;
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

        /// <summary> Whether this room has a room-owned encounter setup (EncounterSO + EnemySpawner). </summary>
        public bool HasRoomOwnedEncounterSetup => ActiveEncounter != null && _spawner != null;

        // ──────────────────── Lifecycle ────────────────────

        private const string NAVIGATION_ROOT_NAME = "Navigation";
        private const string ENCOUNTERS_ROOT_NAME = "Encounters";
        private const string ELEMENTS_ROOT_NAME = "Elements";
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
            _arenaController = GetComponent<ArenaController>();
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
            var encounterSpawnRoot = transform.Find($"{ENCOUNTERS_ROOT_NAME}/SpawnPoints");
            var encounterSpawnPoints = CollectDirectChildTransforms(encounterSpawnRoot);
            if (encounterSpawnPoints.Length > 0)
            {
                return encounterSpawnPoints;
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
        /// Activate combat in this room through the single room-level entry.
        /// Arena/Boss rooms delegate ceremony to ArenaController, normal rooms start the room-owned encounter directly,
        /// and legacy rooms without encounter data fall back to pre-placed child enemies.
        /// </summary>
        public void ActivateEnemies()
        {
            if (_state == RoomState.Cleared)
            {
                return;
            }

            if (UsesArenaEncounterOrchestrator)
            {
                _arenaController.BeginEncounter();
                return;
            }

            if (IsArenaOrBossRoom && _arenaController == null)
            {
                Debug.LogWarning($"[Room] {RoomID}: {NodeType} room is missing ArenaController. Falling back to direct room activation.");
                LockAllDoors(DoorState.Locked_Combat);
            }

            if (HasRoomOwnedEncounterSetup)
            {
                StartRoomOwnedEncounter();
                return;
            }

            // 回退模式：激活预放置的敌人 GameObject
            ActivatePreplacedEnemies();
        }

        /// <summary>
        /// Start the room-owned encounter using the active EncounterSO and EnemySpawner.
        /// ArenaController may supply a completion callback for arena-specific post-clear orchestration.
        /// </summary>
        public bool StartRoomOwnedEncounter(Action onEncounterComplete = null)
        {
            var encounter = ActiveEncounter;
            if (_state == RoomState.Cleared || encounter == null || _spawner == null)
            {
                return false;
            }

            if (_waveStrategy != null)
            {
                Debug.LogWarning($"[Room] {RoomID}: Room-owned encounter is already active.");
                return false;
            }

            _roomOwnedEncounterComplete = onEncounterComplete;
            _waveStrategy = new WaveSpawnStrategy(encounter);
            _waveStrategy.OnEncounterComplete += HandleEncounterComplete;

            _spawner.SetStrategy(_waveStrategy);
            _spawner.StartStrategy();

            Debug.Log($"[Room] {RoomID}: Room-owned encounter started ({encounter.WaveCount} waves)");
            return true;
        }

        /// <summary>
        /// Stop the room-owned encounter lifecycle and despawn currently active spawned enemies.
        /// </summary>
        public void StopRoomOwnedEncounter()
        {
            _spawner?.StopAndDespawnActiveEnemies();
            CleanupWaveStrategy();
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

        private void DeactivatePreplacedEnemies()
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

        private void HandleEncounterComplete()
        {
            var onEncounterComplete = _roomOwnedEncounterComplete;
            CleanupWaveStrategy();

            if (onEncounterComplete != null)
            {
                onEncounterComplete.Invoke();
                return;
            }

            NotifyRoomOwnedEncounterCleared();
        }

        private void NotifyRoomOwnedEncounterCleared()
        {
            // Room-owned encounter may clear the whole room.
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.NotifyRoomCleared(this);
            }
            else
            {
                Debug.LogError($"[Room] {RoomID}: RoomManager not found when encounter completed.");
            }

            Debug.Log($"[Room] {RoomID}: Encounter complete → Room cleared!");
        }

        /// <summary>
        /// Deactivate all room-owned combat actors when this room is no longer current.
        /// Handles pre-placed enemies, room-level spawner output, and local open encounters.
        /// </summary>
        public void DeactivateEnemies()
        {
            _arenaController?.HandleRoomExit();
            DeactivatePreplacedEnemies();
            StopRoomOwnedEncounter();
            DeactivateOpenEncountersForRoomExit();
        }

        /// <summary>
        /// Reset all enemies in this room to their initial state.
        /// Used by death/respawn system to repopulate the room.
        /// Deactivates all enemies, resets spawner strategy, resets room state, then reactivates.
        /// </summary>
        public void ResetEnemies()
        {
            _arenaController?.ResetEncounter();
            DeactivatePreplacedEnemies();
            StopRoomOwnedEncounter();
            ResetOpenEncounters();

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

        private void DeactivateOpenEncountersForRoomExit()
        {
            if (_openEncounters == null)
            {
                return;
            }

            foreach (var openEnc in _openEncounters)
            {
                openEnc?.HandleRoomExit();
            }
        }

        private void ResetOpenEncounters()
        {
            if (_openEncounters == null)
            {
                return;
            }

            foreach (var openEnc in _openEncounters)
            {
                openEnc?.ResetEncounter();
            }
        }

        private void CleanupWaveStrategy()
        {
            _roomOwnedEncounterComplete = null;

            if (_waveStrategy == null)
            {
                return;
            }

            _waveStrategy.OnEncounterComplete -= HandleEncounterComplete;
            _waveStrategy.Dispose();
            _waveStrategy = null;
        }

        private bool UsesArenaEncounterOrchestrator => IsArenaOrBossRoom && _arenaController != null;

        private bool IsArenaOrBossRoom => NodeType == RoomNodeType.Arena || NodeType == RoomNodeType.Boss;

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
