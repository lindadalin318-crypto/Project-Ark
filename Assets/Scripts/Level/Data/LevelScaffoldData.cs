using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Serializable data structure for storing a level design.
    /// Contains all rooms, their positions, and connections.
    /// Can be saved to JSON and loaded back.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelScaffold", menuName = "ProjectArk/Level/Level Scaffold")]
    public class LevelScaffoldData : ScriptableObject
    {
        [Header("Level Info")]
        [Tooltip("Name of this level")]
        [SerializeField] private string _levelName = "New Level";

        [Tooltip("Floor level for this entire scaffold (0 = surface)")]
        [SerializeField] private int _floorLevel = 0;

        [Header("Rooms")]
        [Tooltip("All rooms in this level")]
        [SerializeField] private List<ScaffoldRoom> _rooms = new List<ScaffoldRoom>();

        // ──────────────────── Public Properties ────────────────────

        public string LevelName => _levelName;
        public int FloorLevel => _floorLevel;
        public List<ScaffoldRoom> Rooms => _rooms;

        // ──────────────────── Public Methods ────────────────────

        public void AddRoom(ScaffoldRoom room)
        {
            _rooms.Add(room);
        }

        public void RemoveRoom(ScaffoldRoom room)
        {
            _rooms.Remove(room);
        }

        public void ClearRooms()
        {
            _rooms.Clear();
        }
    }

    /// <summary>
    /// Single room in the level scaffold.
    /// Contains position, size, type, and connections to other rooms.
    /// </summary>
    [Serializable]
    public class ScaffoldRoom
    {
        [Tooltip("Unique identifier for this room")]
        [SerializeField] private string _roomID = Guid.NewGuid().ToString();

        [Tooltip("Display name for this room")]
        [SerializeField] private string _displayName = "New Room";

        [Tooltip("Type of room")]
        [SerializeField] private RoomType _roomType = RoomType.Normal;

        [Tooltip("Position in world space")]
        [SerializeField] private Vector3 _position = Vector3.zero;

        [Tooltip("Size of the room (width, height)")]
        [SerializeField] private Vector2 _size = new Vector2(20, 15);

        [Tooltip("Connections to other rooms")]
        [SerializeField] private List<ScaffoldDoorConnection> _connections = new List<ScaffoldDoorConnection>();

        [Tooltip("RoomSO to use for this room (optional, will be auto-generated if null)")]
        [SerializeField] private RoomSO _roomSO;

        // ──────────────────── Public Properties ────────────────────

        public string RoomID => _roomID;
        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }
        public RoomType RoomType
        {
            get => _roomType;
            set => _roomType = value;
        }
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }
        public Vector2 Size
        {
            get => _size;
            set => _size = value;
        }
        public List<ScaffoldDoorConnection> Connections => _connections;
        public RoomSO RoomSO
        {
            get => _roomSO;
            set => _roomSO = value;
        }

        // ──────────────────── Public Methods ────────────────────

        public void AddConnection(ScaffoldDoorConnection connection)
        {
            _connections.Add(connection);
        }

        public void RemoveConnection(ScaffoldDoorConnection connection)
        {
            _connections.Remove(connection);
        }
    }

    /// <summary>
    /// Connection between two rooms via a door.
    /// </summary>
    [Serializable]
    public class ScaffoldDoorConnection
    {
        [Tooltip("Target room ID")]
        [SerializeField] private string _targetRoomID;

        [Tooltip("Position of the door in this room")]
        [SerializeField] private Vector3 _doorPosition;

        [Tooltip("Direction the door faces (towards the target room)")]
        [SerializeField] private Vector2 _doorDirection;

        [Tooltip("Is this a layer transition (longer fade)")]
        [SerializeField] private bool _isLayerTransition;

        // ──────────────────── Public Properties ────────────────────

        public string TargetRoomID
        {
            get => _targetRoomID;
            set => _targetRoomID = value;
        }
        public Vector3 DoorPosition
        {
            get => _doorPosition;
            set => _doorPosition = value;
        }
        public Vector2 DoorDirection
        {
            get => _doorDirection;
            set => _doorDirection = value;
        }
        public bool IsLayerTransition
        {
            get => _isLayerTransition;
            set => _isLayerTransition = value;
        }
    }
}
