using System;
using UnityEngine;
using ProjectArk.Core;

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

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player. Used to filter OnTriggerEnter2D.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private RoomState _state = RoomState.Undiscovered;
        private Door[] _doors;

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
        /// Activate all enemies in this room (enable their GameObjects).
        /// Called by RoomManager when player enters the room.
        /// </summary>
        public void ActivateEnemies()
        {
            // Phase 3 将接入 EnemySpawner WaveSpawnStrategy
            // 当前阶段：激活房间内已有的敌人 GameObject
            foreach (var sp in _spawnPoints)
            {
                if (sp == null) continue;
                // 激活 spawn point 下的所有子对象（预放置的敌人）
                for (int i = 0; i < sp.childCount; i++)
                {
                    sp.GetChild(i).gameObject.SetActive(true);
                }
            }
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
        /// Deactivates all enemies, resets room state to Entered, then reactivates.
        /// </summary>
        public void ResetEnemies()
        {
            // 先全部关闭
            DeactivateEnemies();

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
