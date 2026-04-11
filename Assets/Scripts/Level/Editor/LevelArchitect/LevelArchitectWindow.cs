using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Main entry point for the Level Architect Tool.
    /// Three-tab layout: Design | Build | Validate.
    /// - Design: optional LevelDesigner.html shortcut + JSON import
    /// - Build:  primary whitebox authoring path (Select/Blockout/Connect) + room list + quick play
    /// - Validate: validation results + auto-fix aligned with current Level guardrails
    /// </summary>
    public class LevelArchitectWindow : EditorWindow
    {
        // ──────────────────── Constants ────────────────────

        private const string WINDOW_TITLE = "Level Architect";
        private const string MENU_PATH = "ProjectArk/Level/Authority/Level Architect";
        private const float SIDE_PANEL_WIDTH = 260f;
        private const float SIDE_PANEL_MARGIN = 10f;
        private const float TOOLBAR_HEIGHT = 30f;

        // ──────────────────── Tab ────────────────────

        private enum Tab { Design, Build, Validate }
        private static readonly string[] TAB_LABELS = { "🎨 Design", "🔨 Build", "✅ Validate" };
        [SerializeField] private Tab _activeTab = Tab.Build;

        // ──────────────────── Tool Modes ────────────────────

        public enum ToolMode
        {
            Select,
            Blockout,
            Connect
        }

        // ──────────────────── Serialized State ────────────────────

        [SerializeField] private ToolMode _currentMode = ToolMode.Select;
        [SerializeField] private bool _sidePanelExpanded = true;
        [SerializeField] private bool _showPacingOverlay;
        [SerializeField] private bool _showCriticalPath;
        [SerializeField] private bool _showLockKeyGraph;
        [SerializeField] private bool _showConnectionTypes;
        [SerializeField] private int _activeFloorLevel = int.MinValue; // MinValue = show all

        // ──────────────────── Runtime State ────────────────────

        private bool _isActive;
        private Vector2 _sidePanelScroll;
        private List<Room> _selectedRooms = new List<Room>();
        private Room _hoveredRoom;
        private readonly Dictionary<int, RoomAuthoringState> _roomAuthoringStates = new Dictionary<int, RoomAuthoringState>();

        private struct RoomAuthoringState
        {
            public readonly Rect Bounds;
            public readonly int FloorLevel;

            public RoomAuthoringState(Rect bounds, int floorLevel)
            {
                Bounds = bounds;
                FloorLevel = floorLevel;
            }
        }

        // ──────────────────── Sub-Systems ────────────────────

        internal static LevelArchitectWindow Instance { get; private set; }

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Currently active tool mode. </summary>
        public ToolMode CurrentMode => _currentMode;

        /// <summary> Whether the tool is currently active and injecting into SceneView. </summary>
        public bool IsActive => _isActive;

        /// <summary> Currently selected rooms. </summary>
        public List<Room> SelectedRooms => _selectedRooms;

        /// <summary> Currently hovered room. </summary>
        public Room HoveredRoom => _hoveredRoom;

        /// <summary> Active floor level filter (int.MinValue = show all). </summary>
        public int ActiveFloorLevel => _activeFloorLevel;

        /// <summary> Whether pacing overlay is enabled. </summary>
        public bool ShowPacingOverlay => _showPacingOverlay;

        /// <summary> Whether critical path overlay is enabled. </summary>
        public bool ShowCriticalPath => _showCriticalPath;

        /// <summary> Whether lock-key graph overlay is enabled. </summary>
        public bool ShowLockKeyGraph => _showLockKeyGraph;

        /// <summary> Whether connection type color overlay is enabled. </summary>
        public bool ShowConnectionTypes => _showConnectionTypes;

        // ──────────────────── Menu Item ────────────────────

        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelArchitectWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

        // ──────────────────── Lifecycle ────────────────────

        private void OnEnable()
        {
            Instance = this;
            Activate();
        }

        private void OnDisable()
        {
            Deactivate();
            if (Instance == this) Instance = null;
        }

        private void OnDestroy()
        {
            Deactivate();
            if (Instance == this) Instance = null;
        }

        // ──────────────────── Activation ────────────────────

        private void Activate()
        {
            if (_isActive) return;
            _isActive = true;

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.undoRedoPerformed += OnUndoRedo;

            SceneView.RepaintAll();
        }

        private void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;

            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;

            SceneView.RepaintAll();
        }

        // ──────────────────── EditorWindow GUI ────────────────────

        private void OnGUI()
        {
            DrawWindowHeader();
            EditorGUILayout.Space(4);

            // ── Tab Bar ──
            _activeTab = (Tab)GUILayout.Toolbar((int)_activeTab, TAB_LABELS, GUILayout.Height(26));
            EditorGUILayout.Space(4);

            switch (_activeTab)
            {
                case Tab.Design:   DrawDesignTab();   break;
                case Tab.Build:    DrawBuildTab();    break;
                case Tab.Validate: DrawValidateTab(); break;
            }
        }

        // ──────────────────── Design Tab ────────────────────

        private void DrawDesignTab()
        {
            EditorGUILayout.LabelField("Level Designer (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use LevelDesigner.html only when you want a browser-side topology draft or JSON import source. For minimal validation slices and current room-runtime verification, Build is the preferred one-stop authoring path.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("🌐 Open LevelDesigner.html", GUILayout.Height(30)))
            {
                string htmlPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath),
                    "Tools", "LevelDesigner.html");
                Application.OpenURL("file://" + htmlPath.Replace("\\", "/"));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Import from JSON", EditorStyles.boldLabel);

            if (GUILayout.Button("📂 Import LevelDesigner JSON...", GUILayout.Height(28)))
            {
                LevelSliceBuilder.ImportFromJson();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Optional HTML Workflow", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "1. Open LevelDesigner.html in browser\n" +
                "2. Sketch room topology & connections\n" +
                "3. Set Level Name in the right panel\n" +
                "4. Click 💾 Export File to save JSON\n" +
                "5. Click Import LevelDesigner JSON here\n" +
                "6. Switch to Build tab to refine scene objects and validation rooms",
                EditorStyles.helpBox);
        }

        // ──────────────────── Build Tab ────────────────────

        private void DrawBuildTab()
        {
            EditorGUILayout.HelpBox(
                "Primary whitebox authoring path. Build creates and refines Room / RoomSO / Door skeletons directly in scene. Encounter triggers, checkpoints, locks, ambience triggers, and other runtime elements should be added in-scene after the room skeleton is in place.",
                MessageType.Info);
            EditorGUILayout.Space(4);
            DrawModeSelector();
            EditorGUILayout.Space(4);
            DrawOverlayToggles();
            EditorGUILayout.Space(4);
            DrawFloorLevelSelector();
            EditorGUILayout.Space(8);
            DrawBuildActions();
        }

        // ──────────────────── Validate Tab ────────────────────

        private void DrawValidateTab()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate All", GUILayout.Height(24)))
            {
                LevelValidator.ValidateAll();
                Repaint();
            }
            if (GUILayout.Button("Validate Scene", GUILayout.Height(24)))
            {
                LevelValidator.ValidateAll();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            var results = LevelValidator.LastResults;
            if (results.Count > 0)
            {
                int errors = 0, warnings = 0;
                foreach (var r in results)
                {
                    if (r.Severity == LevelValidator.Severity.Error) errors++;
                    else if (r.Severity == LevelValidator.Severity.Warning) warnings++;
                }

                EditorGUILayout.HelpBox($"{errors} error(s), {warnings} warning(s)",
                    errors > 0 ? MessageType.Error : MessageType.Warning);

                if (GUILayout.Button("Auto-Fix All", GUILayout.Height(20)))
                {
                    LevelValidator.AutoFixAll();
                    Repaint();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

                _sidePanelScroll = EditorGUILayout.BeginScrollView(_sidePanelScroll);
                foreach (var result in results)
                {
                    EditorGUILayout.BeginHorizontal();
                    Color iconColor = result.Severity == LevelValidator.Severity.Error ? Color.red :
                        result.Severity == LevelValidator.Severity.Warning ? Color.yellow : Color.cyan;
                    var prevColor = GUI.color;
                    GUI.color = iconColor;
                    GUILayout.Label("●", GUILayout.Width(12));
                    GUI.color = prevColor;
                    GUILayout.Label(result.Message, EditorStyles.miniLabel);
                    if (result.CanAutoFix && result.FixAction != null)
                    {
                        if (GUILayout.Button("Fix", GUILayout.Width(30)))
                        {
                            result.FixAction();
                            LevelValidator.ValidateAll();
                        }
                    }
                    else if (result.TargetObject != null)
                    {
                        if (GUILayout.Button("→", GUILayout.Width(20)))
                        {
                            Selection.activeObject = result.TargetObject;
                            SceneView.lastActiveSceneView?.FrameSelected();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No validation results yet. Click Validate All.", MessageType.None);
            }
        }

        private void DrawWindowHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(WINDOW_TITLE, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var statusColor = _isActive ? Color.green : Color.red;
            var prevColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(_isActive ? "● Active" : "● Inactive", EditorStyles.miniLabel);
            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeSelector()
        {
            EditorGUILayout.LabelField("Tool Mode", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (DrawModeButton("Select", ToolMode.Select, "d_RectTool"))
                SetMode(ToolMode.Select);
            if (DrawModeButton("Blockout", ToolMode.Blockout, "d_Grid.BoxTool"))
                SetMode(ToolMode.Blockout);
            if (DrawModeButton("Connect", ToolMode.Connect, "d_Linked"))
                SetMode(ToolMode.Connect);

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawModeButton(string label, ToolMode mode, string iconName)
        {
            var isActive = _currentMode == mode;

            var prevBg = GUI.backgroundColor;
            if (isActive) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);

            var icon = EditorGUIUtility.IconContent(iconName);
            var content = new GUIContent(" " + label, icon.image);
            bool clicked = GUILayout.Button(content, GUILayout.Height(28));

            GUI.backgroundColor = prevBg;
            return clicked;
        }

        private void DrawOverlayToggles()
        {
            EditorGUILayout.LabelField("Overlays", EditorStyles.boldLabel);

            _showPacingOverlay = EditorGUILayout.Toggle("Pacing Overlay", _showPacingOverlay);
            _showCriticalPath = EditorGUILayout.Toggle("Critical Path", _showCriticalPath);
            _showLockKeyGraph = EditorGUILayout.Toggle("Lock-Key Graph", _showLockKeyGraph);
            _showConnectionTypes = EditorGUILayout.Toggle("Connection Types", _showConnectionTypes);

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawFloorLevelSelector()
        {
            EditorGUILayout.LabelField("Floor Level", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("All", _activeFloorLevel == int.MinValue ? EditorStyles.toolbarButton : EditorStyles.miniButton))
            {
                _activeFloorLevel = int.MinValue;
                SceneView.RepaintAll();
            }

            // Show floor buttons from -3 to +1
            for (int i = 1; i >= -3; i--)
            {
                string floorLabel = i == 0 ? "G" : (i > 0 ? $"+{i}" : $"{i}");
                var style = (_activeFloorLevel == i) ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(floorLabel, style, GUILayout.Width(30)))
                {
                    _activeFloorLevel = i;
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBuildActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Create / Verify Built-in Presets", GUILayout.Height(22)))
            {
                RoomFactory.CreateBuiltInPresets();
                Debug.Log("[LevelArchitect] Built-in presets created/verified.");
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Quick Play is a structure smoke test. If RoomManager or DoorTransitionController is missing, the tool will create temporary _QuickPlay_* helpers before entering Play Mode.",
                MessageType.None);

            if (GUILayout.Button("Quick Play ▶", GUILayout.Height(28)))
            {
                BlockoutModeHandler.QuickPlay();
            }
        }

        // ──────────────────── SceneView Integration ────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive) return;

            // Lightweight validation (fatal issues only)
            LevelValidator.LightweightCheck();

            // Draw room blockouts and handle room interaction
            RoomBlockoutRenderer.DrawAndInteract(sceneView);

            // Draw SceneView toolbar overlay
            DrawSceneViewToolbar(sceneView);

            // Draw side panel (if expanded)
            if (_sidePanelExpanded)
            {
                DrawSceneViewSidePanel(sceneView);
            }

            // Process input events based on current mode
            ProcessSceneInput(sceneView);

            // Re-sync authoring-owned door state when room bounds or floor metadata changes.
            TrackRoomAuthoringChanges();

            // Force SceneView to repaint for continuous updates
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }
        }

        // ──────────────────── SceneView Toolbar ────────────────────

        private void DrawSceneViewToolbar(SceneView sceneView)
        {
            Handles.BeginGUI();

            float toolbarWidth = 320f;
            float toolbarX = (sceneView.position.width - toolbarWidth) / 2f;

            var toolbarRect = new Rect(toolbarX, 8, toolbarWidth, TOOLBAR_HEIGHT);
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

            GUILayout.BeginArea(new Rect(toolbarRect.x + 4, toolbarRect.y + 2, toolbarRect.width - 8, toolbarRect.height - 4));
            GUILayout.BeginHorizontal();

            // Mode buttons
            DrawSceneToolbarButton("Select", ToolMode.Select);
            DrawSceneToolbarButton("Blockout", ToolMode.Blockout);
            DrawSceneToolbarButton("Connect", ToolMode.Connect);

            GUILayout.FlexibleSpace();

            // Toggle side panel
            if (GUILayout.Button(_sidePanelExpanded ? "◀ Panel" : "▶ Panel", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _sidePanelExpanded = !_sidePanelExpanded;
                sceneView.Repaint();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private void DrawSceneToolbarButton(string label, ToolMode mode)
        {
            var prevBg = GUI.backgroundColor;
            if (_currentMode == mode)
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);

            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                SetMode(mode);
            }

            GUI.backgroundColor = prevBg;
        }

        // ──────────────────── SceneView Side Panel ────────────────────

        private void DrawSceneViewSidePanel(SceneView sceneView)
        {
            Handles.BeginGUI();

            float panelHeight = sceneView.position.height - TOOLBAR_HEIGHT - 60;
            var panelRect = new Rect(
                SIDE_PANEL_MARGIN,
                TOOLBAR_HEIGHT + 20,
                SIDE_PANEL_WIDTH,
                panelHeight
            );

            // Background
            EditorGUI.DrawRect(panelRect, new Color(0.2f, 0.2f, 0.2f, 0.9f));

            GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
            _sidePanelScroll = GUILayout.BeginScrollView(_sidePanelScroll);

            DrawSidePanelContent();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private void DrawSidePanelContent()
        {
            // ── Room List ──
            GUILayout.Label("Rooms in Scene", EditorStyles.boldLabel);

            var rooms = FindObjectsByType<Room>();
            if (rooms.Length == 0)
            {
                GUILayout.Label("No rooms found in scene.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var room in rooms)
                {
                    if (room == null) continue;

                    // Floor filter
                    if (_activeFloorLevel != int.MinValue)
                    {
                        int roomFloor = room.Data != null ? room.Data.FloorLevel : 0;
                        if (roomFloor != _activeFloorLevel) continue;
                    }

                    EditorGUILayout.BeginHorizontal();

                    // Color indicator
                    var prevColor = GUI.color;
                    GUI.color = GetRoomNodeTypeColor(room.NodeType);
                    GUILayout.Label("■", GUILayout.Width(14));
                    GUI.color = prevColor;

                    // Room name button (select on click)
                    bool isSelected = _selectedRooms.Contains(room);
                    var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button(room.RoomID, style))
                    {
                        HandleRoomListClick(room);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(8);

            // ── Selection Info ──
            if (_selectedRooms.Count > 0)
            {
                GUILayout.Label($"Selected: {_selectedRooms.Count} room(s)", EditorStyles.boldLabel);

                if (_selectedRooms.Count == 1)
                {
                    DrawSingleRoomInfo(_selectedRooms[0]);
                }
                else
                {
                    BatchEditPanel.DrawBatchEditPanel(_selectedRooms);
                }

                // Right-click context menu
                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.ContextClick && lastRect.Contains(Event.current.mousePosition))
                {
                    BatchEditPanel.ShowContextMenu(_selectedRooms);
                    Event.current.Use();
                }
            }

            GUILayout.Space(8);

            // ── Add Room Button ──
            GUILayout.Label("Add Room", EditorStyles.boldLabel);

            var presets = RoomFactory.FindAllPresets();
            if (presets.Length == 0)
            {
                if (GUILayout.Button("Create Built-in Presets First", GUILayout.Height(24)))
                {
                    RoomFactory.CreateBuiltInPresets();
                }
            }
            else
            {
                foreach (var preset in presets)
                {
                    if (preset == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    var prevColor = GUI.color;
                    GUI.color = LevelArchitectWindow.GetRoomTypeColor(preset.NodeTypeValue);
                    GUILayout.Label("■", GUILayout.Width(14));
                    GUI.color = prevColor;

                    string label = $"{preset.PresetName} ({preset.DefaultSize.x}×{preset.DefaultSize.y})";
                    if (GUILayout.Button(label, EditorStyles.miniButton))
                    {
                        // Place at SceneView center
                        var sv = SceneView.lastActiveSceneView;
                        Vector3 placePos = sv != null ? sv.camera.transform.position : Vector3.zero;
                        placePos.z = 0;
                        RoomFactory.CreateRoomFromPreset(preset, placePos);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(8);

            // ── Quick Validate (compact) ──
            if (GUILayout.Button("✅ Validate All", GUILayout.Height(22)))
            {
                LevelValidator.ValidateAll();
                var window = LevelArchitectWindow.Instance;
                if (window != null) window.Repaint();
            }

            var validationResults = LevelValidator.LastResults;
            if (validationResults.Count > 0)
            {
                int errors = 0, warnings = 0;
                foreach (var r in validationResults)
                {
                    if (r.Severity == LevelValidator.Severity.Error) errors++;
                    else if (r.Severity == LevelValidator.Severity.Warning) warnings++;
                }
                EditorGUILayout.HelpBox($"{errors} error(s), {warnings} warning(s) — see Validate tab",
                    errors > 0 ? MessageType.Error : MessageType.Warning);
            }
        }

        private void DrawSingleRoomInfo(Room room)
        {
            if (room == null) return;

            EditorGUILayout.BeginVertical("HelpBox");

            GUILayout.Label($"ID: {room.RoomID}");
            GUILayout.Label($"Node Type: {room.NodeType}");

            if (room.Data != null)
            {
                GUILayout.Label($"Explicit Node: {room.Data.NodeType}");
                GUILayout.Label($"Floor: {room.Data.FloorLevel}");
                GUILayout.Label($"SO: {room.Data.name}");

                if (room.Data.Encounter != null)
                {
                    GUILayout.Label($"Encounter: {room.Data.Encounter.name} ({room.Data.Encounter.WaveCount} waves)");
                }
            }

            var box = room.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                GUILayout.Label($"Size: {box.size}");
            }

            var doors = room.GetComponentsInChildren<Door>(true);
            GUILayout.Label($"Doors: {doors.Length}");

            EditorGUILayout.EndVertical();
        }

        // ──────────────────── Scene Input Processing ────────────────────

        private void ProcessSceneInput(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null) return;

            // Update hovered room
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                UpdateHoveredRoom(e);
                sceneView.Repaint();
            }

            // Mode-specific input
            switch (_currentMode)
            {
                case ToolMode.Connect:
                    DoorWiringService.HandleConnectModeInput(sceneView);
                    break;

                case ToolMode.Blockout:
                    BlockoutModeHandler.HandleBlockoutInput(sceneView);
                    break;
            }
        }

        private void UpdateHoveredRoom(Event e)
        {
            Vector2 mousePos = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            Vector2 worldPos = ray.origin;

            _hoveredRoom = null;
            var rooms = FindObjectsByType<Room>();

            foreach (var room in rooms)
            {
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                Rect roomRect = GetRoomWorldRect(room, box);
                if (roomRect.Contains(worldPos))
                {
                    _hoveredRoom = room;
                    break;
                }
            }
        }

        // ──────────────────── Mode Management ────────────────────

        public void SetMode(ToolMode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;
            _selectedRooms.Clear();

            Repaint();
            SceneView.RepaintAll();
        }

        // ──────────────────── Room Selection ────────────────────

        private void HandleRoomListClick(Room room)
        {
            if (Event.current.shift)
            {
                // Shift+click: toggle selection
                if (_selectedRooms.Contains(room))
                    _selectedRooms.Remove(room);
                else
                    _selectedRooms.Add(room);
            }
            else
            {
                // Normal click: single selection
                _selectedRooms.Clear();
                _selectedRooms.Add(room);
            }

            if (_selectedRooms.Count == 1)
            {
                Selection.activeGameObject = _selectedRooms[0].gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            SceneView.RepaintAll();
        }

        public void SelectRoom(Room room, bool additive = false)
        {
            if (!additive)
                _selectedRooms.Clear();

            if (!_selectedRooms.Contains(room))
                _selectedRooms.Add(room);

            Repaint();
            SceneView.RepaintAll();
        }

        public void DeselectRoom(Room room)
        {
            _selectedRooms.Remove(room);
            Repaint();
            SceneView.RepaintAll();
        }

        public void ClearSelection()
        {
            _selectedRooms.Clear();
            Repaint();
            SceneView.RepaintAll();
        }

        // ──────────────────── Utility ────────────────────

        private void TrackRoomAuthoringChanges()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var rooms = FindObjectsByType<Room>();
            var liveRoomIds = new HashSet<int>();

            foreach (var room in rooms)
            {
                if (room == null) continue;

                int roomId = room.GetInstanceID();
                liveRoomIds.Add(roomId);

                var currentState = CaptureRoomAuthoringState(room);
                if (!_roomAuthoringStates.TryGetValue(roomId, out var previousState))
                {
                    _roomAuthoringStates[roomId] = currentState;
                    continue;
                }

                bool boundsChanged = !AreRectsApproximatelyEqual(previousState.Bounds, currentState.Bounds);
                bool floorChanged = previousState.FloorLevel != currentState.FloorLevel;
                if (!boundsChanged && !floorChanged)
                {
                    continue;
                }

                DoorWiringService.SynchronizeRoomConnections(room);
                _roomAuthoringStates[roomId] = CaptureRoomAuthoringState(room);
            }

            var staleIds = new List<int>();
            foreach (var roomId in _roomAuthoringStates.Keys)
            {
                if (!liveRoomIds.Contains(roomId))
                {
                    staleIds.Add(roomId);
                }
            }

            foreach (var staleId in staleIds)
            {
                _roomAuthoringStates.Remove(staleId);
            }
        }

        private static RoomAuthoringState CaptureRoomAuthoringState(Room room)
        {
            var box = room != null ? room.GetComponent<BoxCollider2D>() : null;
            Rect bounds = box != null
                ? GetRoomWorldRect(room, box)
                : new Rect(room != null ? (Vector2)room.transform.position : Vector2.zero, Vector2.zero);
            int floorLevel = room != null && room.Data != null ? room.Data.FloorLevel : 0;
            return new RoomAuthoringState(bounds, floorLevel);
        }

        private static bool AreRectsApproximatelyEqual(Rect a, Rect b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.width, b.width)
                && Mathf.Approximately(a.height, b.height);
        }

        /// <summary>
        /// Returns the world-space Rect for a room based on its Transform and BoxCollider2D.
        /// </summary>
        public static Rect GetRoomWorldRect(Room room, BoxCollider2D box)
        {
            Vector2 center = (Vector2)room.transform.position + box.offset;
            Vector2 size = box.size;
            return new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y);
        }

        /// <summary>
        /// Returns the display color for a RoomNodeType (fill).
        /// </summary>
        public static Color GetRoomTypeColor(RoomNodeType type)
        {
            switch (type)
            {
                case RoomNodeType.Transit: return new Color(0.3f, 0.5f, 0.9f, 0.5f);   // Blue
                case RoomNodeType.Combat: return new Color(0.9f, 0.7f, 0.2f, 0.5f);   // Yellow
                case RoomNodeType.Arena: return new Color(0.9f, 0.6f, 0.2f, 0.5f);    // Orange
                case RoomNodeType.Reward: return new Color(0.2f, 0.8f, 0.4f, 0.5f);   // Green
                case RoomNodeType.Safe: return new Color(0.2f, 0.8f, 0.3f, 0.5f);     // Green-blue
                case RoomNodeType.Boss: return new Color(0.9f, 0.2f, 0.2f, 0.5f);     // Red
                default: return new Color(0.5f, 0.5f, 0.5f, 0.5f);                    // Gray
            }
        }

        /// <summary>
        /// Returns the outline color for a RoomNodeType (full opacity).
        /// </summary>
        public static Color GetRoomTypeOutlineColor(RoomNodeType type)
        {
            switch (type)
            {
                case RoomNodeType.Transit: return new Color(0.3f, 0.5f, 0.9f, 1f);
                case RoomNodeType.Combat: return new Color(0.9f, 0.7f, 0.2f, 1f);
                case RoomNodeType.Arena: return new Color(0.9f, 0.6f, 0.2f, 1f);
                case RoomNodeType.Reward: return new Color(0.2f, 0.8f, 0.4f, 1f);
                case RoomNodeType.Safe: return new Color(0.2f, 0.8f, 0.3f, 1f);
                case RoomNodeType.Boss: return new Color(0.9f, 0.2f, 0.2f, 1f);
                default: return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        /// <summary>
        /// Returns the display color for a RoomNodeType.
        /// Kept aligned with WorldGraph editor colors for pacing readability.
        /// </summary>
        public static Color GetRoomNodeTypeColor(RoomNodeType nodeType)
        {
            switch (nodeType)
            {
                case RoomNodeType.Transit: return new Color(0.5f, 0.5f, 0.5f, 0.5f);
                case RoomNodeType.Combat: return new Color(0.9f, 0.7f, 0.2f, 0.5f);
                case RoomNodeType.Arena: return new Color(0.9f, 0.3f, 0.2f, 0.5f);
                case RoomNodeType.Reward: return new Color(0.2f, 0.8f, 0.4f, 0.5f);
                case RoomNodeType.Safe: return new Color(0.2f, 0.9f, 0.6f, 0.5f);
                case RoomNodeType.Boss: return new Color(0.8f, 0.1f, 0.1f, 0.5f);
                default: return new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Returns the outline color for a RoomNodeType (full opacity).
        /// </summary>
        public static Color GetRoomNodeTypeOutlineColor(RoomNodeType nodeType)
        {
            var color = GetRoomNodeTypeColor(nodeType);
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// Returns the display color for a ConnectionType.
        /// Migrated from ConnectionGraphEdge (WorldGraph Editor removed).
        /// </summary>
        public static Color GetConnectionTypeColor(ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Progression: return new Color(0.7f, 0.7f, 0.7f);   // Light gray
                case ConnectionType.Return:      return new Color(0.5f, 0.3f, 0.8f);   // Purple
                case ConnectionType.Ability:     return new Color(0.2f, 0.7f, 0.9f);   // Cyan
                case ConnectionType.Challenge:   return new Color(0.9f, 0.3f, 0.2f);   // Red
                case ConnectionType.Identity:    return new Color(0.9f, 0.7f, 0.2f);   // Gold
                case ConnectionType.Scheduled:   return new Color(0.3f, 0.9f, 0.5f);   // Green
                default:                         return new Color(0.5f, 0.5f, 0.5f);   // Gray
            }
        }

        // ──────────────────── Callbacks ────────────────────

        private void OnHierarchyChanged()
        {
            // Clean up references to destroyed rooms
            _selectedRooms.RemoveAll(r => r == null);
            if (_hoveredRoom == null) _hoveredRoom = null; // Unity null check

            _roomAuthoringStates.Clear();

            Repaint();
            SceneView.RepaintAll();
        }

        private void OnUndoRedo()
        {
            _roomAuthoringStates.Clear();
            DoorWiringService.SynchronizeAllRoomConnections();
            Repaint();
            SceneView.RepaintAll();
        }

    }
}
