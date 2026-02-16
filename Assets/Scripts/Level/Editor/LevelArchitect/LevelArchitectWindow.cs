using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Main entry point for the Level Architect Tool.
    /// Integrates with SceneView to provide a Scene-View-First editing experience.
    /// Manages tool modes, SceneView overlay, and side control panel.
    /// </summary>
    public class LevelArchitectWindow : EditorWindow
    {
        // ──────────────────── Constants ────────────────────

        private const string WINDOW_TITLE = "Level Architect";
        private const string MENU_PATH = "Window/ProjectArk/Level Architect";
        private const float SIDE_PANEL_WIDTH = 260f;
        private const float SIDE_PANEL_MARGIN = 10f;
        private const float TOOLBAR_HEIGHT = 30f;

        // ──────────────────── Tool Modes ────────────────────

        public enum ToolMode
        {
            Select,
            Blockout,
            Connect
        }

        // ──────────────────── Serialized State ────────────────────

        [SerializeField] private LevelScaffoldData _scaffoldData;
        [SerializeField] private ToolMode _currentMode = ToolMode.Select;
        [SerializeField] private bool _sidePanelExpanded = true;
        [SerializeField] private bool _showPacingOverlay;
        [SerializeField] private bool _showCriticalPath;
        [SerializeField] private bool _showLockKeyGraph;
        [SerializeField] private int _activeFloorLevel = int.MinValue; // MinValue = show all

        // ──────────────────── Runtime State ────────────────────

        private bool _isActive;
        private Vector2 _sidePanelScroll;
        private List<Room> _selectedRooms = new List<Room>();
        private Room _hoveredRoom;

        // ──────────────────── Sub-Systems ────────────────────

        private ScaffoldSceneBinder _scaffoldBinder = new ScaffoldSceneBinder();

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

        /// <summary> The scaffold data asset being edited. </summary>
        public LevelScaffoldData ScaffoldData => _scaffoldData;

        /// <summary> The scaffold-scene binder instance. </summary>
        public ScaffoldSceneBinder ScaffoldBinder => _scaffoldBinder;

        /// <summary> Active floor level filter (int.MinValue = show all). </summary>
        public int ActiveFloorLevel => _activeFloorLevel;

        /// <summary> Whether pacing overlay is enabled. </summary>
        public bool ShowPacingOverlay => _showPacingOverlay;

        /// <summary> Whether critical path overlay is enabled. </summary>
        public bool ShowCriticalPath => _showCriticalPath;

        /// <summary> Whether lock-key graph overlay is enabled. </summary>
        public bool ShowLockKeyGraph => _showLockKeyGraph;

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

            // Initialize scaffold binder
            if (_scaffoldData != null)
            {
                _scaffoldBinder.Initialize(_scaffoldData);
            }

            // Detect legacy tools and suggest migration
            CheckLegacyTools();

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
            DrawScaffoldDataField();
            EditorGUILayout.Space(4);
            DrawModeSelector();
            EditorGUILayout.Space(4);
            DrawOverlayToggles();
            EditorGUILayout.Space(4);
            DrawFloorLevelSelector();
            EditorGUILayout.Space(8);
            DrawQuickActions();
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

        private void DrawScaffoldDataField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scaffold Data", GUILayout.Width(90));
            var newData = (LevelScaffoldData)EditorGUILayout.ObjectField(
                _scaffoldData, typeof(LevelScaffoldData), false);
            if (newData != _scaffoldData)
            {
                _scaffoldData = newData;
                if (_scaffoldData != null)
                    _scaffoldBinder.Initialize(_scaffoldData);
                SceneView.RepaintAll();
            }
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
            var style = isActive ? "Button" : "Button";

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

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate All", GUILayout.Height(24)))
            {
                LevelValidator.ValidateAll();
                Repaint();
            }

            // Show results summary
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
            }
            if (GUILayout.Button("Scan Scene", GUILayout.Height(24)))
            {
                var scannedData = SceneScanner.ScanScene();
                if (scannedData != null)
                {
                    _scaffoldData = scannedData;
                    _scaffoldBinder.Initialize(_scaffoldData);
                    Repaint();
                }
            }
            if (GUILayout.Button("Create Built-in Presets", GUILayout.Height(22)))
            {
                BlockoutModeHandler.CreateBuiltInPresets();
                Debug.Log("[LevelArchitect] Built-in presets created/verified.");
            }
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Quick Play ▶", GUILayout.Height(28)))
            {
                BlockoutModeHandler.QuickPlay();
            }
        }

        // ──────────────────── SceneView Integration ────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive) return;

            // Tick scaffold-scene binder
            if (_scaffoldData != null)
            {
                _scaffoldBinder.Tick();
            }

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
            // ── Scaffold Data ──
            GUILayout.Label("Scaffold Data", EditorStyles.boldLabel);
            _scaffoldData = (LevelScaffoldData)EditorGUILayout.ObjectField(
                _scaffoldData, typeof(LevelScaffoldData), false, GUILayout.Width(SIDE_PANEL_WIDTH - 40));

            GUILayout.Space(8);

            // ── Room List ──
            GUILayout.Label("Rooms in Scene", EditorStyles.boldLabel);

            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
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
                    GUI.color = GetRoomTypeColor(room.Type);
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
                    BlockoutModeHandler.CreateBuiltInPresets();
                }
            }
            else
            {
                foreach (var preset in presets)
                {
                    if (preset == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    var prevColor = GUI.color;
                    GUI.color = LevelArchitectWindow.GetRoomTypeColor(preset.RoomTypeValue);
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

            // ── Validate ──
            if (GUILayout.Button("Validate All", GUILayout.Height(22)))
            {
                LevelValidator.ValidateAll();
                var window = LevelArchitectWindow.Instance;
                if (window != null) window.Repaint();
            }

            // Show compact validation results
            var validationResults = LevelValidator.LastResults;
            if (validationResults.Count > 0)
            {
                foreach (var result in validationResults)
                {
                    EditorGUILayout.BeginHorizontal();

                    Color iconColor = result.Severity == LevelValidator.Severity.Error ? Color.red :
                        result.Severity == LevelValidator.Severity.Warning ? Color.yellow : Color.cyan;
                    var prevColor2 = GUI.color;
                    GUI.color = iconColor;
                    GUILayout.Label("●", GUILayout.Width(12));
                    GUI.color = prevColor2;

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
            }
        }

        private void DrawSingleRoomInfo(Room room)
        {
            if (room == null) return;

            EditorGUILayout.BeginVertical("HelpBox");

            GUILayout.Label($"ID: {room.RoomID}");
            GUILayout.Label($"Type: {room.Type}");

            if (room.Data != null)
            {
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
            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

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
        /// Returns the display color for a room type.
        /// </summary>
        public static Color GetRoomTypeColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Normal: return new Color(0.3f, 0.5f, 0.9f, 0.5f);   // Blue
                case RoomType.Arena:  return new Color(0.9f, 0.6f, 0.2f, 0.5f);   // Orange
                case RoomType.Boss:   return new Color(0.9f, 0.2f, 0.2f, 0.5f);   // Red
                case RoomType.Safe:   return new Color(0.2f, 0.8f, 0.3f, 0.5f);   // Green
                default:              return new Color(0.5f, 0.5f, 0.5f, 0.5f);   // Gray
            }
        }

        /// <summary>
        /// Returns the outline color for a room type (full opacity).
        /// </summary>
        public static Color GetRoomTypeOutlineColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Normal: return new Color(0.3f, 0.5f, 0.9f, 1f);
                case RoomType.Arena:  return new Color(0.9f, 0.6f, 0.2f, 1f);
                case RoomType.Boss:   return new Color(0.9f, 0.2f, 0.2f, 1f);
                case RoomType.Safe:   return new Color(0.2f, 0.8f, 0.3f, 1f);
                default:              return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        // ──────────────────── Callbacks ────────────────────

        private void OnHierarchyChanged()
        {
            // Clean up references to destroyed rooms
            _selectedRooms.RemoveAll(r => r == null);
            if (_hoveredRoom == null) _hoveredRoom = null; // Unity null check

            Repaint();
            SceneView.RepaintAll();
        }

        private void OnUndoRedo()
        {
            Repaint();
            SceneView.RepaintAll();
        }

        // ──────────────────── Legacy Tool Detection ────────────────────

        private void CheckLegacyTools()
        {
            bool hasLegacy = false;

            if (HasOpenInstances<RoomBatchEditor>())
            {
                hasLegacy = true;
            }

            // Check for LevelDesignerWindow if it exists
            var ldwType = Type.GetType("ProjectArk.Level.Editor.LevelDesignerWindow, ProjectArk.Level.Editor");
            if (ldwType != null)
            {
                var method = typeof(EditorWindow).GetMethod("HasOpenInstances");
                if (method != null)
                {
                    var generic = method.MakeGenericMethod(ldwType);
                    if ((bool)generic.Invoke(null, null))
                    {
                        hasLegacy = true;
                    }
                }
            }

            if (hasLegacy)
            {
                bool switchNow = EditorUtility.DisplayDialog(
                    "Legacy Level Tools Detected",
                    "The Level Architect replaces RoomBatchEditor and LevelDesignerWindow.\n\n" +
                    "It's recommended to close the legacy tools to avoid conflicts.",
                    "Close Legacy Tools",
                    "Keep Both Open"
                );

                if (switchNow)
                {
                    CloseLegacyTools();
                }
            }
        }

        private void CloseLegacyTools()
        {
            // Close RoomBatchEditor
            if (HasOpenInstances<RoomBatchEditor>())
            {
                var rbe = GetWindow<RoomBatchEditor>();
                if (rbe != null) rbe.Close();
            }

            // Close LevelDesignerWindow via reflection
            var ldwType = Type.GetType("ProjectArk.Level.Editor.LevelDesignerWindow, ProjectArk.Level.Editor");
            if (ldwType != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(ldwType);
                foreach (var w in windows)
                {
                    if (w is EditorWindow ew) ew.Close();
                }
            }
        }
    }
}
