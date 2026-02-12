using System;
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

        private void SubscribeToAllRooms()
        {
            _allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

            foreach (var room in _allRooms)
            {
                room.OnPlayerEntered += HandlePlayerEnteredRoom;
                room.OnPlayerExited += HandlePlayerExitedRoom;
            }

            Debug.Log($"[RoomManager] Subscribed to {_allRooms.Length} rooms.");
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
            if (room.State == RoomState.Undiscovered)
            {
                room.SetState(RoomState.Entered);
            }

            // ── Enemy management ──
            // Deactivate enemies in the previous room
            if (_previousRoom != null && _previousRoom != _currentRoom)
            {
                _previousRoom.DeactivateEnemies();
            }

            // Activate enemies in the current room
            _currentRoom.ActivateEnemies();

            // ── Director cleanup ──
            // Clear attack tokens to prevent stale references from the previous room
            var director = ServiceLocator.Get<EnemyDirector>();
            if (director != null)
            {
                director.ReturnAllTokens();
            }

            // ── Arena/Boss auto-lock ──
            if (room.Type == RoomType.Arena || room.Type == RoomType.Boss)
            {
                if (room.State != RoomState.Cleared)
                {
                    room.LockAllDoors(DoorState.Locked_Combat);
                }
            }

            // ── Broadcast events ──
            LevelEvents.RaiseRoomEntered(room.RoomID);
            OnCurrentRoomChanged?.Invoke(room);

            Debug.Log($"[RoomManager] Entered room: {room.RoomID} (Type: {room.Type}, State: {room.State})");
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
