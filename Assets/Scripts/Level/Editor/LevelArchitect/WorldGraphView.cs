using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.2]
    /// The GraphView that renders and edits a WorldGraphSO as a node graph.
    /// Supports drag-to-reposition nodes, create/delete nodes and connections,
    /// right-click context menus, auto-layout, and persistence back to the SO.
    /// </summary>
    public class WorldGraphView : GraphView
    {
        // ──────────────────── Fields ────────────────────

        private WorldGraphSO _graph;
        private WorldGraphEditorWindow _window;
        private Dictionary<string, RoomGraphNode> _nodeMap = new Dictionary<string, RoomGraphNode>();

        // ──────────────────── Construction ────────────────────

        public WorldGraphView(WorldGraphEditorWindow window)
        {
            _window = window;

            // Standard GraphView manipulators
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContentZoomer());

            // Figma-like Space+LMB pan (also supports MMB drag)
            this.AddManipulator(new SpacePanManipulator());

            // Grid background
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Style
            SetupZoom(0.1f, 3f);
            style.flexGrow = 1;

            // Node creation callback (from edge drag)
            nodeCreationRequest = OnNodeCreationRequest;

            // Graph changes callback
            graphViewChanged = OnGraphViewChanged;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Load and display a WorldGraphSO in the graph view.
        /// </summary>
        public void PopulateFromGraph(WorldGraphSO graph)
        {
            _graph = graph;
            _nodeMap.Clear();

            // Clear existing elements
            DeleteElements(graphElements.ToList());

            if (_graph == null) return;

            // Create nodes
            for (int i = 0; i < _graph.Rooms.Length; i++)
            {
                var roomData = _graph.Rooms[i];
                if (string.IsNullOrEmpty(roomData.RoomID)) continue;

                var node = new RoomGraphNode(roomData, i);
                _nodeMap[roomData.RoomID] = node;
                AddElement(node);
            }

            // Create edges
            for (int i = 0; i < _graph.Connections.Length; i++)
            {
                var conn = _graph.Connections[i];
                CreateEdgeFromConnection(conn, i);
            }

            // Frame all content
            schedule.Execute(() => FrameAll());
        }

        /// <summary>
        /// Save all node positions and graph changes back to the WorldGraphSO.
        /// </summary>
        public void SaveToGraph()
        {
            if (_graph == null) return;

            Undo.RecordObject(_graph, "Save World Graph Layout");

            var so = new SerializedObject(_graph);

            // Save node positions
            var roomsProp = so.FindProperty("_rooms");
            foreach (var kvp in _nodeMap)
            {
                var node = kvp.Value;
                if (node.RoomIndex >= 0 && node.RoomIndex < roomsProp.arraySize)
                {
                    var elem = roomsProp.GetArrayElementAtIndex(node.RoomIndex);
                    var rect = node.GetPosition();
                    elem.FindPropertyRelative("EditorPosition").vector2Value =
                        new Vector2(rect.x, rect.y);

                    // Also save NodeType changes
                    elem.FindPropertyRelative("NodeType").enumValueIndex = (int)node.NodeType;
                    elem.FindPropertyRelative("DesignerNote").stringValue = node.DesignerNote ?? "";
                }
            }

            // Rebuild edges from current graph state
            var currentEdges = edges.ToList().OfType<ConnectionGraphEdge>().ToList();
            var connProp = so.FindProperty("_connections");
            connProp.arraySize = currentEdges.Count;

            for (int i = 0; i < currentEdges.Count; i++)
            {
                var edge = currentEdges[i];
                var fromNode = edge.output?.node as RoomGraphNode;
                var toNode = edge.input?.node as RoomGraphNode;

                if (fromNode == null || toNode == null) continue;

                var elem = connProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("FromRoomID").stringValue = fromNode.RoomID;
                elem.FindPropertyRelative("FromGateID").stringValue = edge.FromGateID ?? "";
                elem.FindPropertyRelative("ToRoomID").stringValue = toNode.RoomID;
                elem.FindPropertyRelative("ToGateID").stringValue = edge.ToGateID ?? "";
                elem.FindPropertyRelative("Type").enumValueIndex = (int)edge.ConnType;
                elem.FindPropertyRelative("IsLayerTransition").boolValue = edge.IsLayerTransition;
                elem.FindPropertyRelative("DesignerNote").stringValue = edge.DesignerNote ?? "";
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_graph);

            Debug.Log($"[WorldGraphEditor] Saved: {_nodeMap.Count} nodes, {currentEdges.Count} edges.");
        }

        /// <summary>
        /// Auto-layout nodes using a simple force-directed algorithm.
        /// </summary>
        public void AutoLayout()
        {
            if (_nodeMap.Count == 0) return;

            var positions = new Dictionary<string, Vector2>();

            // Initialize positions from current layout
            foreach (var kvp in _nodeMap)
            {
                var rect = kvp.Value.GetPosition();
                positions[kvp.Key] = new Vector2(rect.x, rect.y);
            }

            // Build adjacency
            var adjacency = new Dictionary<string, HashSet<string>>();
            foreach (var kvp in _nodeMap)
                adjacency[kvp.Key] = new HashSet<string>();

            foreach (var edge in edges.ToList().OfType<ConnectionGraphEdge>())
            {
                var fromNode = edge.output?.node as RoomGraphNode;
                var toNode = edge.input?.node as RoomGraphNode;
                if (fromNode == null || toNode == null) continue;

                adjacency[fromNode.RoomID].Add(toNode.RoomID);
                adjacency[toNode.RoomID].Add(fromNode.RoomID);
            }

            // Simple force-directed layout (50 iterations)
            const float REPULSION = 50000f;
            const float ATTRACTION = 0.01f;
            const float IDEAL_DISTANCE = 300f;
            const float DAMPING = 0.9f;

            var velocities = new Dictionary<string, Vector2>();
            foreach (var key in positions.Keys)
                velocities[key] = Vector2.zero;

            for (int iter = 0; iter < 50; iter++)
            {
                var forces = new Dictionary<string, Vector2>();
                foreach (var key in positions.Keys)
                    forces[key] = Vector2.zero;

                // Repulsion between all pairs
                var keys = positions.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    for (int j = i + 1; j < keys.Count; j++)
                    {
                        Vector2 diff = positions[keys[i]] - positions[keys[j]];
                        float dist = Mathf.Max(diff.magnitude, 1f);
                        Vector2 force = diff.normalized * (REPULSION / (dist * dist));

                        forces[keys[i]] += force;
                        forces[keys[j]] -= force;
                    }
                }

                // Attraction along edges
                foreach (var kvp in adjacency)
                {
                    foreach (var neighbor in kvp.Value)
                    {
                        if (!positions.ContainsKey(neighbor)) continue;

                        Vector2 diff = positions[neighbor] - positions[kvp.Key];
                        float dist = diff.magnitude;
                        float displacement = dist - IDEAL_DISTANCE;

                        Vector2 force = diff.normalized * (displacement * ATTRACTION);
                        forces[kvp.Key] += force;
                    }
                }

                // Apply forces
                foreach (var key in keys)
                {
                    velocities[key] = (velocities[key] + forces[key]) * DAMPING;
                    positions[key] += velocities[key];
                }
            }

            // Apply positions to nodes
            Undo.RecordObject(_graph, "Auto Layout World Graph");

            foreach (var kvp in _nodeMap)
            {
                if (positions.TryGetValue(kvp.Key, out var pos))
                {
                    kvp.Value.SetPosition(new Rect(pos.x, pos.y, 200, 150));
                }
            }

            SaveToGraph();
            schedule.Execute(() => FrameAll());
        }

        // ──────────────────── Node Creation ────────────────────

        /// <summary>
        /// Add a new room node to the graph.
        /// </summary>
        public void AddNewRoom(Vector2 position, string roomID = null, RoomNodeType nodeType = RoomNodeType.Transit)
        {
            if (_graph == null) return;

            // Generate unique ID if not provided
            if (string.IsNullOrEmpty(roomID))
            {
                roomID = $"ROOM_{_graph.RoomCount + 1:D3}";
                int counter = 1;
                while (_nodeMap.ContainsKey(roomID))
                {
                    counter++;
                    roomID = $"ROOM_{_graph.RoomCount + counter:D3}";
                }
            }

            if (_nodeMap.ContainsKey(roomID))
            {
                Debug.LogWarning($"[WorldGraphEditor] Room '{roomID}' already exists!");
                return;
            }

            var newData = new RoomNodeData
            {
                RoomID = roomID,
                NodeType = nodeType,
                GateIDs = new string[] { "gate_west_in", "gate_east_out" },
                DesignerNote = "New Room",
                EditorPosition = position
            };

            // Add to SO
            Undo.RecordObject(_graph, "Add Room Node");

            var so = new SerializedObject(_graph);
            var roomsProp = so.FindProperty("_rooms");
            int newIndex = roomsProp.arraySize;
            roomsProp.arraySize = newIndex + 1;

            var elem = roomsProp.GetArrayElementAtIndex(newIndex);
            elem.FindPropertyRelative("RoomID").stringValue = newData.RoomID;
            elem.FindPropertyRelative("NodeType").enumValueIndex = (int)newData.NodeType;

            var gatesProp = elem.FindPropertyRelative("GateIDs");
            gatesProp.arraySize = newData.GateIDs.Length;
            for (int i = 0; i < newData.GateIDs.Length; i++)
                gatesProp.GetArrayElementAtIndex(i).stringValue = newData.GateIDs[i];

            elem.FindPropertyRelative("DesignerNote").stringValue = newData.DesignerNote;
            elem.FindPropertyRelative("EditorPosition").vector2Value = newData.EditorPosition;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_graph);

            // Create visual node
            var node = new RoomGraphNode(newData, newIndex);
            _nodeMap[roomID] = node;
            AddElement(node);
        }

        // ──────────────────── Compatibility Overrides ────────────────────

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(p =>
                p.direction != startPort.direction &&
                p.node != startPort.node
            ).ToList();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            var localPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.InsertAction(0, "Create Room Node/Transit",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Transit));
            evt.menu.InsertAction(1, "Create Room Node/Pressure",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Pressure));
            evt.menu.InsertAction(2, "Create Room Node/Resolution",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Resolution));
            evt.menu.InsertAction(3, "Create Room Node/Reward",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Reward));
            evt.menu.InsertAction(4, "Create Room Node/Anchor",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Anchor));
            evt.menu.InsertAction(5, "Create Room Node/Loop",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Loop));
            evt.menu.InsertAction(6, "Create Room Node/Hub",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Hub));
            evt.menu.InsertAction(7, "Create Room Node/Threshold",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Threshold));
            evt.menu.InsertAction(8, "Create Room Node/Safe",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Safe));
            evt.menu.InsertAction(9, "Create Room Node/Boss",
                a => AddNewRoom(localPos, nodeType: RoomNodeType.Boss));

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Auto Layout", a => AutoLayout());
            evt.menu.AppendAction("Frame All", a => FrameAll());
        }

        // ──────────────────── Callbacks ────────────────────

        private void OnNodeCreationRequest(NodeCreationContext ctx)
        {
            var localPos = contentViewContainer.WorldToLocal(ctx.screenMousePosition);
            AddNewRoom(localPos);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            // Handle node/edge deletions
            if (changes.elementsToRemove != null)
            {
                foreach (var elem in changes.elementsToRemove)
                {
                    if (elem is RoomGraphNode roomNode)
                    {
                        _nodeMap.Remove(roomNode.RoomID);
                    }
                }

                // Mark dirty for save
                if (_graph != null)
                    EditorUtility.SetDirty(_graph);
            }

            // Handle new edges created by drag
            if (changes.edgesToCreate != null)
            {
                foreach (var edge in changes.edgesToCreate)
                {
                    // Wrap standard Edge in our ConnectionGraphEdge if needed
                    if (edge is not ConnectionGraphEdge)
                    {
                        // The edge is already being added by GraphView; we just tag it
                        // We'll handle this in SaveToGraph
                    }
                }
            }

            // Handle node moves
            if (changes.movedElements != null)
            {
                // Position changes are auto-saved via SaveToGraph
            }

            return changes;
        }

        // ──────────────────── Edge Creation ────────────────────

        private void CreateEdgeFromConnection(ConnectionEdge conn, int edgeIndex)
        {
            if (!_nodeMap.TryGetValue(conn.FromRoomID, out var fromNode)) return;
            if (!_nodeMap.TryGetValue(conn.ToRoomID, out var toNode)) return;

            var outputPort = fromNode.GetPortForGate(conn.FromGateID, true);
            var inputPort = toNode.GetPortForGate(conn.ToGateID, false);

            if (outputPort == null || inputPort == null) return;

            var edge = new ConnectionGraphEdge
            {
                EdgeIndex = edgeIndex,
                ConnType = conn.Type,
                IsLayerTransition = conn.IsLayerTransition,
                FromGateID = conn.FromGateID,
                ToGateID = conn.ToGateID,
                DesignerNote = conn.DesignerNote
            };

            edge.output = outputPort;
            edge.input = inputPort;

            outputPort.Connect(edge);
            inputPort.Connect(edge);

            AddElement(edge);

            // Apply color styling after adding to view
            schedule.Execute(() => edge.ApplyConnectionStyle());
        }
    }
}
