using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Central minimap/map data manager. Gathers room data from the scene,
    /// builds adjacency graph from Door connections, and tracks visited rooms.
    /// 
    /// Registers with ServiceLocator. Consumed by MapPanel and MinimapHUD.
    /// </summary>
    public class MinimapManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Save")]
        [Tooltip("Save slot index for persisting visited rooms.")]
        [SerializeField] private int _saveSlot = 0;

        // ──────────────────── Runtime State ────────────────────

        private readonly Dictionary<string, Room> _roomLookup = new();
        private readonly Dictionary<string, MapRoomData> _roomDataCache = new();
        private readonly List<MapConnection> _connections = new();
        private readonly HashSet<string> _visitedRoomIDs = new();
        private readonly HashSet<int> _discoveredFloors = new();

        private string _currentRoomID;
        private int _currentFloor;

        // ──────────────────── Events ────────────────────

        /// <summary> Fired when map data changes (room visited, room changed). </summary>
        public event Action OnMapDataChanged;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Currently active floor level. </summary>
        public int CurrentFloor => _currentFloor;

        /// <summary> Currently active room ID. </summary>
        public string CurrentRoomID => _currentRoomID;

        /// <summary> All discovered floor levels. </summary>
        public IReadOnlyCollection<int> DiscoveredFloors => _discoveredFloors;

        /// <summary> Number of rooms visited. </summary>
        public int VisitedCount => _visitedRoomIDs.Count;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            GatherSceneData();
            LoadVisitedRooms();

            // Subscribe to room events
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.OnCurrentRoomChanged += HandleRoomChanged;

                // Initialize with current room if already set
                if (roomManager.CurrentRoom != null)
                {
                    HandleRoomChanged(roomManager.CurrentRoom);
                }
            }
        }

        private void OnDestroy()
        {
            var roomManager = ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.OnCurrentRoomChanged -= HandleRoomChanged;
            }

            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Scene Data Gathering ────────────────────

        private void GatherSceneData()
        {
            _roomLookup.Clear();
            _roomDataCache.Clear();
            _connections.Clear();

            var allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

            // Build room lookup and data cache
            foreach (var room in allRooms)
            {
                if (room.Data == null) continue;

                string id = room.RoomID;
                _roomLookup[id] = room;

                var boxCol = room.GetComponent<BoxCollider2D>();
                Vector2 center = boxCol != null
                    ? (Vector2)room.transform.position + boxCol.offset
                    : (Vector2)room.transform.position;
                Vector2 size = boxCol != null ? boxCol.size : Vector2.one * 10f;

                _roomDataCache[id] = new MapRoomData
                {
                    RoomID = id,
                    DisplayName = room.Data.DisplayName,
                    WorldCenter = center,
                    WorldSize = size,
                    FloorLevel = room.Data.FloorLevel,
                    Type = room.Data.Type,
                    MapIcon = room.Data.MapIcon,
                    IsVisited = false,
                    IsCurrent = false
                };
            }

            // Build connections from Door references
            var processedPairs = new HashSet<string>();
            foreach (var room in allRooms)
            {
                if (room.Doors == null) continue;

                foreach (var door in room.Doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    string fromID = room.RoomID;
                    string toID = door.TargetRoom.RoomID;

                    // Deduplicate bidirectional connections
                    string pairKey = string.Compare(fromID, toID, StringComparison.Ordinal) < 0
                        ? $"{fromID}|{toID}"
                        : $"{toID}|{fromID}";

                    if (!processedPairs.Add(pairKey)) continue;

                    if (_roomDataCache.TryGetValue(fromID, out var fromData) &&
                        _roomDataCache.TryGetValue(toID, out var toData))
                    {
                        _connections.Add(new MapConnection
                        {
                            FromRoomID = fromID,
                            ToRoomID = toID,
                            Midpoint = (fromData.WorldCenter + toData.WorldCenter) * 0.5f,
                            IsLayerTransition = door.IsLayerTransition
                        });
                    }
                }
            }

            Debug.Log($"[MinimapManager] Gathered {_roomDataCache.Count} rooms, {_connections.Count} connections.");
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Get all room nodes for the specified floor level.
        /// </summary>
        public List<MapRoomData> GetRoomNodes(int floor)
        {
            var result = new List<MapRoomData>();
            foreach (var kvp in _roomDataCache)
            {
                if (kvp.Value.FloorLevel == floor)
                {
                    result.Add(kvp.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// Get all room nodes across all floors.
        /// </summary>
        public List<MapRoomData> GetAllRoomNodes()
        {
            return new List<MapRoomData>(_roomDataCache.Values);
        }

        /// <summary>
        /// Get all connections, optionally filtered to a specific floor.
        /// A connection is included if at least one endpoint is on the specified floor.
        /// </summary>
        public List<MapConnection> GetConnections(int? floorFilter = null)
        {
            if (floorFilter == null) return new List<MapConnection>(_connections);

            int floor = floorFilter.Value;
            var result = new List<MapConnection>();

            foreach (var conn in _connections)
            {
                bool fromOnFloor = _roomDataCache.TryGetValue(conn.FromRoomID, out var from) &&
                                   from.FloorLevel == floor;
                bool toOnFloor = _roomDataCache.TryGetValue(conn.ToRoomID, out var to) &&
                                 to.FloorLevel == floor;

                if (fromOnFloor || toOnFloor)
                {
                    result.Add(conn);
                }
            }

            return result;
        }

        /// <summary> Check if a room has been visited. </summary>
        public bool IsVisited(string roomID) => _visitedRoomIDs.Contains(roomID);

        /// <summary>
        /// Get data for a specific room, or null if not found.
        /// </summary>
        public MapRoomData? GetRoomData(string roomID)
        {
            return _roomDataCache.TryGetValue(roomID, out var data) ? data : null;
        }

        /// <summary>
        /// Get all discovered floor levels (sorted).
        /// </summary>
        public List<int> GetFloorsDiscovered()
        {
            var floors = new List<int>(_discoveredFloors);
            floors.Sort();
            return floors;
        }

        /// <summary>
        /// Mark a room as visited. Called by RoomManager on first visit.
        /// </summary>
        public void MarkVisited(string roomID)
        {
            if (string.IsNullOrEmpty(roomID)) return;
            if (!_visitedRoomIDs.Add(roomID)) return; // Already visited

            // Update cached data
            if (_roomDataCache.TryGetValue(roomID, out var data))
            {
                data.IsVisited = true;
                _roomDataCache[roomID] = data;

                // Track discovered floors
                _discoveredFloors.Add(data.FloorLevel);
            }

            // Broadcast first visit
            LevelEvents.RaiseRoomFirstVisit(roomID);
            OnMapDataChanged?.Invoke();
        }

        /// <summary>
        /// Get the read-only visited room ID set (for save system).
        /// </summary>
        public IReadOnlyCollection<string> GetVisitedRoomIDs() => _visitedRoomIDs;

        /// <summary>
        /// Import visited room IDs from save data.
        /// </summary>
        public void ImportVisitedRooms(List<string> roomIDs)
        {
            if (roomIDs == null) return;

            foreach (var id in roomIDs)
            {
                _visitedRoomIDs.Add(id);

                if (_roomDataCache.TryGetValue(id, out var data))
                {
                    data.IsVisited = true;
                    _roomDataCache[id] = data;
                    _discoveredFloors.Add(data.FloorLevel);
                }
            }

            OnMapDataChanged?.Invoke();
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleRoomChanged(Room newRoom)
        {
            if (newRoom == null) return;

            string newID = newRoom.RoomID;

            // Update current room flag
            if (!string.IsNullOrEmpty(_currentRoomID) && _roomDataCache.TryGetValue(_currentRoomID, out var oldData))
            {
                oldData.IsCurrent = false;
                _roomDataCache[_currentRoomID] = oldData;
            }

            if (_roomDataCache.TryGetValue(newID, out var newData))
            {
                newData.IsCurrent = true;
                _roomDataCache[newID] = newData;
            }

            _currentRoomID = newID;

            // Floor change detection
            int newFloor = newRoom.Data != null ? newRoom.Data.FloorLevel : 0;
            if (newFloor != _currentFloor)
            {
                _currentFloor = newFloor;
                LevelEvents.RaiseFloorChanged(_currentFloor);
            }

            // Mark as visited
            MarkVisited(newID);
        }

        // ──────────────────── Save/Load ────────────────────

        private void LoadVisitedRooms()
        {
            var data = SaveManager.Load(_saveSlot);
            if (data?.Progress?.VisitedRoomIDs == null) return;

            ImportVisitedRooms(data.Progress.VisitedRoomIDs);

            Debug.Log($"[MinimapManager] Loaded {_visitedRoomIDs.Count} visited rooms.");
        }
    }
}
