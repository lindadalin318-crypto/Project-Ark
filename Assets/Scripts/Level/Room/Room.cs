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

        /// <summary> Room type (from SO). </summary>
        public RoomType Type => _data != null ? _data.Type : RoomType.Normal;

        /// <summary> Camera confiner bounds collider. </summary>
        public Collider2D ConfinerBounds => _confinerBounds;

        /// <summary> Enemy spawn points in this room. </summary>
        public Transform[] SpawnPoints => _spawnPoints;

        /// <summary> All doors belonging to this room. </summary>
        public Door[] Doors => _doors;

        /// <summary> Current runtime state. </summary>
        public RoomState State => _state;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Auto-collect Door components from children
            _doors = GetComponentsInChildren<Door>(true);

            // Auto-find EnemySpawner on this room (if any)
            _spawner = GetComponentInChildren<EnemySpawner>(true);

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
