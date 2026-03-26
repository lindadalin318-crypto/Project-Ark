using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// 显式的世界房间网络图谱。定义所有房间节点和它们之间的连接关系。
    /// 这是关卡拓扑结构的 Single Source of Truth。
    /// 
    /// 运行时由 RoomManager 加载并缓存为字典，供 DoorTransitionController 和
    /// MinimapManager 消费。
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectArk/Level/World Graph", order = 0)]
    public class WorldGraphSO : ScriptableObject
    {
        [Header("Graph Info")]
        [SerializeField] private string _graphName = "New World Graph";

        [Header("Nodes")]
        [Tooltip("所有房间节点（拓扑数据）")]
        [SerializeField] private RoomNodeData[] _rooms = System.Array.Empty<RoomNodeData>();

        [Header("Edges")]
        [Tooltip("所有连接边（单向；双向门需要两条反向边）")]
        [SerializeField] private ConnectionEdge[] _connections = System.Array.Empty<ConnectionEdge>();

        // ──────────────────── Public Properties ────────────────────

        /// <summary> 图谱名称。 </summary>
        public string GraphName => _graphName;

        /// <summary> 所有房间节点数组。 </summary>
        public RoomNodeData[] Rooms => _rooms;

        /// <summary> 所有连接边数组。 </summary>
        public ConnectionEdge[] Connections => _connections;

        /// <summary> 房间节点数量。 </summary>
        public int RoomCount => _rooms.Length;

        /// <summary> 连接边数量。 </summary>
        public int ConnectionCount => _connections.Length;

        // ──────────────────── Runtime Lookup Cache ────────────────────

        // 运行时缓存，首次访问时构建
        private Dictionary<string, RoomNodeData> _roomLookup;
        private Dictionary<string, List<ConnectionEdge>> _outgoingLookup;
        private Dictionary<string, List<ConnectionEdge>> _incomingLookup;

        /// <summary>
        /// 按 RoomID 查找节点。运行时使用字典缓存，O(1)。
        /// </summary>
        public bool TryGetRoom(string roomID, out RoomNodeData roomNode)
        {
            EnsureLookup();
            return _roomLookup.TryGetValue(roomID, out roomNode);
        }

        /// <summary>
        /// 获取指定房间的所有出边（从该房间出发的连接）。
        /// </summary>
        public IReadOnlyList<ConnectionEdge> GetOutgoingConnections(string roomID)
        {
            EnsureLookup();
            if (_outgoingLookup.TryGetValue(roomID, out var list))
                return list;
            return System.Array.Empty<ConnectionEdge>();
        }

        /// <summary>
        /// 获取指定房间的所有入边（到达该房间的连接）。
        /// </summary>
        public IReadOnlyList<ConnectionEdge> GetIncomingConnections(string roomID)
        {
            EnsureLookup();
            if (_incomingLookup.TryGetValue(roomID, out var list))
                return list;
            return System.Array.Empty<ConnectionEdge>();
        }

        /// <summary>
        /// 根据 GateID 查找连接。返回从指定房间的指定 Gate 出发的连接边。
        /// </summary>
        public bool TryGetConnectionByGate(string fromRoomID, string fromGateID, out ConnectionEdge edge)
        {
            EnsureLookup();
            if (_outgoingLookup.TryGetValue(fromRoomID, out var edges))
            {
                foreach (var e in edges)
                {
                    if (e.FromGateID == fromGateID)
                    {
                        edge = e;
                        return true;
                    }
                }
            }

            edge = default;
            return false;
        }

        /// <summary>
        /// 获取所有与指定房间相邻的房间 ID 集合（包含双向邻居）。
        /// MinimapManager 用此替代运行时 Door 推导。
        /// </summary>
        public HashSet<string> GetAdjacentRoomIDs(string roomID)
        {
            EnsureLookup();
            var result = new HashSet<string>();

            if (_outgoingLookup.TryGetValue(roomID, out var outgoing))
            {
                foreach (var e in outgoing)
                    result.Add(e.ToRoomID);
            }

            if (_incomingLookup.TryGetValue(roomID, out var incoming))
            {
                foreach (var e in incoming)
                    result.Add(e.FromRoomID);
            }

            return result;
        }

        /// <summary>
        /// 检查是否存在从一个房间到另一个房间的连接。
        /// </summary>
        public bool HasConnection(string fromRoomID, string toRoomID)
        {
            EnsureLookup();
            if (_outgoingLookup.TryGetValue(fromRoomID, out var edges))
            {
                foreach (var e in edges)
                {
                    if (e.ToRoomID == toRoomID)
                        return true;
                }
            }
            return false;
        }

        // ──────────────────── Lookup Construction ────────────────────

        private void EnsureLookup()
        {
            if (_roomLookup != null) return;
            RebuildLookup();
        }

        /// <summary>
        /// 重建运行时查找缓存。在资产修改后可手动调用。
        /// </summary>
        public void RebuildLookup()
        {
            _roomLookup = new Dictionary<string, RoomNodeData>(_rooms.Length);
            _outgoingLookup = new Dictionary<string, List<ConnectionEdge>>();
            _incomingLookup = new Dictionary<string, List<ConnectionEdge>>();

            foreach (var room in _rooms)
            {
                if (string.IsNullOrEmpty(room.RoomID))
                {
                    Debug.LogWarning($"[WorldGraphSO] '{_graphName}': Found a room node with empty RoomID!");
                    continue;
                }

                if (!_roomLookup.TryAdd(room.RoomID, room))
                {
                    Debug.LogWarning($"[WorldGraphSO] '{_graphName}': Duplicate RoomID '{room.RoomID}'!");
                }
            }

            foreach (var conn in _connections)
            {
                if (string.IsNullOrEmpty(conn.FromRoomID) || string.IsNullOrEmpty(conn.ToRoomID))
                {
                    Debug.LogWarning($"[WorldGraphSO] '{_graphName}': Found a connection with empty RoomID!");
                    continue;
                }

                if (!_outgoingLookup.TryGetValue(conn.FromRoomID, out var outList))
                {
                    outList = new List<ConnectionEdge>(4);
                    _outgoingLookup[conn.FromRoomID] = outList;
                }
                outList.Add(conn);

                if (!_incomingLookup.TryGetValue(conn.ToRoomID, out var inList))
                {
                    inList = new List<ConnectionEdge>(4);
                    _incomingLookup[conn.ToRoomID] = inList;
                }
                inList.Add(conn);
            }
        }

        // ──────────────────── Validation Helpers (Editor) ────────────────────

        /// <summary>
        /// 获取在图谱中定义但场景中不存在的 RoomID（用于 LevelValidator）。
        /// </summary>
        public List<string> GetAllRoomIDs()
        {
            var ids = new List<string>(_rooms.Length);
            foreach (var room in _rooms)
            {
                if (!string.IsNullOrEmpty(room.RoomID))
                    ids.Add(room.RoomID);
            }
            return ids;
        }

        /// <summary>
        /// 获取没有任何连接边的孤立房间 ID。
        /// </summary>
        public List<string> GetIsolatedRoomIDs()
        {
            EnsureLookup();
            var isolated = new List<string>();

            foreach (var room in _rooms)
            {
                if (string.IsNullOrEmpty(room.RoomID)) continue;

                bool hasOutgoing = _outgoingLookup.ContainsKey(room.RoomID);
                bool hasIncoming = _incomingLookup.ContainsKey(room.RoomID);

                if (!hasOutgoing && !hasIncoming)
                    isolated.Add(room.RoomID);
            }

            return isolated;
        }

        private void OnEnable()
        {
            // 资产加载时清空缓存，下次访问时重建
            _roomLookup = null;
            _outgoingLookup = null;
            _incomingLookup = null;
        }

        private void OnValidate()
        {
            // Inspector 编辑时清空缓存
            _roomLookup = null;
            _outgoingLookup = null;
            _incomingLookup = null;
        }
    }
}
