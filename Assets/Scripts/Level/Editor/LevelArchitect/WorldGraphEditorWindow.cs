using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.2]
    /// EditorWindow hosting the WorldGraphView for visual editing of WorldGraphSO assets.
    /// 
    /// Features:
    /// - Full node graph editor (drag nodes, create/delete connections)
    /// - Color-coded by RoomNodeType and ConnectionType
    /// - Right-click context menu for node creation
    /// - Auto-layout algorithm
    /// - Toolbar with Save/Load/Layout controls
    /// - Inspector side panel for selected node details
    /// - Import from LevelScaffoldData via ScaffoldToWorldGraphBuilder
    /// 
    /// Menu: ProjectArk/Level/Authority/World Graph Editor
    /// </summary>
    public class WorldGraphEditorWindow : EditorWindow
    {
        private const string MENU_PATH = "ProjectArk/Level/Authority/World Graph Editor";
        private const string WINDOW_TITLE = "World Graph Editor";

        // ──────────────────── Fields ────────────────────

        private WorldGraphView _graphView;
        private WorldGraphSO _currentGraph;
        private ObjectField _graphField;
        private VisualElement _inspectorPanel;
        private Label _statsLabel;

        // ──────────────────── Menu Item ────────────────────

        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<WorldGraphEditorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        /// <summary>
        /// Opens the editor with a specific WorldGraphSO asset loaded.
        /// </summary>
        public static void OpenWithGraph(WorldGraphSO graph)
        {
            var window = GetWindow<WorldGraphEditorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(800, 500);
            window.LoadGraph(graph);
            window.Show();
        }

        // ──────────────────── Lifecycle ────────────────────

        private void OnEnable()
        {
            BuildUI();
        }

        private void OnDisable()
        {
            // Auto-save on close
            if (_graphView != null && _currentGraph != null)
            {
                _graphView.SaveToGraph();
            }
        }

        // ──────────────────── UI Construction ────────────────────

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // ── Top toolbar ──
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.height = 30;
            toolbar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.alignItems = Align.Center;

            // Graph asset field
            var graphLabel = new Label("Graph:");
            graphLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            graphLabel.style.marginRight = 4;
            toolbar.Add(graphLabel);

            _graphField = new ObjectField();
            _graphField.objectType = typeof(WorldGraphSO);
            _graphField.style.width = 200;
            _graphField.RegisterValueChangedCallback(evt =>
            {
                LoadGraph(evt.newValue as WorldGraphSO);
            });
            toolbar.Add(_graphField);

            AddToolbarSpacer(toolbar, 8);

            // Save button
            var saveBtn = new Button(() => OnSaveClicked()) { text = "💾 Save" };
            saveBtn.style.width = 60;
            styleToolbarButton(saveBtn);
            toolbar.Add(saveBtn);

            AddToolbarSpacer(toolbar, 4);

            // Auto Layout button
            var layoutBtn = new Button(() => OnAutoLayoutClicked()) { text = "⬡ Layout" };
            layoutBtn.style.width = 70;
            styleToolbarButton(layoutBtn);
            toolbar.Add(layoutBtn);

            AddToolbarSpacer(toolbar, 4);

            // Frame All button
            var frameBtn = new Button(() => _graphView?.FrameAll()) { text = "🔍 Frame" };
            frameBtn.style.width = 70;
            styleToolbarButton(frameBtn);
            toolbar.Add(frameBtn);

            AddToolbarSpacer(toolbar, 4);

            // Import from Scaffold button
            var importBtn = new Button(() => OnImportFromScaffoldClicked()) { text = "📥 Import Scaffold" };
            importBtn.style.width = 120;
            styleToolbarButton(importBtn);
            toolbar.Add(importBtn);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Stats label
            _statsLabel = new Label("No graph loaded");
            _statsLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            _statsLabel.style.fontSize = 10;
            _statsLabel.style.marginRight = 8;
            toolbar.Add(_statsLabel);

            rootVisualElement.Add(toolbar);

            // ── Main content area (graph + inspector) ──
            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1;

            // Graph view
            _graphView = new WorldGraphView(this);
            _graphView.style.flexGrow = 1;
            mainArea.Add(_graphView);

            // Inspector side panel
            _inspectorPanel = BuildInspectorPanel();
            mainArea.Add(_inspectorPanel);

            rootVisualElement.Add(mainArea);

            // ── Bottom legend bar ──
            rootVisualElement.Add(BuildLegendBar());

            // ── Load initial graph if any ──
            if (_currentGraph != null)
            {
                _graphField.SetValueWithoutNotify(_currentGraph);
                _graphView.PopulateFromGraph(_currentGraph);
                UpdateStats();
            }
        }

        private VisualElement BuildInspectorPanel()
        {
            var panel = new VisualElement();
            panel.style.width = 250;
            panel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            panel.style.borderLeftWidth = 1;
            panel.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            panel.style.paddingLeft = 8;
            panel.style.paddingRight = 8;
            panel.style.paddingTop = 8;

            var title = new Label("Inspector");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.9f, 0.9f, 0.9f);
            title.style.marginBottom = 8;
            panel.Add(title);

            var hint = new Label("Select a node or edge to inspect.");
            hint.style.fontSize = 11;
            hint.style.color = new Color(0.6f, 0.6f, 0.6f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(hint);

            // Selection listener
            _graphView?.RegisterCallback<MouseUpEvent>(evt => UpdateInspectorPanel());

            return panel;
        }

        private VisualElement BuildLegendBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.height = 24;
            bar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            bar.style.paddingLeft = 8;
            bar.style.alignItems = Align.Center;

            // Node type legend
            AddLegendItem(bar, "Transit", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Transit));
            AddLegendItem(bar, "Pressure", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Pressure));
            AddLegendItem(bar, "Resolution", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Resolution));
            AddLegendItem(bar, "Reward", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Reward));
            AddLegendItem(bar, "Anchor", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Anchor));
            AddLegendItem(bar, "Loop", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Loop));
            AddLegendItem(bar, "Hub", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Hub));
            AddLegendItem(bar, "Boss", RoomGraphNode.GetNodeTypeColor(RoomNodeType.Boss));

            // Separator
            var sep = new VisualElement();
            sep.style.width = 1;
            sep.style.height = 16;
            sep.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            sep.style.marginLeft = 8;
            sep.style.marginRight = 8;
            bar.Add(sep);

            // Connection type legend
            AddLegendItem(bar, "Prog", ConnectionGraphEdge.GetConnectionTypeColor(ConnectionType.Progression));
            AddLegendItem(bar, "Return", ConnectionGraphEdge.GetConnectionTypeColor(ConnectionType.Return));
            AddLegendItem(bar, "Ability", ConnectionGraphEdge.GetConnectionTypeColor(ConnectionType.Ability));
            AddLegendItem(bar, "Challenge", ConnectionGraphEdge.GetConnectionTypeColor(ConnectionType.Challenge));

            return bar;
        }

        private void AddLegendItem(VisualElement parent, string label, Color color)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginRight = 8;

            var dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.backgroundColor = color;
            dot.style.borderTopLeftRadius = 4;
            dot.style.borderTopRightRadius = 4;
            dot.style.borderBottomLeftRadius = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight = 2;
            item.Add(dot);

            var text = new Label(label);
            text.style.fontSize = 9;
            text.style.color = new Color(0.7f, 0.7f, 0.7f);
            item.Add(text);

            parent.Add(item);
        }

        // ──────────────────── Graph Loading ────────────────────

        public void LoadGraph(WorldGraphSO graph)
        {
            // Auto-save previous graph
            if (_currentGraph != null && _graphView != null)
            {
                _graphView.SaveToGraph();
            }

            _currentGraph = graph;

            if (_graphField != null)
                _graphField.SetValueWithoutNotify(graph);

            if (_graphView != null)
            {
                _graphView.PopulateFromGraph(graph);
            }

            UpdateStats();
        }

        // ──────────────────── Button Handlers ────────────────────

        private void OnSaveClicked()
        {
            if (_graphView != null && _currentGraph != null)
            {
                _graphView.SaveToGraph();
                AssetDatabase.SaveAssets();
                Debug.Log("[WorldGraphEditor] Graph saved successfully.");
            }
            else
            {
                Debug.LogWarning("[WorldGraphEditor] No graph loaded to save.");
            }
        }

        private void OnAutoLayoutClicked()
        {
            if (_graphView != null && _currentGraph != null)
            {
                _graphView.AutoLayout();
            }
        }

        private void OnImportFromScaffoldClicked()
        {
            // Find scaffold assets
            var guids = AssetDatabase.FindAssets("t:LevelScaffoldData");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Scaffold",
                    "No LevelScaffoldData assets found in the project.", "OK");
                return;
            }

            // If only one, use it; otherwise show picker
            LevelScaffoldData scaffold;
            if (guids.Length == 1)
            {
                scaffold = AssetDatabase.LoadAssetAtPath<LevelScaffoldData>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            else
            {
                // Show a simple selection dialog
                var menu = new GenericMenu();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<LevelScaffoldData>(path);
                    if (asset == null) continue;

                    menu.AddItem(new GUIContent(asset.LevelName ?? path), false, () =>
                    {
                        ImportFromScaffold(asset);
                    });
                }
                menu.ShowAsContext();
                return;
            }

            ImportFromScaffold(scaffold);
        }

        private void ImportFromScaffold(LevelScaffoldData scaffold)
        {
            if (scaffold == null) return;

            var graph = ScaffoldToWorldGraphBuilder.Build(scaffold);
            if (graph != null)
            {
                LoadGraph(graph);
            }
        }

        // ──────────────────── Inspector Updates ────────────────────

        private void UpdateInspectorPanel()
        {
            if (_inspectorPanel == null || _graphView == null) return;

            _inspectorPanel.Clear();

            var title = new Label("Inspector");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.9f, 0.9f, 0.9f);
            title.style.marginBottom = 8;
            _inspectorPanel.Add(title);

            var selection = _graphView.selection;
            if (selection == null || selection.Count == 0)
            {
                var hint = new Label("Select a node or edge to inspect.");
                hint.style.fontSize = 11;
                hint.style.color = new Color(0.6f, 0.6f, 0.6f);
                hint.style.whiteSpace = WhiteSpace.Normal;
                _inspectorPanel.Add(hint);
                return;
            }

            var selected = selection[0];

            if (selected is RoomGraphNode roomNode)
            {
                DrawNodeInspector(roomNode);
            }
            else if (selected is ConnectionGraphEdge edge)
            {
                DrawEdgeInspector(edge);
            }
        }

        private void DrawNodeInspector(RoomGraphNode node)
        {
            // RoomID (read-only)
            AddInspectorField("Room ID", node.RoomID);

            // NodeType (editable dropdown)
            var nodeTypeField = new EnumField("Node Type", node.NodeType);
            nodeTypeField.RegisterValueChangedCallback(evt =>
            {
                node.NodeType = (RoomNodeType)evt.newValue;
                node.RefreshNodeTypeVisuals();
            });
            _inspectorPanel.Add(nodeTypeField);

            // Designer Note (editable)
            var noteField = new TextField("Note");
            noteField.value = node.DesignerNote ?? "";
            noteField.RegisterValueChangedCallback(evt =>
            {
                node.DesignerNote = evt.newValue;
            });
            _inspectorPanel.Add(noteField);

            // Gate count
            AddInspectorField("Gates", $"{node.GatePorts.Count}");

            // Position
            var rect = node.GetPosition();
            AddInspectorField("Position", $"({rect.x:F0}, {rect.y:F0})");

            // Connected rooms
            AddInspectorSpacer();
            var connTitle = new Label("Connections");
            connTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            connTitle.style.marginTop = 8;
            _inspectorPanel.Add(connTitle);

            foreach (var port in node.GatePorts.Values)
            {
                foreach (var edge in port.connections)
                {
                    var other = (edge.input.node == node) ? edge.output.node : edge.input.node;
                    if (other is RoomGraphNode otherRoom)
                    {
                        string dir = (edge.output.node == node) ? "→" : "←";
                        var connLabel = new Label($"  {dir} {otherRoom.RoomID}");
                        connLabel.style.fontSize = 11;
                        connLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                        _inspectorPanel.Add(connLabel);
                    }
                }
            }
        }

        private void DrawEdgeInspector(ConnectionGraphEdge edge)
        {
            var fromNode = edge.output?.node as RoomGraphNode;
            var toNode = edge.input?.node as RoomGraphNode;

            AddInspectorField("From", fromNode?.RoomID ?? "?");
            AddInspectorField("To", toNode?.RoomID ?? "?");
            AddInspectorField("From Gate", edge.FromGateID ?? "(auto)");
            AddInspectorField("To Gate", edge.ToGateID ?? "(auto)");

            // ConnectionType (editable)
            var typeField = new EnumField("Type", edge.ConnType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                edge.ConnType = (ConnectionType)evt.newValue;
                edge.ApplyConnectionStyle();
            });
            _inspectorPanel.Add(typeField);

            // Layer transition toggle
            var layerToggle = new Toggle("Layer Transition");
            layerToggle.value = edge.IsLayerTransition;
            layerToggle.RegisterValueChangedCallback(evt =>
            {
                edge.IsLayerTransition = evt.newValue;
                edge.ApplyConnectionStyle();
            });
            _inspectorPanel.Add(layerToggle);

            // Note
            var noteField = new TextField("Note");
            noteField.value = edge.DesignerNote ?? "";
            noteField.RegisterValueChangedCallback(evt =>
            {
                edge.DesignerNote = evt.newValue;
            });
            _inspectorPanel.Add(noteField);
        }

        // ──────────────────── Inspector Helpers ────────────────────

        private void AddInspectorField(string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;

            var lbl = new Label($"{label}:");
            lbl.style.width = 80;
            lbl.style.fontSize = 11;
            lbl.style.color = new Color(0.7f, 0.7f, 0.7f);
            row.Add(lbl);

            var val = new Label(value);
            val.style.fontSize = 11;
            val.style.color = new Color(0.9f, 0.9f, 0.9f);
            row.Add(val);

            _inspectorPanel.Add(row);
        }

        private void AddInspectorSpacer()
        {
            var spacer = new VisualElement();
            spacer.style.height = 4;
            _inspectorPanel.Add(spacer);
        }

        // ──────────────────── Utility ────────────────────

        private void UpdateStats()
        {
            if (_statsLabel == null) return;

            if (_currentGraph == null)
            {
                _statsLabel.text = "No graph loaded";
            }
            else
            {
                _statsLabel.text = $"{_currentGraph.GraphName} • {_currentGraph.RoomCount} rooms, {_currentGraph.ConnectionCount} edges";
            }
        }

        private static void AddToolbarSpacer(VisualElement toolbar, float width)
        {
            var spacer = new VisualElement();
            spacer.style.width = width;
            toolbar.Add(spacer);
        }

        private static void styleToolbarButton(Button btn)
        {
            btn.style.height = 22;
            btn.style.fontSize = 11;
        }
    }
}
