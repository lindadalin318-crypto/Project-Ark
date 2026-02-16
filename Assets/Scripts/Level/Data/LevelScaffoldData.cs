using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Type of element that can be placed in a room.
    /// </summary>
    public enum ScaffoldElementType
    {
        Wall,
        WallCorner,
        CrateWooden,
        CrateMetal,
        Door,
        Checkpoint,
        PlayerSpawn,
        EnemySpawn,
        Hazard
    }

    /// <summary>
    /// Gizmo shape to display for an element.
    /// </summary>
    public enum ElementGizmoShape
    {
        Square,
        Circle,
        Diamond
    }

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
    /// Configuration for a Door element.
    /// </summary>
    [Serializable]
    public class ScaffoldDoorElementConfig
    {
        [Tooltip("Initial state of the door")]
        [SerializeField] private DoorState _initialState = DoorState.Open;

        [Tooltip("Required key ID (for Locked_Key state)")]
        [SerializeField] private string _requiredKeyID;

        [Tooltip("Phase indices during which this door opens (for Locked_Schedule)")]
        [SerializeField] private int[] _openDuringPhases;

        // ──────────────────── Public Properties ────────────────────

        public DoorState InitialState
        {
            get => _initialState;
            set => _initialState = value;
        }
        public string RequiredKeyID
        {
            get => _requiredKeyID;
            set => _requiredKeyID = value;
        }
        public int[] OpenDuringPhases
        {
            get => _openDuringPhases;
            set => _openDuringPhases = value;
        }
    }

    /// <summary>
    /// Single element placed inside a room.
    /// </summary>
    [Serializable]
    public class ScaffoldElement
    {
        [Tooltip("Unique identifier for this element")]
        [SerializeField] private string _elementID = Guid.NewGuid().ToString();

        [Tooltip("Type of element")]
        [SerializeField] private ScaffoldElementType _elementType;

        [Tooltip("Gizmo shape to display for this element")]
        [SerializeField] private ElementGizmoShape _gizmoShape = ElementGizmoShape.Square;

        [Tooltip("Position relative to the room center")]
        [SerializeField] private Vector3 _localPosition;

        [Tooltip("Rotation (Z-axis)")]
        [SerializeField] private float _rotation;

        [Tooltip("Scale")]
        [SerializeField] private Vector3 _scale = Vector3.one;

        [Tooltip("Door-specific configuration (only for Door type)")]
        [SerializeField] private ScaffoldDoorElementConfig _doorConfig;

        [Tooltip("ID of the ScaffoldDoorConnection this door is bound to")]
        [SerializeField] private string _boundConnectionID;

        // ──────────────────── Public Properties ────────────────────

        public string ElementID => _elementID;
        public ScaffoldElementType ElementType
        {
            get => _elementType;
            set
            {
                _elementType = value;
                SetDefaultGizmoShapeForType();
            }
        }
        public ElementGizmoShape GizmoShape
        {
            get => _gizmoShape;
            set => _gizmoShape = value;
        }
        public Vector3 LocalPosition
        {
            get => _localPosition;
            set => _localPosition = value;
        }
        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
        }
        public ScaffoldDoorElementConfig DoorConfig
        {
            get => _doorConfig;
            set => _doorConfig = value;
        }
        public string BoundConnectionID
        {
            get => _boundConnectionID;
            set => _boundConnectionID = value;
        }

        // ──────────────────── Public Methods ────────────────────

        public void EnsureDoorConfigExists()
        {
            if (_doorConfig == null)
            {
                _doorConfig = new ScaffoldDoorElementConfig();
            }
        }

        public void SetDefaultGizmoShapeForType()
        {
            switch (_elementType)
            {
                case ScaffoldElementType.Wall:
                case ScaffoldElementType.WallCorner:
                case ScaffoldElementType.CrateWooden:
                case ScaffoldElementType.CrateMetal:
                    _gizmoShape = ElementGizmoShape.Square;
                    break;
                case ScaffoldElementType.Checkpoint:
                case ScaffoldElementType.PlayerSpawn:
                case ScaffoldElementType.EnemySpawn:
                case ScaffoldElementType.Hazard:
                    _gizmoShape = ElementGizmoShape.Circle;
                    break;
                case ScaffoldElementType.Door:
                    _gizmoShape = ElementGizmoShape.Square;
                    break;
                default:
                    _gizmoShape = ElementGizmoShape.Square;
                    break;
            }
        }
    }

    /// <summary>
    /// Single room in the level scaffold.
    /// Contains position, size, type, connections, and elements inside it.
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

        [Tooltip("Elements placed inside this room")]
        [SerializeField] private List<ScaffoldElement> _elements = new List<ScaffoldElement>();

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
        public List<ScaffoldElement> Elements => _elements;
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

        public void AddElement(ScaffoldElement element)
        {
            _elements.Add(element);
        }

        public void RemoveElement(ScaffoldElement element)
        {
            _elements.Remove(element);
        }
    }

    /// <summary>
    /// Connection between two rooms via a door.
    /// </summary>
    [Serializable]
    public class ScaffoldDoorConnection
    {
        [Tooltip("Unique identifier for this connection")]
        [SerializeField] private string _connectionID = Guid.NewGuid().ToString();

        [Tooltip("Target room ID")]
        [SerializeField] private string _targetRoomID;

        [Tooltip("Position of the door in this room")]
        [SerializeField] private Vector3 _doorPosition;

        [Tooltip("Direction the door faces (towards the target room)")]
        [SerializeField] private Vector2 _doorDirection;

        [Tooltip("Is this a layer transition (longer fade)")]
        [SerializeField] private bool _isLayerTransition;

        // ──────────────────── Public Properties ────────────────────

        public string ConnectionID => _connectionID;
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
