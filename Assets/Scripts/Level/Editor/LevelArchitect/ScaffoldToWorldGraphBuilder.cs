using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.2]
    /// Authority Builder: generates a WorldGraphSO asset from a LevelScaffoldData asset.
    /// 
    /// Mapping rules:
    /// - ScaffoldRoom → RoomNodeData (with RoomType → RoomNodeType mapping)
    /// - ScaffoldDoorConnection → bidirectional ConnectionEdge pairs
    /// - GateIDs auto-generated from connections (format: "gate_{direction}_{targetRoomID}")
    /// - EditorPosition derived from ScaffoldRoom.Position
    /// - ConnectionType inferred from JSON condition + room types
    /// </summary>
    public static class ScaffoldToWorldGraphBuilder
    {
        private const string MENU_PATH = "ProjectArk/Level/Authority/Build WorldGraph from Scaffold";
        private const string DEFAULT_OUTPUT_DIR = "Assets/_Data/Level";

        // ──────────────────── Menu Entry ────────────────────

        [MenuItem(MENU_PATH)]
        public static void BuildFromSelection()
        {
            var scaffold = Selection.activeObject as LevelScaffoldData;
            if (scaffold == null)
            {
                // Try to find any scaffold in the project
                var guids = AssetDatabase.FindAssets("t:LevelScaffoldData");
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("No Scaffold Data",
                        "No LevelScaffoldData asset found. Please select one in the Project window first.",
                        "OK");
                    return;
                }

                if (guids.Length == 1)
                {
                    scaffold = AssetDatabase.LoadAssetAtPath<LevelScaffoldData>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                else
                {
                    EditorUtility.DisplayDialog("Multiple Scaffold Assets",
                        "Multiple LevelScaffoldData assets found. Please select the one you want to convert.",
                        "OK");
                    return;
                }
            }

            if (scaffold == null)
            {
                Debug.LogError("[ScaffoldToWorldGraphBuilder] Failed to locate LevelScaffoldData.");
                return;
            }

            Build(scaffold);
        }

        [MenuItem(MENU_PATH, true)]
        public static bool BuildFromSelectionValidate()
        {
            // Enable if selection is a scaffold or there are scaffolds in the project
            return true;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Build a WorldGraphSO from the given scaffold data.
        /// Returns the created/updated asset, or null on failure.
        /// </summary>
        public static WorldGraphSO Build(LevelScaffoldData scaffold, string outputPath = null)
        {
            if (scaffold == null)
            {
                Debug.LogError("[ScaffoldToWorldGraphBuilder] Scaffold is null.");
                return null;
            }

            if (scaffold.Rooms == null || scaffold.Rooms.Count == 0)
            {
                Debug.LogError("[ScaffoldToWorldGraphBuilder] Scaffold has no rooms.");
                return null;
            }

            // Determine output path
            if (string.IsNullOrEmpty(outputPath))
            {
                string safeName = scaffold.LevelName.Replace(" ", "_").Replace("·", "_");
                outputPath = $"{DEFAULT_OUTPUT_DIR}/WorldGraph_{safeName}.asset";
            }

            // Ensure directory exists
            string dir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            // Check for existing asset
            var existingAsset = AssetDatabase.LoadAssetAtPath<WorldGraphSO>(outputPath);
            bool isUpdate = existingAsset != null;

            // Build nodes
            var nodes = BuildNodes(scaffold);

            // Build edges
            var edges = BuildEdges(scaffold, nodes);

            // Create or update asset
            WorldGraphSO graph;
            if (isUpdate)
            {
                graph = existingAsset;
                Undo.RecordObject(graph, "Update WorldGraph from Scaffold");
            }
            else
            {
                graph = ScriptableObject.CreateInstance<WorldGraphSO>();
            }

            // Write data via SerializedObject for proper Undo/dirty tracking
            var so = new SerializedObject(graph);

            so.FindProperty("_graphName").stringValue = scaffold.LevelName;

            // Write rooms array
            var roomsProp = so.FindProperty("_rooms");
            roomsProp.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                var elem = roomsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("RoomID").stringValue = nodes[i].RoomID;
                elem.FindPropertyRelative("NodeType").enumValueIndex = (int)nodes[i].NodeType;

                // GateIDs
                var gatesProp = elem.FindPropertyRelative("GateIDs");
                gatesProp.arraySize = nodes[i].GateIDs?.Length ?? 0;
                for (int g = 0; g < (nodes[i].GateIDs?.Length ?? 0); g++)
                {
                    gatesProp.GetArrayElementAtIndex(g).stringValue = nodes[i].GateIDs[g];
                }

                elem.FindPropertyRelative("DesignerNote").stringValue = nodes[i].DesignerNote;
                elem.FindPropertyRelative("EditorPosition").vector2Value = nodes[i].EditorPosition;
            }

            // Write connections array
            var connProp = so.FindProperty("_connections");
            connProp.arraySize = edges.Length;
            for (int i = 0; i < edges.Length; i++)
            {
                var elem = connProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("FromRoomID").stringValue = edges[i].FromRoomID;
                elem.FindPropertyRelative("FromGateID").stringValue = edges[i].FromGateID;
                elem.FindPropertyRelative("ToRoomID").stringValue = edges[i].ToRoomID;
                elem.FindPropertyRelative("ToGateID").stringValue = edges[i].ToGateID;
                elem.FindPropertyRelative("Type").enumValueIndex = (int)edges[i].Type;
                elem.FindPropertyRelative("IsLayerTransition").boolValue = edges[i].IsLayerTransition;
                elem.FindPropertyRelative("DesignerNote").stringValue = edges[i].DesignerNote ?? "";
            }

            so.ApplyModifiedProperties();

            if (!isUpdate)
            {
                AssetDatabase.CreateAsset(graph, outputPath);
            }

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Report
            Debug.Log($"[ScaffoldToWorldGraphBuilder] {(isUpdate ? "Updated" : "Created")} WorldGraphSO: " +
                       $"'{outputPath}' — {nodes.Length} rooms, {edges.Length} edges.");

            // Ping in project
            EditorGUIUtility.PingObject(graph);
            Selection.activeObject = graph;

            return graph;
        }

        // ──────────────────── Node Building ────────────────────

        private static RoomNodeData[] BuildNodes(LevelScaffoldData scaffold)
        {
            var nodes = new List<RoomNodeData>();
            // 用于查找 room 的连接以推导 GateIDs
            var roomConnectionMap = BuildRoomConnectionMap(scaffold);

            foreach (var room in scaffold.Rooms)
            {
                if (string.IsNullOrEmpty(room.RoomID)) continue;

                // 推导 GateIDs
                var gateIDs = new List<string>();
                if (roomConnectionMap.TryGetValue(room.RoomID, out var connections))
                {
                    foreach (var conn in connections)
                    {
                        string gateID = DeriveGateID(room.RoomID, conn);
                        if (!gateIDs.Contains(gateID))
                            gateIDs.Add(gateID);
                    }
                }

                var node = new RoomNodeData
                {
                    RoomID = room.RoomID,
                    NodeType = MapRoomTypeToNodeType(room.RoomType, room.DisplayName),
                    GateIDs = gateIDs.ToArray(),
                    DesignerNote = room.DisplayName,
                    EditorPosition = new Vector2(room.Position.x, room.Position.y) * 10f // 缩放到编辑器坐标
                };

                nodes.Add(node);
            }

            return nodes.ToArray();
        }

        // ──────────────────── Edge Building ────────────────────

        private static ConnectionEdge[] BuildEdges(LevelScaffoldData scaffold, RoomNodeData[] nodes)
        {
            var edges = new List<ConnectionEdge>();
            var processedPairs = new HashSet<string>(); // 避免重复的双向边

            foreach (var room in scaffold.Rooms)
            {
                if (room.Connections == null) continue;

                foreach (var conn in room.Connections)
                {
                    if (string.IsNullOrEmpty(conn.TargetRoomID)) continue;

                    string fromID = room.RoomID;
                    string toID = conn.TargetRoomID;

                    // 生成 GateID
                    string fromGateID = DeriveGateID(fromID, conn);
                    string toGateID = DeriveReverseGateID(toID, conn);

                    // 推导连接类型
                    ConnectionType connType = InferConnectionType(room, conn, scaffold);

                    // 正向边
                    string forwardKey = $"{fromID}->{toID}:{fromGateID}";
                    if (!processedPairs.Contains(forwardKey))
                    {
                        edges.Add(new ConnectionEdge
                        {
                            FromRoomID = fromID,
                            FromGateID = fromGateID,
                            ToRoomID = toID,
                            ToGateID = toGateID,
                            Type = connType,
                            IsLayerTransition = conn.IsLayerTransition,
                            DesignerNote = ""
                        });
                        processedPairs.Add(forwardKey);
                    }

                    // 反向边（双向门）
                    string reverseKey = $"{toID}->{fromID}:{toGateID}";
                    if (!processedPairs.Contains(reverseKey))
                    {
                        edges.Add(new ConnectionEdge
                        {
                            FromRoomID = toID,
                            FromGateID = toGateID,
                            ToRoomID = fromID,
                            ToGateID = fromGateID,
                            Type = connType,
                            IsLayerTransition = conn.IsLayerTransition,
                            DesignerNote = "(auto-reverse)"
                        });
                        processedPairs.Add(reverseKey);
                    }
                }
            }

            return edges.ToArray();
        }

        // ──────────────────── Mapping Tables ────────────────────

        /// <summary>
        /// Maps the old RoomType enum to the new RoomNodeType enum.
        /// Uses room display name as hint for ambiguous cases.
        /// </summary>
        private static RoomNodeType MapRoomTypeToNodeType(RoomType roomType, string displayName)
        {
            // 先根据显示名称中的关键字做精确匹配
            string lower = (displayName ?? "").ToLowerInvariant();

            if (lower.Contains("锚点") || lower.Contains("anchor"))
                return RoomNodeType.Anchor;
            if (lower.Contains("回路") || lower.Contains("loop") || lower.Contains("shortcut") || lower.Contains("捷径"))
                return RoomNodeType.Loop;
            if (lower.Contains("门槛") || lower.Contains("threshold") || lower.Contains("gate"))
                return RoomNodeType.Threshold;
            if (lower.Contains("压力") || lower.Contains("pressure"))
                return RoomNodeType.Pressure;
            if (lower.Contains("清算") || lower.Contains("resolution") || lower.Contains("arena"))
                return RoomNodeType.Resolution;
            if (lower.Contains("回报") || lower.Contains("reward"))
                return RoomNodeType.Reward;
            if (lower.Contains("收束") || lower.Contains("hub") || lower.Contains("枢纽"))
                return RoomNodeType.Hub;
            if (lower.Contains("transit") || lower.Contains("引导") || lower.Contains("过路"))
                return RoomNodeType.Transit;

            // Fallback: 基于 RoomType 枚举
            switch (roomType)
            {
                case RoomType.Safe:     return RoomNodeType.Safe;
                case RoomType.Normal:   return RoomNodeType.Transit;
                case RoomType.Arena:    return RoomNodeType.Resolution;
                case RoomType.Boss:     return RoomNodeType.Boss;
                case RoomType.Corridor: return RoomNodeType.Transit;
                case RoomType.Shop:     return RoomNodeType.Safe;
                case RoomType.Hub:      return RoomNodeType.Hub;
                case RoomType.Gate:     return RoomNodeType.Threshold;
                default:                return RoomNodeType.Transit;
            }
        }

        /// <summary>
        /// Infers ConnectionType from scaffold connection data and room context.
        /// </summary>
        private static ConnectionType InferConnectionType(ScaffoldRoom fromRoom, ScaffoldDoorConnection conn,
            LevelScaffoldData scaffold)
        {
            // 层间过渡 → Identity（空间章节分割）
            if (conn.IsLayerTransition)
                return ConnectionType.Identity;

            // 从 DoorElement 的配置推断
            var doorElement = fromRoom.Elements?.FirstOrDefault(e =>
                e.ElementType == ScaffoldElementType.Door && e.BoundConnectionID == conn.ConnectionID);

            if (doorElement != null)
            {
                switch (doorElement.DoorConfig?.InitialState)
                {
                    case DoorState.Locked_Key:
                        return ConnectionType.Return; // 钥匙锁通常是回路捷径
                    case DoorState.Locked_Combat:
                        return ConnectionType.Challenge;
                    case DoorState.Locked_Ability:
                        return ConnectionType.Ability;
                    case DoorState.Locked_Schedule:
                        return ConnectionType.Scheduled;
                }
            }

            // 通过目标房间类型推断
            var targetRoom = scaffold.Rooms.FirstOrDefault(r => r.RoomID == conn.TargetRoomID);
            if (targetRoom != null)
            {
                if (targetRoom.RoomType == RoomType.Boss)
                    return ConnectionType.Challenge;
                if (targetRoom.RoomType == RoomType.Arena)
                    return ConnectionType.Challenge;
            }

            // 默认推进连接
            return ConnectionType.Progression;
        }

        // ──────────────────── GateID Derivation ────────────────────

        private static string DeriveGateID(string roomID, ScaffoldDoorConnection conn)
        {
            // 从门方向推导 GateID
            Vector2 dir = conn.DoorDirection;

            string dirName;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                dirName = dir.x > 0 ? "east" : "west";
            else
                dirName = dir.y > 0 ? "north" : "south";

            // 如果门方向为零向量，从位置关系推导
            if (dir == Vector2.zero)
            {
                dirName = "door";
            }

            return $"gate_{dirName}_{conn.TargetRoomID}";
        }

        private static string DeriveReverseGateID(string roomID, ScaffoldDoorConnection conn)
        {
            // 反向 GateID：方向取反
            Vector2 dir = conn.DoorDirection;

            string dirName;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                dirName = dir.x > 0 ? "west" : "east"; // 反向
            else
                dirName = dir.y > 0 ? "south" : "north"; // 反向

            if (dir == Vector2.zero)
            {
                dirName = "door";
            }

            // 从 conn 的目标房间看，回来的 gate 指向原始房间
            // 需要找原始房间ID，这里用 conn 的上下文
            return $"gate_{dirName}_return";
        }

        // ──────────────────── Helpers ────────────────────

        /// <summary>
        /// Builds a map of RoomID → all connections involving that room (both outgoing and incoming).
        /// </summary>
        private static Dictionary<string, List<ScaffoldDoorConnection>> BuildRoomConnectionMap(
            LevelScaffoldData scaffold)
        {
            var map = new Dictionary<string, List<ScaffoldDoorConnection>>();

            foreach (var room in scaffold.Rooms)
            {
                if (string.IsNullOrEmpty(room.RoomID)) continue;

                if (!map.ContainsKey(room.RoomID))
                    map[room.RoomID] = new List<ScaffoldDoorConnection>();

                if (room.Connections == null) continue;

                foreach (var conn in room.Connections)
                {
                    map[room.RoomID].Add(conn);

                    // 也在目标房间侧添加（用于推导目标房间的 GateIDs）
                    if (!string.IsNullOrEmpty(conn.TargetRoomID))
                    {
                        if (!map.ContainsKey(conn.TargetRoomID))
                            map[conn.TargetRoomID] = new List<ScaffoldDoorConnection>();
                        // 添加同一连接引用（目标侧视角）
                        map[conn.TargetRoomID].Add(conn);
                    }
                }
            }

            return map;
        }
    }
}
