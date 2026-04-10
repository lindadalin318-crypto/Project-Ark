using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level
{
    /// <summary>
    /// Central room management service. Tracks the current room, broadcasts room events
    /// via LevelEvents, and manages enemy activation/deactivation for performance.
    /// 
    /// Place one RoomManager in the scene. Registers itself to ServiceLocator.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Setup")]
        [Tooltip("The room the player starts in when the scene loads.")]
        [SerializeField] private Room _startingRoom;

        // ──────────────────── Runtime State ────────────────────

        private Room _currentRoom;
        private Room _previousRoom;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> The room the player is currently in. </summary>
        public Room CurrentRoom => _currentRoom;

        /// <summary> The room the player was in before the current one. </summary>
        public Room PreviousRoom => _previousRoom;

        /// <summary> Current floor level (convenience property derived from current room). </summary>
        public int CurrentFloor => _currentRoom != null && _currentRoom.Data != null
            ? _currentRoom.Data.FloorLevel
            : 0;

        // ──────────────────── Events ────────────────────

        /// <summary>
        /// Instance event fired when the current room changes.
        /// Used by local listeners (e.g., RoomCameraConfiner) that need the Room reference.
        /// Global listeners should use LevelEvents.OnRoomEntered instead.
        /// </summary>
        public event Action<Room> OnCurrentRoomChanged;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (_startingRoom != null)
            {
                EnterRoom(_startingRoom);
            }
            else
            {
                Debug.LogWarning("[RoomManager] No starting room assigned. Assign a Room reference in the Inspector.");
            }

            // Subscribe to all Room triggers in the scene
            SubscribeToAllRooms();
        }

        private void OnDestroy()
        {
            UnsubscribeFromAllRooms();
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Room Subscription ────────────────────

        private Room[] _allRooms;
        private Dictionary<string, Room> _roomLookup;

        private void SubscribeToAllRooms()
        {
            _allRooms = FindObjectsByType<Room>();
            _roomLookup = new Dictionary<string, Room>(_allRooms.Length);

            foreach (var room in _allRooms)
            {
                room.OnPlayerEntered += HandlePlayerEnteredRoom;
                room.OnPlayerExited += HandlePlayerExitedRoom;

                // Build RoomID → Room lookup
                string id = room.RoomID;
                if (!string.IsNullOrEmpty(id))
                {
                    _roomLookup.TryAdd(id, room);
                }
            }

            Debug.Log($"[RoomManager] Subscribed to {_allRooms.Length} rooms, lookup built with {_roomLookup.Count} entries.");
        }

        private void UnsubscribeFromAllRooms()
        {
            if (_allRooms == null) return;

            foreach (var room in _allRooms)
            {
                if (room == null) continue;
                room.OnPlayerEntered -= HandlePlayerEnteredRoom;
                room.OnPlayerExited -= HandlePlayerExitedRoom;
            }
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandlePlayerEnteredRoom(Room room)
        {
            if (room == _currentRoom) return; // Already in this room
            EnterRoom(room);
        }

        private void HandlePlayerExitedRoom(Room room)
        {
            if (room != _currentRoom) return;
            LevelEvents.RaiseRoomExited(room.RoomID);
        }

        // ──────────────────── Core API ────────────────────

        /// <summary>
        /// 按 RoomID 查找场景中的 Room。运行时使用字典缓存，O(1)。
        /// </summary>
        public Room FindRoomByID(string roomID)
        {
            if (string.IsNullOrEmpty(roomID)) return null;
            if (_roomLookup == null) return null;
            _roomLookup.TryGetValue(roomID, out var room);
            return room;
        }

        /// <summary>
        /// Find a Door by its GateID within a specific room.
        /// Used by WorldProgressManager to unlock doors when world stage advances.
        /// </summary>
        /// <param name="roomID">The room containing the door.</param>
        /// <param name="gateID">The GateID of the door to find.</param>
        /// <returns>The matching Door, or null if not found.</returns>
        public Door FindDoorByGateID(string roomID, string gateID)
        {
            if (string.IsNullOrEmpty(gateID)) return null;

            var room = FindRoomByID(roomID);
            if (room == null) return null;

            var doors = room.GetComponentsInChildren<Door>(true);
            foreach (var door in doors)
            {
                if (door.GateID == gateID)
                    return door;
            }

            return null;
        }

        /// <summary>
        /// Set the given room as the current room. Handles all side effects:
        /// state transitions, event broadcasting, enemy activation, director token cleanup.
        /// Called by Room triggers and DoorTransitionController.
        /// </summary>
        public void EnterRoom(Room room)
        {
            if (room == null)
            {
                Debug.LogError("[RoomManager] EnterRoom called with null room.");
                return;
            }

            // ── Update tracking ──
            _previousRoom = _currentRoom;
            _currentRoom = room;

            // ── State transition ──
            bool isFirstVisit = room.State == RoomState.Undiscovered;
            if (isFirstVisit)
            {
                room.SetState(RoomState.Entered);
            }

            // ── Map tracking (first visit) ──
            if (isFirstVisit)
            {
                var minimap = ServiceLocator.Get<MinimapManager>();
                minimap?.MarkVisited(room.RoomID);
            }

            // ── Enemy management ──
            // Deactivate enemies in the previous room
            if (_previousRoom != null && _previousRoom != _currentRoom)
            {
                _previousRoom.DeactivateEnemies();
            }

            // ── Director cleanup ──
            // Clear attack tokens to prevent stale references from the previous room
            var director = ServiceLocator.Get<EnemyDirector>();
            if (director != null)
            {
                director.ReturnAllTokens();
            }

            // ── Arena/Boss encounter ──
            // ArenaController owns the full encounter flow (lock doors → delay → spawn waves → unlock).
            // Do NOT call ActivateEnemies() for these rooms — it would create a WaveSpawnStrategy that
            // gets immediately Reset() when ArenaController calls SetStrategy() moments later.
            if (room.NodeType == RoomNodeType.Arena || room.NodeType == RoomNodeType.Boss)
            {
                if (room.State != RoomState.Cleared)
                {
                    var arena = room.GetComponent<ArenaController>();
                    if (arena != null)
                    {
                        arena.BeginEncounter();
                    }
                    else
                    {
                        // Fallback: no ArenaController → lock doors + activate directly
                        room.LockAllDoors(DoorState.Locked_Combat);
                        _currentRoom.ActivateEnemies();
                    }
                }
            }
            else
            {
                // Non-arena rooms activate enemies directly.
                _currentRoom.ActivateEnemies();
            }

            // ── Broadcast events ──
            LevelEvents.RaiseRoomEntered(room.RoomID);
            OnCurrentRoomChanged?.Invoke(room);

            Debug.Log($"[RoomManager] Entered room: {room.RoomID} (NodeType: {room.NodeType}, State: {room.State})");
        }

        /// <summary>
        /// Notify that a room has been cleared of all enemies.
        /// Typically called by the encounter system after the last wave is defeated.
        /// </summary>
        public void NotifyRoomCleared(Room room)
        {
            if (room == null) return;

            room.SetState(RoomState.Cleared);
            room.UnlockCombatDoors();

            LevelEvents.RaiseRoomCleared(room.RoomID);

            Debug.Log($"[RoomManager] Room cleared: {room.RoomID}");
        }
    }
}
