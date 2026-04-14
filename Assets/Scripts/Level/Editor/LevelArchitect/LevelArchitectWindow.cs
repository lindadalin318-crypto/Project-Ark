using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Main entry point for the Level Architect Tool.
    /// Three-workspace layout: Build | Quick Edit | Validate.
    /// - Build: primary whitebox authoring path (Select/Blockout/Connect) + quick play
    /// - Quick Edit: room search, single-room inspector, batch maintenance, fast authoring actions
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
        private const int MAX_RECENT_ROOMS = 8;

        // ──────────────────── Tab ────────────────────

        private enum Tab { Build, QuickEdit, Validate }
        private static readonly string[] TAB_LABELS = { "🔨 Build", "🛠 Quick Edit", "✅ Validate" };
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
        [SerializeField] private string _roomSearchQuery = string.Empty;
        [SerializeField] private Door _selectedConnection;
        [SerializeField] private Door _hoveredConnection;
        [SerializeField] private List<Room> _pinnedRooms = new List<Room>();
        [SerializeField] private List<Room> _recentRooms = new List<Room>();

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

        private sealed class SlicePreviewData
        {
            public Room[] Rooms = Array.Empty<Room>();
            public Dictionary<RoomNodeType, int> NodeTypeCounts = new Dictionary<RoomNodeType, int>();
            public Dictionary<int, int> FloorCounts = new Dictionary<int, int>();
            public Dictionary<Room, List<Room>> DirectedGraph = new Dictionary<Room, List<Room>>();
            public Room EntryRoom;
            public Room BossRoom;
            public Room FirstArenaMissingEncounterRoom;
            public Room FirstOrphanRoom;
            public int TotalRooms;
            public int UniqueConnectionCount;
            public int OneWayConnectionCount;
            public int ConnectedComponentCount;
            public int IslandCount;
            public int OrphanRoomCount;
            public int ArenaMissingEncounterCount;
            public int ValidationErrors;
            public int ValidationWarnings;
            public bool HasClosedLoop;
            public bool HasEntryToBossPath;
            public int CriticalPathRoomCount;
        }

        private sealed class SliceSuggestion
        {
            public MessageType MessageType;
            public string Message;
            public string ActionLabel;
            public Action OnClick;
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

        /// <summary> Currently selected room count. </summary>
        public int SelectedRoomCount => _selectedRooms.Count;

        /// <summary> Currently selected single room, if any. </summary>
        public Room SelectedRoom => _selectedRooms.Count == 1 ? _selectedRooms[0] : null;

        /// <summary> Currently selected connection, if any. </summary>
        public Door SelectedConnection => _selectedConnection;

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

        /// <summary> Whether SceneView room selection / drag interaction should be allowed. </summary>
        public bool AllowSceneSelectionInteraction => _activeTab != Tab.Validate;

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

            var previousTab = _activeTab;
            _activeTab = (Tab)GUILayout.Toolbar((int)_activeTab, TAB_LABELS, GUILayout.Height(26));
            if (previousTab != _activeTab)
            {
                HandleActiveTabChanged(previousTab, _activeTab);
            }

            EditorGUILayout.Space(4);

            switch (_activeTab)
            {
                case Tab.Build:
                    DrawBuildTab();
                    break;
                case Tab.QuickEdit:
                    DrawQuickEditTab();
                    break;
                case Tab.Validate:
                    DrawValidateTab();
                    break;
            }
        }

        // ──────────────────── Build Tab ────────────────────

        private void DrawBuildTab()
        {
            EditorGUILayout.HelpBox(
                "Build 是当前的白盒搭建工作面。你应在这里完成房间骨架、拖拽白盒、连接关系、楼层过滤与 Quick Play 冒烟验证；HTML / JSON 导入仅保留为可选外部草图入口，不再作为顶层工作面。",
                MessageType.Info);
            EditorGUILayout.Space(4);
            DrawModeSelector();
            EditorGUILayout.Space(4);
            DrawOverlayToggles();
            EditorGUILayout.Space(4);
            DrawFloorLevelSelector();
            EditorGUILayout.Space(8);
            DrawBuildActions();
            EditorGUILayout.Space(10);
            DrawOptionalDraftImportTools();
        }

        // ──────────────────── Quick Edit Tab ────────────────────

        private void DrawQuickEditTab()
        {
            EditorGUILayout.HelpBox(
                "Quick Edit 是生产期修改工作面。这里优先服务‘找房间 → 打开 Room Inspector Window → 改 RoomSO 核心字段 / 连接 / starter → 快速回看’的连续 authoring loop。",
                MessageType.Info);
            EditorGUILayout.Space(4);
            DrawFloorLevelSelector();
            EditorGUILayout.Space(4);
            DrawOverlayToggles();
            EditorGUILayout.Space(8);

            _sidePanelScroll = EditorGUILayout.BeginScrollView(_sidePanelScroll);
            DrawRoomSearchField();
            EditorGUILayout.Space(4);
            DrawQuickAccessSection();
            EditorGUILayout.Space(6);
            DrawRoomListSection();
            EditorGUILayout.Space(8);
            DrawSelectionSection();
            EditorGUILayout.Space(8);
            DrawCompactValidationSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawOptionalDraftImportTools()
        {
            EditorGUILayout.LabelField("Optional Draft & Import", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "仅当你需要浏览器侧拓扑草图或 JSON 导入源时才使用这些入口。常规最小验证切片仍应优先走 Build / Quick Edit 主链。",
                MessageType.None);

            if (GUILayout.Button("🌐 Open LevelDesigner.html", GUILayout.Height(28)))
            {
                string htmlPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath),
                    "Tools", "LevelDesigner.html");
                Application.OpenURL("file://" + htmlPath.Replace("\\", "/"));
            }

            if (GUILayout.Button("📂 Import LevelDesigner JSON...", GUILayout.Height(28)))
            {
                LevelSliceBuilder.ImportFromJson();
            }

            EditorGUILayout.LabelField(
                "1. Open LevelDesigner.html in browser\n" +
                "2. Sketch room topology & connections\n" +
                "3. Set Level Name in the right panel\n" +
                "4. Click 💾 Export File to save JSON\n" +
                "5. Import JSON here\n" +
                "6. Return to Build / Quick Edit to refine Room / Door / validation state",
                EditorStyles.helpBox);
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

        private void HandleActiveTabChanged(Tab previousTab, Tab nextTab)
        {
            if (nextTab != Tab.Build && _currentMode != ToolMode.Select)
            {
                _currentMode = ToolMode.Select;
            }

            Repaint();
            SceneView.RepaintAll();
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

            EditorGUILayout.Space(6);
            DrawValidationSliceTemplateSection(false);
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

            CancelActiveSceneInteractionIfPointerIsOverOverlay(sceneView, Event.current);

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

        private static bool IsOverlayMouseEvent(Event e)
        {
            if (e == null || !e.isMouse)
            {
                return false;
            }

            return e.type == EventType.MouseDown ||
                   e.type == EventType.MouseDrag ||
                   e.type == EventType.MouseUp;
        }

        private static bool HasActiveSceneInteraction()
        {
            return RoomBlockoutRenderer.HasActiveInteraction ||
                   BlockoutModeHandler.HasActiveInteraction ||
                   DoorWiringService.HasActiveInteraction;
        }

        private static void CancelAllSceneToolInteractions()
        {
            RoomBlockoutRenderer.CancelSceneInteraction();
            BlockoutModeHandler.CancelSceneInteraction();
            DoorWiringService.CancelSceneInteraction();
        }

        private void CancelActiveSceneInteractionIfPointerIsOverOverlay(SceneView sceneView, Event e)
        {
            if (!HasActiveSceneInteraction() || !IsOverlayMouseEvent(e))
            {
                return;
            }

            if (!IsPointerOverSceneOverlay(sceneView, e.mousePosition))
            {
                return;
            }

            CancelAllSceneToolInteractions();
        }

        internal bool IsPointerOverSceneOverlay(SceneView sceneView, Vector2 mousePosition)
        {
            if (sceneView == null)
            {
                return false;
            }

            if (GetSceneToolbarRect(sceneView).Contains(mousePosition))
            {
                return true;
            }

            if (_sidePanelExpanded && GetSceneSidePanelRect(sceneView).Contains(mousePosition))
            {
                return true;
            }

            if (_currentMode == ToolMode.Blockout && BlockoutModeHandler.GetBrushToolbarRect(sceneView).Contains(mousePosition))
            {
                return true;
            }

            return false;
        }

        private Rect GetSceneToolbarRect(SceneView sceneView)
        {
            float toolbarWidth = _activeTab == Tab.Build ? 320f : 360f;
            float toolbarX = (sceneView.position.width - toolbarWidth) / 2f;
            return new Rect(toolbarX, 8f, toolbarWidth, TOOLBAR_HEIGHT);
        }

        private Rect GetSceneSidePanelRect(SceneView sceneView)
        {
            float panelHeight = Mathf.Max(120f, sceneView.position.height - TOOLBAR_HEIGHT - 60f);
            return new Rect(
                SIDE_PANEL_MARGIN,
                TOOLBAR_HEIGHT + 20f,
                SIDE_PANEL_WIDTH,
                panelHeight
            );
        }

        private void DrawSceneViewToolbar(SceneView sceneView)
        {
            Handles.BeginGUI();

            var toolbarRect = GetSceneToolbarRect(sceneView);
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

            GUILayout.BeginArea(new Rect(toolbarRect.x + 4, toolbarRect.y + 2, toolbarRect.width - 8, toolbarRect.height - 4));
            GUILayout.BeginHorizontal();

            if (_activeTab == Tab.Build)
            {
                DrawSceneToolbarButton("Select", ToolMode.Select);
                DrawSceneToolbarButton("Blockout", ToolMode.Blockout);
                DrawSceneToolbarButton("Connect", ToolMode.Connect);
            }
            else
            {
                GUILayout.Label("Quick Edit", EditorStyles.miniBoldLabel, GUILayout.Width(64));
                DrawSceneToolbarButton("Select", ToolMode.Select);
                GUILayout.Label("Selection + Inspector Window", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

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
            try
            {
                float panelHeight = Mathf.Max(120f, sceneView.position.height - TOOLBAR_HEIGHT - 60f);
                var panelRect = new Rect(
                    SIDE_PANEL_MARGIN,
                    TOOLBAR_HEIGHT + 20,
                    SIDE_PANEL_WIDTH,
                    panelHeight
                );

                EditorGUI.DrawRect(panelRect, new Color(0.2f, 0.2f, 0.2f, 0.9f));

                bool areaBegan = false;
                bool scrollViewBegan = false;

                try
                {
                    GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
                    areaBegan = true;

                    _sidePanelScroll = GUILayout.BeginScrollView(_sidePanelScroll);
                    scrollViewBegan = true;

                    DrawSceneSidePanelContent();
                }
                finally
                {
                    if (scrollViewBegan)
                    {
                        GUILayout.EndScrollView();
                    }

                    if (areaBegan)
                    {
                        GUILayout.EndArea();
                    }
                }
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        private void DrawSceneSidePanelContent()
        {
            switch (_activeTab)
            {
                case Tab.Build:
                    DrawBuildSidePanelContent();
                    break;
                case Tab.QuickEdit:
                    DrawQuickEditSidePanelContent();
                    break;
            }
        }

        private void DrawBuildSidePanelContent()
        {
            GUILayout.Label("Build Helpers", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "这里优先服务搭建：补房、补模板、Quick Play，以及在需要时直接查看当前选中房间。",
                MessageType.None);

            GUILayout.Space(6);
            DrawSelectionSection();
            GUILayout.Space(8);
            DrawAddRoomSection();
            GUILayout.Space(8);
            DrawCompactValidationSection();
        }

        private void DrawQuickEditSidePanelContent()
        {
            DrawRoomSearchField();
            GUILayout.Space(4);
            DrawQuickAccessSection();
            GUILayout.Space(6);
            DrawRoomListSection();
            GUILayout.Space(8);
            DrawSelectionSection();
            GUILayout.Space(8);
            DrawSlicePreviewSection();
            GUILayout.Space(8);
            DrawCompactValidationSection();
        }

        private void DrawRoomSearchField()
        {
            GUILayout.Label("Find Room", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _roomSearchQuery = EditorGUILayout.TextField("Search", _roomSearchQuery);
            if (GUILayout.Button("Clear", GUILayout.Width(48)))
            {
                _roomSearchQuery = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Matches Room ID / Display Name / Node Type / Floor Level.", EditorStyles.miniLabel);
        }

        private void DrawQuickAccessSection()
        {
            SanitizeQuickAccessRooms();

            GUILayout.Label("Quick Access", EditorStyles.boldLabel);

            var selectedRoom = _selectedRooms.Count == 1 ? _selectedRooms[0] : null;

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedRoom == null))
            {
                string pinLabel = selectedRoom != null && IsPinnedRoom(selectedRoom) ? "Unpin Selected" : "Pin Selected";
                if (GUILayout.Button(pinLabel, GUILayout.Height(20)) && selectedRoom != null)
                {
                    TogglePinnedRoom(selectedRoom);
                }
            }

            using (new EditorGUI.DisabledScope(GetPreviousRecentRoom() == null))
            {
                if (GUILayout.Button("Recall Previous", GUILayout.Height(20)))
                {
                    RecallPreviousRoom();
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawQuickAccessRoomList("Pinned", _pinnedRooms, "No pinned rooms yet.");
            GUILayout.Space(3);
            DrawQuickAccessRoomList("Recent", _recentRooms, "Recent rooms appear here after you select or edit a room.");
        }

        private void DrawQuickAccessRoomList(string title, List<Room> source, string emptyMessage)
        {
            GUILayout.Label(title, EditorStyles.miniBoldLabel);

            var rooms = GetQuickAccessRooms(source);
            if (rooms.Count == 0)
            {
                GUILayout.Label(emptyMessage, EditorStyles.miniLabel);
                return;
            }

            foreach (var room in rooms)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(IsPinnedRoom(room) ? "★" : "☆", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    TogglePinnedRoom(room);
                }

                bool isSelected = _selectedRooms.Count == 1 && _selectedRooms[0] == room;
                var labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.miniButton;
                if (GUILayout.Button(GetQuickAccessLabel(room), labelStyle))
                {
                    OpenRoomFromQuickAccess(room, false);
                }

                if (GUILayout.Button("↗", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    OpenRoomFromQuickAccess(room, true);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRoomListSection()
        {
            GUILayout.Label("Rooms in Scene", EditorStyles.boldLabel);

            var rooms = GetFilteredRooms();
            if (rooms.Count == 0)
            {
                string message = string.IsNullOrWhiteSpace(_roomSearchQuery)
                    ? "No rooms found in scene."
                    : "No rooms match the current search / floor filter.";
                GUILayout.Label(message, EditorStyles.miniLabel);
                return;
            }

            foreach (var room in rooms)
            {
                if (room == null) continue;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(IsPinnedRoom(room) ? "★" : "☆", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    TogglePinnedRoom(room);
                }

                var prevColor = GUI.color;
                GUI.color = GetRoomNodeTypeColor(room.NodeType);
                GUILayout.Label("■", GUILayout.Width(14));
                GUI.color = prevColor;

                bool isSelected = _selectedRooms.Contains(room);
                var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(GetRoomListLabel(room), style))
                {
                    HandleRoomListClick(room);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSelectionSection()
        {
            GUILayout.Label(_selectedRooms.Count > 0
                ? $"Selected: {_selectedRooms.Count} room(s)"
                : "Selection", EditorStyles.boldLabel);

            if (_selectedRooms.Count == 0)
            {
                _selectedConnection = null;
                GUILayout.Label("No room selected.", EditorStyles.miniLabel);
                return;
            }

            if (_selectedRooms.Count == 1)
            {
                DrawSingleRoomSelectionSummary(_selectedRooms[0]);
            }
            else
            {
                _selectedConnection = null;
                BatchEditPanel.DrawBatchEditPanel(_selectedRooms);
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.ContextClick && lastRect.Contains(Event.current.mousePosition))
            {
                BatchEditPanel.ShowContextMenu(_selectedRooms);
                Event.current.Use();
            }
        }

        public void DrawDetachedRoomInspectorWindow()
        {
            if (_selectedRooms.Count == 0)
            {
                _selectedConnection = null;
                EditorGUILayout.HelpBox(
                    "未选中房间。请先在 Level Architect 中单选一个房间，再在这里做单房 / 连接细修。",
                    MessageType.Info);
                return;
            }

            if (_selectedRooms.Count > 1)
            {
                _selectedConnection = null;
                EditorGUILayout.HelpBox(
                    $"当前选中了 {_selectedRooms.Count} 个房间。Detached Room Inspector 只服务单房精修；批量维护请回到 Level Architect 主窗口。",
                    MessageType.Info);

                if (GUILayout.Button("Open Level Architect", GUILayout.Height(22f)))
                {
                    ShowWindow();
                }

                return;
            }

            DrawSingleRoomInfo(_selectedRooms[0], true);
        }

        private void DrawSingleRoomSelectionSummary(Room room)
        {
            if (room == null)
            {
                return;
            }

            var roomData = room.Data;
            int doorCount = room.GetComponentsInChildren<Door>(true).Length;

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Room", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Inspector", GUILayout.Width(108f), GUILayout.Height(20f)))
            {
                LevelRoomInspectorWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(GetRoomListLabel(room), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Node / Floor", roomData != null ? $"{roomData.NodeType} · F{roomData.FloorLevel}" : "Missing RoomSO");
            EditorGUILayout.LabelField("Door Links", doorCount.ToString());
            if (_selectedConnection != null)
            {
                EditorGUILayout.LabelField("Selected Connection", GetDoorConnectionLabel(_selectedConnection), EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.HelpBox(
                "完整版 Room / Connection Inspector 已迁到独立窗口；这里保留选择摘要与高频跳转动作，避免 Quick Edit 再被细字段挤满。",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus", GUILayout.Height(22f)))
            {
                FocusRoomInScene(room);
            }
            if (GUILayout.Button(IsPinnedRoom(room) ? "★ Pinned" : "☆ Pin", GUILayout.Height(22f)))
            {
                TogglePinnedRoom(room);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stable Rename", GUILayout.Height(22f)))
            {
                TrackRecentRoom(room);
                PerformStableRename(room);
            }
            if (GUILayout.Button("Set Entry", GUILayout.Height(22f)))
            {
                TrackRecentRoom(room);
                BatchEditPanel.SetEntryRoom(room);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawAddRoomSection()
        {
            GUILayout.Label("Add Room", EditorStyles.boldLabel);
            DrawValidationSliceTemplateSection(true);
            GUILayout.Space(6);

            var presets = RoomFactory.FindAllPresets();
            if (presets.Length == 0)
            {
                if (GUILayout.Button("Create Built-in Presets First", GUILayout.Height(24)))
                {
                    RoomFactory.CreateBuiltInPresets();
                }
                return;
            }

            foreach (var preset in presets)
            {
                if (preset == null) continue;

                EditorGUILayout.BeginHorizontal();

                var prevColor = GUI.color;
                GUI.color = GetRoomTypeColor(preset.NodeTypeValue);
                GUILayout.Label("■", GUILayout.Width(14));
                GUI.color = prevColor;

                string label = $"{preset.PresetName} ({preset.DefaultSize.x}×{preset.DefaultSize.y})";
                if (GUILayout.Button(label, EditorStyles.miniButton))
                {
                    var sv = SceneView.lastActiveSceneView;
                    Vector3 placePos = sv != null ? sv.camera.transform.position : Vector3.zero;
                    placePos.z = 0;
                    RoomFactory.CreateRoomFromPreset(preset, placePos);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawValidationSliceTemplateSection(bool compact)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            GUILayout.Label(compact ? "Validation Slice" : "5-Room Validation Slice", compact ? EditorStyles.miniBoldLabel : EditorStyles.boldLabel);
            GUILayout.Label(
                "Safe Entry → Transit → Combat → Reward → Return Transit。会自动建房、连门、设 Entry，让 Build / Validate / Quick Play 立刻拥有一个最小闭环起点。",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(
                "结果：默认生成 5 个已连通房间，并把整组切片选中到当前 authoring 上下文。",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label($"放置锚点：{GetValidationSliceAnchorHint()}", EditorStyles.miniLabel);

            if (GUILayout.Button("Seed 5-Room Slice", GUILayout.Height(compact ? 22 : 24)))
            {
                CreateValidationSliceTemplate(false, false);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create + Validate", GUILayout.Height(20)))
            {
                CreateValidationSliceTemplate(true, false);
            }
            if (GUILayout.Button("Create + Quick Play", GUILayout.Height(20)))
            {
                CreateValidationSliceTemplate(false, true);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label(
                "Seed = 只建切片；Create + Validate = 立即打开 Validate；Create + Quick Play = 直接做结构冒烟。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private static string GetValidationSliceAnchorHint()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                return "未检测到 SceneView，相机锚点会回退到 (0.0, 0.0)。";
            }

            Vector3 position = sceneView.camera.transform.position;
            return $"使用当前 SceneView 相机位置 ({position.x:0.0}, {position.y:0.0})。";
        }

        private void CreateValidationSliceTemplate(bool validateAfterCreate, bool quickPlayAfterCreate)
        {
            var sceneView = SceneView.lastActiveSceneView;
            Vector3 anchorPosition = sceneView != null ? sceneView.camera.transform.position : Vector3.zero;
            anchorPosition.z = 0f;

            var rooms = RoomFactory.CreateFiveRoomValidationSlice(anchorPosition);
            if (rooms == null || rooms.Length == 0)
            {
                return;
            }

            _activeTab = Tab.Build;
            _selectedRooms.Clear();
            _selectedRooms.AddRange(rooms);
            _selectedConnection = null;

            TrackRecentRoom(rooms[0]);
            Selection.activeGameObject = rooms[0].gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Repaint();
            SceneView.RepaintAll();

            if (validateAfterCreate)
            {
                LevelValidator.ValidateAll();
                OpenValidateTab();
            }

            if (quickPlayAfterCreate)
            {
                BlockoutModeHandler.QuickPlay();
            }
        }

        private void DrawBuildRuntimeAssistSection()
        {
            GUILayout.Label("Runtime Assist", EditorStyles.boldLabel);

            var selectedRoom = _selectedRooms.Count == 1 ? _selectedRooms[0] : null;
            if (selectedRoom == null)
            {
                string hint = _selectedRooms.Count <= 0
                    ? "先单选一个房间，再从这里补 Checkpoint、Encounter Trigger、Biome Trigger 等标准 starter。"
                    : $"当前选中了 {_selectedRooms.Count} 个房间。Runtime Assist 只对单房显示，避免工具替你批量猜设计。";
                EditorGUILayout.HelpBox(hint, MessageType.None);
                return;
            }

            GUILayout.Label(
                "适合在结构与连接已基本稳定后，补第一批 runtime 对象起点。创建后会自动选中新对象，方便继续补 SO / phase / key 配置。",
                EditorStyles.wordWrappedMiniLabel);
            DrawRoomRuntimeAssistSection(selectedRoom, false);
        }

        private void DrawRoomRuntimeAssistSection(Room room, bool compact)
        {
            if (room == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("HelpBox"))
            {
                EditorGUILayout.LabelField(compact ? "Runtime Assist / Starter Objects" : "Starter Objects", compact ? EditorStyles.miniBoldLabel : EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Room: {GetRoomListLabel(room)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "适合在结构与连接已基本稳定后，补第一批 runtime 对象起点。创建后会自动选中新对象，方便继续补 SO / phase / key / hazard 参数配置。",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    "根节点分组：Elements → Checkpoint；Encounters → Open Encounter；Hazards → Contact / Zone / Timed；Triggers → Biome / Scheduled / World Event。",
                    EditorStyles.wordWrappedMiniLabel);

                DrawRoomRuntimeAssistButtons(room, compact);
            }
        }


        private void DrawRoomRuntimeAssistButtons(Room room, bool compact)
        {
            float buttonHeight = compact ? 20f : 22f;

            EditorGUILayout.LabelField("Elements", EditorStyles.miniBoldLabel);
            if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.Checkpoint), GUILayout.Height(buttonHeight)))
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.Checkpoint);
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Encounters", EditorStyles.miniBoldLabel);
            if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.OpenEncounterTrigger), GUILayout.Height(buttonHeight)))
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.OpenEncounterTrigger);
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Hazards", EditorStyles.miniBoldLabel);
            bool createContactHazard = false;
            bool createDamageZone = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                createContactHazard = GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.ContactHazard), GUILayout.Height(buttonHeight));
                createDamageZone = GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.DamageZone), GUILayout.Height(buttonHeight));
            }

            if (createContactHazard)
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.ContactHazard);
                return;
            }

            if (createDamageZone)
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.DamageZone);
                return;
            }

            if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.TimedHazard), GUILayout.Height(buttonHeight)))
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.TimedHazard);
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Triggers", EditorStyles.miniBoldLabel);

            bool createBiomeTrigger = false;
            bool createScheduledBehaviour = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                createBiomeTrigger = GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.BiomeTrigger), GUILayout.Height(buttonHeight));
                createScheduledBehaviour = GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.ScheduledBehaviour), GUILayout.Height(buttonHeight));
            }

            if (createBiomeTrigger)
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.BiomeTrigger);
                return;
            }

            if (createScheduledBehaviour)
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.ScheduledBehaviour);
                return;
            }

            if (GUILayout.Button(LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.WorldEventTrigger), GUILayout.Height(buttonHeight)))
            {
                CreateRoomRuntimeAssist(room, LevelRuntimeAssistFactory.RoomAssistType.WorldEventTrigger);
            }
        }


        private void DrawConnectionRuntimeAssistSection(Room ownerRoom, Door door)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Connection Assist", EditorStyles.boldLabel);

            string ownerRoomId = ownerRoom != null ? ownerRoom.RoomID : "Missing Owner";
            string targetRoomId = door != null && door.TargetRoom != null ? door.TargetRoom.RoomID : "Missing Target";
            EditorGUILayout.LabelField($"Link: {ownerRoomId} → {targetRoomId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "Lock starter 会建在 owner room 的 Elements 根下，自动绑定当前 Door，并把门的初始状态切到 Locked_Key。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(
                "创建后仍需手动指定 KeyItemSO，并确认这条门控是否真的是推进门，而不是 return / shortcut。",
                EditorStyles.wordWrappedMiniLabel);

            if (door == null || door.TargetRoom == null)
            {
                EditorGUILayout.HelpBox("当前连接缺少 TargetRoom，先修复 Door link，再创建 Lock starter。", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(ownerRoom == null || door == null || door.TargetRoom == null))
            {
                string buttonLabel = door != null && door.TargetRoom != null
                    ? $"Create Lock Starter → {door.TargetRoom.RoomID}"
                    : "Create Lock Starter";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(22f)))
                {
                    CreateConnectionLockAssist(ownerRoom, door);
                }
            }
        }

        private void DrawCompactValidationSection()
        {
            if (GUILayout.Button("✅ Validate All", GUILayout.Height(22)))
            {
                LevelValidator.ValidateAll();
                var window = Instance;
                if (window != null) window.Repaint();
            }

            var validationResults = LevelValidator.LastResults;
            if (validationResults.Count <= 0)
            {
                return;
            }

            int errors = 0;
            int warnings = 0;
            foreach (var result in validationResults)
            {
                if (result.Severity == LevelValidator.Severity.Error) errors++;
                else if (result.Severity == LevelValidator.Severity.Warning) warnings++;
            }

            EditorGUILayout.HelpBox(
                $"{errors} error(s), {warnings} warning(s) — see Validate tab",
                errors > 0 ? MessageType.Error : MessageType.Warning);
        }

        private void DrawSlicePreviewSection()
        {
            var preview = BuildSlicePreviewData();

            GUILayout.Label("Preview / Summary", EditorStyles.boldLabel);

            if (preview.TotalRooms <= 0)
            {
                EditorGUILayout.HelpBox(
                    "No rooms in scene yet. Start from Build and place a starter room before wiring topology.",
                    MessageType.Info);
                DrawNextStepSuggestions(preview);
                return;
            }

            EditorGUILayout.HelpBox(GetSliceStateHeadline(preview), GetSliceStateMessageType(preview));

            EditorGUILayout.LabelField("Topology", EditorStyles.miniBoldLabel);
            GUILayout.Label(GetTopologySummary(preview), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(GetFloorSummary(preview), EditorStyles.wordWrappedMiniLabel);

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Semantics", EditorStyles.miniBoldLabel);
            GUILayout.Label(GetNodeTypeSummary(preview), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(GetCriticalPathSummary(preview), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(GetLoopSummary(preview), EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(GetValidationSnapshotSummary(preview), EditorStyles.wordWrappedMiniLabel);

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Next Step", EditorStyles.miniBoldLabel);
            DrawNextStepSuggestions(preview);
        }

        private void DrawNextStepSuggestions(SlicePreviewData preview)
        {
            var suggestions = BuildNextStepSuggestions(preview);
            if (suggestions.Count <= 0)
            {
                GUILayout.Label("No immediate next step.", EditorStyles.miniLabel);
                return;
            }

            foreach (var suggestion in suggestions)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                GUILayout.Label($"{GetSuggestionPrefix(suggestion.MessageType)} {suggestion.Message}", EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrWhiteSpace(suggestion.ActionLabel) && suggestion.OnClick != null)
                {
                    if (GUILayout.Button(suggestion.ActionLabel, GUILayout.Height(20)))
                    {
                        suggestion.OnClick();
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private List<SliceSuggestion> BuildNextStepSuggestions(SlicePreviewData preview)
        {
            var suggestions = new List<SliceSuggestion>();
            Room selectedRoom = _selectedRooms.Count == 1 ? _selectedRooms[0] : null;
            bool hasValidationSnapshot = LevelValidator.LastResults.Count > 0;

            if (preview.TotalRooms <= 0)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Info,
                    Message = "先在 Build 里放一个起手房间，当前切片还没有 Room 可供连线或验证。",
                    ActionLabel = "Go Build",
                    OnClick = GoToBuildBlockout
                });
                return suggestions;
            }

            if (preview.EntryRoom == null)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Warning,
                    Message = selectedRoom != null
                        ? $"还没有 Entry Room。可以先把当前选中的 '{GetRoomListLabel(selectedRoom)}' 设为起手入口。"
                        : "还没有 Entry Room。先在 Quick Edit 里选中一个合适房间并执行 Set Entry。",
                    ActionLabel = selectedRoom != null ? "Set Selected Entry" : "Open Quick Edit",
                    OnClick = selectedRoom != null ? (Action)SetSelectedRoomAsEntry : OpenQuickEditTab
                });
            }

            if (preview.EntryRoom != null && preview.BossRoom != null && !preview.HasEntryToBossPath)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Warning,
                    Message = $"Entry '{GetRoomListLabel(preview.EntryRoom)}' 还走不到 Boss '{GetRoomListLabel(preview.BossRoom)}'，主路径仍未打通。",
                    ActionLabel = "Focus Entry",
                    OnClick = () => FocusRoomFromSuggestion(preview.EntryRoom)
                });
            }
            else if (preview.BossRoom == null && preview.TotalRooms >= 3)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Info,
                    Message = "当前还没有 Boss 房；如果这轮目标是最小纵切，可以补一个终点房来验证 Entry → Boss 主路径。",
                    ActionLabel = "Go Build Connect",
                    OnClick = GoToBuildConnect
                });
            }

            if (preview.ArenaMissingEncounterCount > 0)
            {
                string roomLabel = preview.FirstArenaMissingEncounterRoom != null
                    ? GetRoomListLabel(preview.FirstArenaMissingEncounterRoom)
                    : "Arena/Boss room";

                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Warning,
                    Message = $"{preview.ArenaMissingEncounterCount} 个 Arena/Boss 房还缺 EncounterSO。先补战斗 owner，首个缺口在 '{roomLabel}'。",
                    ActionLabel = preview.FirstArenaMissingEncounterRoom != null ? "Focus Room" : "Open Quick Edit",
                    OnClick = preview.FirstArenaMissingEncounterRoom != null
                        ? () => FocusRoomFromSuggestion(preview.FirstArenaMissingEncounterRoom)
                        : OpenQuickEditTab
                });
            }

            if (preview.OrphanRoomCount > 0)
            {
                string roomLabel = preview.FirstOrphanRoom != null
                    ? GetRoomListLabel(preview.FirstOrphanRoom)
                    : "isolated room";

                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Warning,
                    Message = $"还有 {preview.OrphanRoomCount} 个房间没有有效 Door 连接。先处理孤立房，首个缺口在 '{roomLabel}'。",
                    ActionLabel = preview.FirstOrphanRoom != null ? "Focus Room" : "Go Build Connect",
                    OnClick = preview.FirstOrphanRoom != null
                        ? () => FocusRoomFromSuggestion(preview.FirstOrphanRoom)
                        : GoToBuildConnect
                });
            }

            if (preview.OneWayConnectionCount > 0)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Warning,
                    Message = $"当前有 {preview.OneWayConnectionCount} 条单向连接。确认它们是否故意；如果不是，优先回到 Validate 修 reciprocal Door。",
                    ActionLabel = "Open Validate",
                    OnClick = OpenValidateAndRun
                });
            }

            if (!hasValidationSnapshot)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Info,
                    Message = "还没有 Validate 快照。跑一次 Validate All，可以把 authoring gaps 和 auto-fix 入口收口出来。",
                    ActionLabel = "Validate Now",
                    OnClick = OpenValidateAndRun
                });
            }
            else if (preview.ValidationErrors > 0 || preview.ValidationWarnings > 0)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = preview.ValidationErrors > 0 ? MessageType.Error : MessageType.Warning,
                    Message = $"Validate 当前还有 {preview.ValidationErrors} 个 error / {preview.ValidationWarnings} 个 warning，建议先收口再继续扩图。",
                    ActionLabel = "Open Validate",
                    OnClick = OpenValidateTab
                });
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add(new SliceSuggestion
                {
                    MessageType = MessageType.Info,
                    Message = "当前切片已经具备最小结构闭环，可以进入 Quick Play 做一轮连通与手感冒烟验证。",
                    ActionLabel = "Quick Play",
                    OnClick = RunQuickPlayFromSuggestion
                });
            }

            if (suggestions.Count > 4)
            {
                suggestions.RemoveRange(4, suggestions.Count - 4);
            }

            return suggestions;
        }

        private SlicePreviewData BuildSlicePreviewData()
        {
            var preview = new SlicePreviewData();
            foreach (RoomNodeType nodeType in Enum.GetValues(typeof(RoomNodeType)))
            {
                preview.NodeTypeCounts[nodeType] = 0;
            }

            var rooms = FindObjectsByType<Room>();
            preview.Rooms = rooms ?? Array.Empty<Room>();
            preview.TotalRooms = preview.Rooms.Length;
            if (preview.TotalRooms <= 0)
            {
                return preview;
            }

            var undirectedGraph = new Dictionary<Room, List<Room>>();
            var uniqueConnectionPairs = new HashSet<string>();
            var oneWayConnectionPairs = new HashSet<string>();

            foreach (var room in preview.Rooms)
            {
                if (room == null)
                {
                    continue;
                }

                preview.DirectedGraph[room] = new List<Room>();
                undirectedGraph[room] = new List<Room>();
                preview.NodeTypeCounts[room.NodeType]++;

                int floorLevel = room.Data != null ? room.Data.FloorLevel : 0;
                if (!preview.FloorCounts.ContainsKey(floorLevel))
                {
                    preview.FloorCounts[floorLevel] = 0;
                }
                preview.FloorCounts[floorLevel]++;

                bool hasConnectedDoor = false;
                var seenTargets = new HashSet<int>();
                var doors = room.GetComponentsInChildren<Door>(true);
                foreach (var door in doors)
                {
                    Room targetRoom = door != null ? door.TargetRoom : null;
                    if (targetRoom == null || targetRoom == room)
                    {
                        continue;
                    }

                    hasConnectedDoor = true;

                    if (seenTargets.Add(targetRoom.GetInstanceID()))
                    {
                        preview.DirectedGraph[room].Add(targetRoom);
                    }

                    if (!undirectedGraph[room].Contains(targetRoom))
                    {
                        undirectedGraph[room].Add(targetRoom);
                    }

                    if (!undirectedGraph.ContainsKey(targetRoom))
                    {
                        undirectedGraph[targetRoom] = new List<Room>();
                    }

                    if (!undirectedGraph[targetRoom].Contains(room))
                    {
                        undirectedGraph[targetRoom].Add(room);
                    }

                    string pairKey = GetRoomPairKey(room, targetRoom);
                    uniqueConnectionPairs.Add(pairKey);

                    if (DoorWiringService.FindReverseDoor(door) == null)
                    {
                        oneWayConnectionPairs.Add(pairKey);
                    }
                }

                if (!hasConnectedDoor)
                {
                    preview.OrphanRoomCount++;
                    if (preview.FirstOrphanRoom == null)
                    {
                        preview.FirstOrphanRoom = room;
                    }
                }

                bool needsEncounter = room.NodeType == RoomNodeType.Arena || room.NodeType == RoomNodeType.Boss;
                if (needsEncounter && (room.Data == null || room.Data.Encounter == null))
                {
                    preview.ArenaMissingEncounterCount++;
                    if (preview.FirstArenaMissingEncounterRoom == null)
                    {
                        preview.FirstArenaMissingEncounterRoom = room;
                    }
                }
            }

            preview.UniqueConnectionCount = uniqueConnectionPairs.Count;
            preview.OneWayConnectionCount = oneWayConnectionPairs.Count;
            preview.ConnectedComponentCount = CountConnectedComponents(preview.Rooms, undirectedGraph);
            preview.IslandCount = Mathf.Max(0, preview.ConnectedComponentCount - 1);
            preview.HasClosedLoop = HasClosedLoop(preview.Rooms, undirectedGraph);

            preview.EntryRoom = FindConfiguredEntryRoom();
            preview.BossRoom = FindBossRoom(preview.Rooms);

            if (preview.EntryRoom != null && preview.BossRoom != null)
            {
                var path = FindPath(preview.EntryRoom, preview.BossRoom, preview.DirectedGraph);
                preview.HasEntryToBossPath = path != null && path.Count > 0;
                preview.CriticalPathRoomCount = preview.HasEntryToBossPath ? path.Count : 0;
            }

            foreach (var result in LevelValidator.LastResults)
            {
                if (result.Severity == LevelValidator.Severity.Error)
                {
                    preview.ValidationErrors++;
                }
                else if (result.Severity == LevelValidator.Severity.Warning)
                {
                    preview.ValidationWarnings++;
                }
            }

            return preview;
        }

        private string GetSliceStateHeadline(SlicePreviewData preview)
        {
            string state;
            if (preview.TotalRooms <= 0)
            {
                state = "Slice State · No slice yet";
            }
            else if (preview.ValidationErrors > 0)
            {
                state = "Slice State · Blocking validation gaps";
            }
            else if (preview.EntryRoom == null || preview.OrphanRoomCount > 0 || (preview.BossRoom != null && !preview.HasEntryToBossPath))
            {
                state = "Slice State · Topology incomplete";
            }
            else if (preview.ArenaMissingEncounterCount > 0 || preview.ValidationWarnings > 0)
            {
                state = "Slice State · Needs authoring pass";
            }
            else
            {
                state = "Slice State · Ready for Quick Play";
            }

            return $"{state} · {preview.TotalRooms} room(s) · {preview.UniqueConnectionCount} link(s) · {preview.IslandCount} island(s) · {preview.OneWayConnectionCount} one-way";
        }

        private static MessageType GetSliceStateMessageType(SlicePreviewData preview)
        {
            if (preview.TotalRooms <= 0)
            {
                return MessageType.Info;
            }

            if (preview.ValidationErrors > 0)
            {
                return MessageType.Error;
            }

            if (preview.EntryRoom == null || preview.OrphanRoomCount > 0 || (preview.BossRoom != null && !preview.HasEntryToBossPath))
            {
                return MessageType.Warning;
            }

            if (preview.ArenaMissingEncounterCount > 0 || preview.OneWayConnectionCount > 0 || preview.ValidationWarnings > 0)
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }

        private static string GetTopologySummary(SlicePreviewData preview)
        {
            return $"Rooms {preview.TotalRooms} · Connections {preview.UniqueConnectionCount} · Clusters {Mathf.Max(preview.ConnectedComponentCount, 1)} · Orphans {preview.OrphanRoomCount} · One-way {preview.OneWayConnectionCount}";
        }

        private string GetFloorSummary(SlicePreviewData preview)
        {
            if (preview.FloorCounts.Count <= 0)
            {
                return "Floors: none.";
            }

            if (_activeFloorLevel != int.MinValue)
            {
                preview.FloorCounts.TryGetValue(_activeFloorLevel, out int count);
                return $"Current Floor {GetFloorLabel(_activeFloorLevel)} · {count} room(s) in active filter.";
            }

            var floorLevels = new List<int>(preview.FloorCounts.Keys);
            floorLevels.Sort((a, b) => b.CompareTo(a));

            var parts = new List<string>();
            foreach (int floorLevel in floorLevels)
            {
                parts.Add($"{GetFloorLabel(floorLevel)}×{preview.FloorCounts[floorLevel]}");
            }

            return $"Floors: {string.Join(" · ", parts)}";
        }

        private static string GetNodeTypeSummary(SlicePreviewData preview)
        {
            var parts = new List<string>();
            foreach (RoomNodeType nodeType in Enum.GetValues(typeof(RoomNodeType)))
            {
                if (!preview.NodeTypeCounts.TryGetValue(nodeType, out int count) || count <= 0)
                {
                    continue;
                }

                parts.Add($"{nodeType}×{count}");
            }

            return parts.Count > 0
                ? $"Node Types: {string.Join(" · ", parts)}"
                : "Node Types: none.";
        }

        private static string GetCriticalPathSummary(SlicePreviewData preview)
        {
            if (preview.EntryRoom == null)
            {
                return "Critical Path: Entry Room is not configured yet.";
            }

            if (preview.BossRoom == null)
            {
                return $"Critical Path: Entry is '{GetRoomListLabel(preview.EntryRoom)}'; Boss room has not been authored yet.";
            }

            if (!preview.HasEntryToBossPath)
            {
                return $"Critical Path: Entry '{GetRoomListLabel(preview.EntryRoom)}' cannot reach Boss '{GetRoomListLabel(preview.BossRoom)}' yet.";
            }

            return $"Critical Path: Entry '{GetRoomListLabel(preview.EntryRoom)}' reaches Boss '{GetRoomListLabel(preview.BossRoom)}' in {preview.CriticalPathRoomCount} room(s).";
        }

        private static string GetLoopSummary(SlicePreviewData preview)
        {
            if (preview.TotalRooms < 3)
            {
                return "Loop: not enough rooms yet to judge a meaningful closed loop.";
            }

            return preview.HasClosedLoop
                ? "Loop: at least one closed route exists in the current slice."
                : "Loop: no closed route yet — the current slice is still mostly linear.";
        }

        private static string GetValidationSnapshotSummary(SlicePreviewData preview)
        {
            return LevelValidator.LastResults.Count > 0
                ? $"Validation Snapshot: {preview.ValidationErrors} error(s), {preview.ValidationWarnings} warning(s)."
                : "Validation Snapshot: not run yet.";
        }

        private static string GetSuggestionPrefix(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Error:
                    return "✖";
                case MessageType.Warning:
                    return "⚠";
                default:
                    return "•";
            }
        }

        private static string GetFloorLabel(int floorLevel)
        {
            if (floorLevel == 0)
            {
                return "G";
            }

            return floorLevel > 0 ? $"+{floorLevel}" : floorLevel.ToString();
        }

        private static int CountConnectedComponents(Room[] rooms, Dictionary<Room, List<Room>> graph)
        {
            var visited = new HashSet<Room>();
            int count = 0;

            foreach (var room in rooms)
            {
                if (room == null || visited.Contains(room))
                {
                    continue;
                }

                count++;
                var queue = new Queue<Room>();
                queue.Enqueue(room);
                visited.Add(room);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (!graph.TryGetValue(current, out var neighbors))
                    {
                        continue;
                    }

                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor == null || !visited.Add(neighbor))
                        {
                            continue;
                        }

                        queue.Enqueue(neighbor);
                    }
                }
            }

            return count;
        }

        private static bool HasClosedLoop(Room[] rooms, Dictionary<Room, List<Room>> graph)
        {
            var visited = new HashSet<Room>();
            foreach (var room in rooms)
            {
                if (room == null || visited.Contains(room))
                {
                    continue;
                }

                if (ContainsCycle(room, null, graph, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCycle(Room current, Room parent, Dictionary<Room, List<Room>> graph, HashSet<Room> visited)
        {
            visited.Add(current);
            if (!graph.TryGetValue(current, out var neighbors))
            {
                return false;
            }

            foreach (var neighbor in neighbors)
            {
                if (neighbor == null)
                {
                    continue;
                }

                if (!visited.Contains(neighbor))
                {
                    if (ContainsCycle(neighbor, current, graph, visited))
                    {
                        return true;
                    }
                }
                else if (neighbor != parent)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Room> FindPath(Room start, Room target, Dictionary<Room, List<Room>> graph)
        {
            if (start == null || target == null)
            {
                return null;
            }

            var visited = new HashSet<Room>();
            var queue = new Queue<List<Room>>();
            queue.Enqueue(new List<Room> { start });
            visited.Add(start);

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();
                var currentRoom = currentPath[currentPath.Count - 1];
                if (currentRoom == target)
                {
                    return currentPath;
                }

                if (!graph.TryGetValue(currentRoom, out var neighbors))
                {
                    continue;
                }

                foreach (var neighbor in neighbors)
                {
                    if (neighbor == null || !visited.Add(neighbor))
                    {
                        continue;
                    }

                    var nextPath = new List<Room>(currentPath) { neighbor };
                    queue.Enqueue(nextPath);
                }
            }

            return null;
        }

        private static Room FindConfiguredEntryRoom()
        {
            var roomManager = UnityEngine.Object.FindAnyObjectByType<RoomManager>();
            if (roomManager == null)
            {
                return null;
            }

            var serialized = new SerializedObject(roomManager);
            return serialized.FindProperty("_startingRoom").objectReferenceValue as Room;
        }

        private static Room FindBossRoom(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room != null && room.NodeType == RoomNodeType.Boss)
                {
                    return room;
                }
            }

            return null;
        }

        private static string GetRoomPairKey(Room roomA, Room roomB)
        {
            int idA = roomA != null ? roomA.GetInstanceID() : 0;
            int idB = roomB != null ? roomB.GetInstanceID() : 0;
            return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }

        private void OpenQuickEditTab()
        {
            _activeTab = Tab.QuickEdit;
            Repaint();
            SceneView.RepaintAll();
        }

        private void OpenValidateTab()
        {
            _activeTab = Tab.Validate;
            Repaint();
            SceneView.RepaintAll();
        }

        private void OpenValidateAndRun()
        {
            LevelValidator.ValidateAll();
            OpenValidateTab();
        }

        private void FocusRoomFromSuggestion(Room room)
        {
            if (room == null)
            {
                OpenQuickEditTab();
                return;
            }

            _activeTab = Tab.QuickEdit;
            OpenRoomFromQuickAccess(room, true);
        }

        private void SetSelectedRoomAsEntry()
        {
            var selectedRoom = _selectedRooms.Count == 1 ? _selectedRooms[0] : null;
            if (selectedRoom == null)
            {
                OpenQuickEditTab();
                return;
            }

            TrackRecentRoom(selectedRoom);
            BatchEditPanel.SetEntryRoom(selectedRoom);
            Repaint();
            SceneView.RepaintAll();
        }

        private void GoToBuildBlockout()
        {
            _activeTab = Tab.Build;
            SetMode(ToolMode.Blockout);
        }

        private void GoToBuildConnect()
        {
            _activeTab = Tab.Build;
            SetMode(ToolMode.Connect);
        }

        private static void RunQuickPlayFromSuggestion()
        {
            BlockoutModeHandler.QuickPlay();
        }

        private void DrawSingleRoomInfo(Room room)
        {
            DrawSingleRoomInfo(room, _activeTab == Tab.QuickEdit);
        }

        private void DrawSingleRoomInfo(Room room, bool includeQuickEditSections)
        {
            if (room == null) return;

            var roomData = room.Data;
            var box = room.GetComponent<BoxCollider2D>();
            var doors = room.GetComponentsInChildren<Door>(true);

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Room Inspector", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(IsPinnedRoom(room) ? "★ Pinned" : "☆ Pin", EditorStyles.miniButton, GUILayout.Width(76)))
            {
                TogglePinnedRoom(room);
            }
            EditorGUILayout.EndHorizontal();

            var assignedRoomData = (RoomSO)EditorGUILayout.ObjectField("RoomSO", roomData, typeof(RoomSO), false);
            if (assignedRoomData != roomData)
            {
                TrackRecentRoom(room);
                BatchEditPanel.AssignRoomData(room, assignedRoomData);
                EditorGUILayout.EndVertical();
                return;
            }

            if (roomData == null)
            {
                EditorGUILayout.HelpBox(
                    "This room is missing a RoomSO reference. Assign an existing RoomSO here, or use Validate Auto-Fix to create one.",
                    MessageType.Error);

                if (GUILayout.Button("Validate All", GUILayout.Height(22)))
                {
                    LevelValidator.ValidateAll();
                    Repaint();
                }

                EditorGUILayout.EndVertical();
                return;
            }

            string currentDisplayName = string.IsNullOrWhiteSpace(roomData.DisplayName)
                ? roomData.RoomID
                : roomData.DisplayName;

            string updatedRoomId = EditorGUILayout.DelayedTextField("Room ID", roomData.RoomID);
            string updatedDisplayName = EditorGUILayout.DelayedTextField("Display Name", currentDisplayName);
            if (updatedRoomId != roomData.RoomID || updatedDisplayName != currentDisplayName)
            {
                TrackRecentRoom(room);
                ApplyRoomIdentity(room, updatedRoomId, updatedDisplayName);
            }

            RoomFactory.StableRoomIdentity stableIdentity = RoomFactory.GenerateStableIdentity(roomData.NodeType, roomData.FloorLevel, room);
            EditorGUILayout.HelpBox(
                $"Stable Suggestion: {stableIdentity.RoomId} / {stableIdentity.DisplayName}",
                MessageType.None);

            var newNodeType = (RoomNodeType)EditorGUILayout.EnumPopup("Node Type", roomData.NodeType);
            if (newNodeType != roomData.NodeType)
            {
                TrackRecentRoom(room);
                BatchEditPanel.ApplySingleNodeType(room, newNodeType);
            }

            int newFloorLevel = EditorGUILayout.IntField("Floor Level", roomData.FloorLevel);
            if (newFloorLevel != roomData.FloorLevel)
            {
                TrackRecentRoom(room);
                BatchEditPanel.ApplySingleFloorLevel(room, newFloorLevel);
            }

            var newEncounter = (EncounterSO)EditorGUILayout.ObjectField("Encounter", roomData.Encounter, typeof(EncounterSO), false);
            if (newEncounter != roomData.Encounter)
            {
                TrackRecentRoom(room);
                BatchEditPanel.AssignEncounter(room, newEncounter);
            }

            if (box != null)
            {
                Vector2 editedSize = EditorGUILayout.Vector2Field("Size", box.size);
                Vector2 clampedSize = new Vector2(Mathf.Max(1f, editedSize.x), Mathf.Max(1f, editedSize.y));
                if (!Mathf.Approximately(clampedSize.x, box.size.x) || !Mathf.Approximately(clampedSize.y, box.size.y))
                {
                    TrackRecentRoom(room);
                    BatchEditPanel.ApplySingleSize(room, clampedSize);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This room is missing BoxCollider2D, so size cannot be edited here.", MessageType.Warning);
            }

            GUILayout.Label($"Doors: {doors.Length}");
            if (roomData.Encounter != null)
            {
                GUILayout.Label($"Encounter Waves: {roomData.Encounter.WaveCount}");
            }

            DrawConnectedRoomsSummary(doors);

            if (includeQuickEditSections)
            {
                DrawConnectionInspector(room, doors);
                DrawRoomRuntimeAssistSection(room, true);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus", GUILayout.Height(22)))
            {
                Selection.activeGameObject = room.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
                Repaint();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Duplicate", GUILayout.Height(22)))
            {
                DuplicateSelectedRoom(room);
            }
            if (GUILayout.Button("Set Entry", GUILayout.Height(22)))
            {
                TrackRecentRoom(room);
                BatchEditPanel.SetEntryRoom(room);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Stable Rename → {stableIdentity.RoomId}", GUILayout.Height(22)))
            {
                TrackRecentRoom(room);
                PerformStableRename(room);
            }
            if (GUILayout.Button("Select Connected", GUILayout.Height(22)))
            {
                TrackRecentRoom(room);
                SelectConnectedRooms(room, doors);
            }
            if (GUILayout.Button("Save Preset", GUILayout.Height(22)))
            {
                TrackRecentRoom(room);
                BatchEditPanel.SaveRoomPreset(room);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Context", GUILayout.Height(22)))
            {
                BatchEditPanel.ShowContextMenu(new List<Room> { room });
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConnectedRoomsSummary(Door[] doors)
        {
            GUILayout.Label("Connected Rooms", EditorStyles.boldLabel);

            var connectedRoomIds = new List<string>();
            foreach (var door in doors)
            {
                if (door == null || door.TargetRoom == null) continue;
                string targetRoomId = door.TargetRoom.RoomID;
                if (!connectedRoomIds.Contains(targetRoomId))
                {
                    connectedRoomIds.Add(targetRoomId);
                }
            }

            if (connectedRoomIds.Count == 0)
            {
                GUILayout.Label("None", EditorStyles.miniLabel);
                return;
            }

            GUILayout.Label(string.Join(", ", connectedRoomIds), EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawConnectionInspector(Room room, Door[] doors)
        {
            GUILayout.Space(6);
            GUILayout.Label("Connection Inspector", EditorStyles.boldLabel);

            var connectionDoors = new List<Door>();
            foreach (var door in doors)
            {
                if (door == null || door.TargetRoom == null) continue;
                connectionDoors.Add(door);
            }

            connectionDoors.Sort((a, b) => string.Compare(GetDoorConnectionLabel(a), GetDoorConnectionLabel(b), System.StringComparison.Ordinal));

            if (connectionDoors.Count == 0)
            {
                _selectedConnection = null;
                EditorGUILayout.HelpBox(
                    "This room has no authored Door links yet. Use Build > Connect or auto-connect helpers first.",
                    MessageType.None);
                return;
            }

            EnsureSelectedConnectionIsValid(connectionDoors);

            EditorGUILayout.HelpBox(
                "Tip: hover or click a connection line in SceneView to preview or select that authored traversal path directly.",
                MessageType.None);

            foreach (var door in connectionDoors)
            {
                EditorGUILayout.BeginHorizontal();

                var prevColor = GUI.color;
                GUI.color = GetConnectionTypeColor(door.ConnectionType);
                GUILayout.Label("■", GUILayout.Width(14));
                GUI.color = prevColor;

                bool isSelected = _selectedConnection == door;
                var buttonStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.miniButton;
                if (GUILayout.Button(GetDoorConnectionLabel(door), buttonStyle))
                {
                    SelectConnection(room, door);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_selectedConnection == null)
            {
                return;
            }

            DrawSelectedConnectionInspector(room, _selectedConnection);
        }

        private void EnsureSelectedConnectionIsValid(List<Door> connectionDoors)
        {
            if (_selectedConnection != null && connectionDoors.Contains(_selectedConnection))
            {
                return;
            }

            _selectedConnection = connectionDoors.Count > 0 ? connectionDoors[0] : null;
        }

        private void DrawSelectedConnectionInspector(Room ownerRoom, Door door)
        {
            if (ownerRoom == null || door == null)
            {
                return;
            }

            var reverseDoor = DoorWiringService.FindReverseDoor(door);

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Selected Connection", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("From Room", ownerRoom, typeof(Room), true);
                EditorGUILayout.ObjectField("To Room", door.TargetRoom, typeof(Room), true);
                EditorGUILayout.ObjectField("Target Spawn", door.TargetSpawnPoint, typeof(Transform), true);
            }

            EditorGUILayout.LabelField("Direction", GetConnectionDirectionLabel(ownerRoom, door.TargetRoom));
            EditorGUILayout.LabelField("Directionality", GetConnectionDirectionalityLabel(door, reverseDoor));
            EditorGUILayout.LabelField("Target Landing", GetTargetLandingSummary(door.TargetSpawnPoint));
            EditorGUILayout.LabelField("Gate ID", string.IsNullOrWhiteSpace(door.GateID) ? "—" : door.GateID);
            EditorGUILayout.LabelField("Ceremony", door.Ceremony.ToString());
            EditorGUILayout.LabelField("Reverse Link", reverseDoor != null ? reverseDoor.gameObject.name : "None (legal one-way)");

            if (reverseDoor == null)
            {
                EditorGUILayout.HelpBox(
                    "This connection is authored as one-way. That is legal; add a reverse door only if the player should be able to travel back.",
                    MessageType.Info);
            }

            var newConnectionType = (ConnectionType)EditorGUILayout.EnumPopup("Connection Type", door.ConnectionType);
            if (newConnectionType != door.ConnectionType)
            {
                DoorWiringService.SetConnectionType(door, newConnectionType);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete", GUILayout.Height(22)))
            {
                DeleteSelectedConnection(ownerRoom, door);
            }
            using (new EditorGUI.DisabledScope(reverseDoor == null))
            {
                if (GUILayout.Button("Flip Direction", GUILayout.Height(22)))
                {
                    FlipSelectedConnection(door);
                }
            }
            if (GUILayout.Button("Make Return", GUILayout.Height(22)))
            {
                DoorWiringService.SetConnectionType(door, ConnectionType.Return);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Recalc Landing", GUILayout.Height(22)))
            {
                DoorWiringService.RecalculateConnection(door);
            }
            if (GUILayout.Button("Focus To Room", GUILayout.Height(22)))
            {
                FocusRoomInScene(door.TargetRoom);
            }
            if (GUILayout.Button("Select Door", GUILayout.Height(22)))
            {
                SelectObjectInScene(door.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(door.TargetSpawnPoint == null))
            {
                if (GUILayout.Button("Select Target Spawn", GUILayout.Height(22)))
                {
                    SelectObjectInScene(door.TargetSpawnPoint);
                }
            }
            using (new EditorGUI.DisabledScope(reverseDoor == null))
            {
                if (GUILayout.Button("Select Reverse Door", GUILayout.Height(22)))
                {
                    SelectObjectInScene(reverseDoor.gameObject);
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawConnectionRuntimeAssistSection(ownerRoom, door);

            EditorGUILayout.EndVertical();
        }

        private void DeleteSelectedConnection(Room ownerRoom, Door door)
        {
            if (ownerRoom == null || door == null || door.TargetRoom == null)
            {
                return;
            }

            TrackRecentRoom(ownerRoom);
            DoorWiringService.DisconnectRooms(ownerRoom, door.TargetRoom);
            _selectedConnection = null;
            Selection.activeGameObject = ownerRoom.gameObject;
            Repaint();
            SceneView.RepaintAll();
        }

        private void FlipSelectedConnection(Door door)
        {
            if (door == null)
            {
                return;
            }

            var reverseDoor = DoorWiringService.FindReverseDoor(door);
            if (reverseDoor == null)
            {
                Debug.LogWarning($"[LevelArchitect] Cannot flip connection '{door.gameObject.name}': reverse door is missing.");
                return;
            }

            var reverseOwnerRoom = reverseDoor.GetComponentInParent<Room>();
            if (reverseOwnerRoom == null)
            {
                Debug.LogWarning($"[LevelArchitect] Cannot flip connection '{door.gameObject.name}': reverse owner room is missing.");
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(reverseOwnerRoom);
            _selectedConnection = reverseDoor;
            TrackRecentRoom(reverseOwnerRoom);
            Selection.activeGameObject = reverseOwnerRoom.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Repaint();
            SceneView.RepaintAll();
        }

        private static void FocusRoomInScene(Room room)
        {
            if (room == null)
            {
                return;
            }

            Selection.activeGameObject = room.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
        }

        private static void SelectObjectInScene(UnityEngine.Object targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            Selection.activeObject = targetObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
        }

        private static string GetDoorConnectionLabel(Door door)
        {
            if (door == null)
            {
                return "Missing Door";
            }

            string targetRoomId = door.TargetRoom != null ? door.TargetRoom.RoomID : "Missing Target";
            return $"{targetRoomId} · {door.ConnectionType} · {GetConnectionDirectionalityLabel(door)}";
        }

        private static string GetConnectionDirectionalityLabel(Door door, Door reverseDoor = null)
        {
            if (door == null)
            {
                return "Unknown";
            }

            if (reverseDoor == null)
            {
                reverseDoor = DoorWiringService.FindReverseDoor(door);
            }

            return reverseDoor != null ? "Bidirectional" : "One-way";
        }

        private static string GetConnectionDirectionLabel(Room ownerRoom, Room targetRoom)
        {
            if (ownerRoom == null || targetRoom == null)
            {
                return "Unknown";
            }

            var ownerBox = ownerRoom.GetComponent<BoxCollider2D>();
            var targetBox = targetRoom.GetComponent<BoxCollider2D>();
            if (ownerBox != null && targetBox != null)
            {
                Rect ownerRect = GetRoomWorldRect(ownerRoom, ownerBox);
                Rect targetRect = GetRoomWorldRect(targetRoom, targetBox);
                if (DoorWiringService.FindSharedEdge(ownerRect, targetRect, out _, out Vector2 direction))
                {
                    return GetDirectionLabel(direction);
                }
            }

            Vector2 fallbackDirection = ((Vector2)targetRoom.transform.position - (Vector2)ownerRoom.transform.position).normalized;
            return GetDirectionLabel(fallbackDirection);
        }

        private static string GetDirectionLabel(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? "East / Right" : "West / Left";
            }

            return direction.y >= 0f ? "North / Up" : "South / Down";
        }

        private static string GetTargetLandingSummary(Transform targetSpawnPoint)
        {
            if (targetSpawnPoint == null)
            {
                return "Missing";
            }

            Vector3 position = targetSpawnPoint.position;
            return $"{targetSpawnPoint.name} ({position.x:0.0}, {position.y:0.0})";
        }

        private void CreateRoomRuntimeAssist(Room room, LevelRuntimeAssistFactory.RoomAssistType assistType)
        {
            if (room == null)
            {
                return;
            }

            TrackRecentRoom(room);
            var createdObject = LevelRuntimeAssistFactory.CreateRoomAssist(room, assistType);
            if (createdObject == null)
            {
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(room);
            _selectedConnection = null;
            Repaint();
            SceneView.RepaintAll();
        }

        private void CreateConnectionLockAssist(Room ownerRoom, Door door)
        {
            if (ownerRoom == null || door == null)
            {
                return;
            }

            TrackRecentRoom(ownerRoom);
            var createdObject = LevelRuntimeAssistFactory.CreateLockAssist(ownerRoom, door);
            if (createdObject == null)
            {
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(ownerRoom);
            _selectedConnection = door;
            Repaint();
            SceneView.RepaintAll();
        }

        private void DuplicateSelectedRoom(Room room)
        {
            if (room == null) return;

            Vector3 duplicatePosition = room.transform.position + GetDuplicateRoomOffset(room);
            var duplicatedRoom = RoomFactory.DuplicateRoom(room, duplicatePosition);
            if (duplicatedRoom == null) return;

            _selectedRooms.Clear();
            _selectedConnection = null;
            _selectedRooms.Add(duplicatedRoom);
            TrackRecentRoom(duplicatedRoom);
            Selection.activeGameObject = duplicatedRoom.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Repaint();
            SceneView.RepaintAll();
        }

        private static Vector3 GetDuplicateRoomOffset(Room room)
        {
            var box = room != null ? room.GetComponent<BoxCollider2D>() : null;
            float offsetX = box != null ? Mathf.Max(4f, box.size.x + 2f) : 6f;
            return new Vector3(offsetX, 0f, 0f);
        }

        private void ApplyRoomIdentity(Room room, string roomId, string displayName)
        {
            if (room == null || room.Data == null) return;

            string sanitizedRoomId = string.IsNullOrWhiteSpace(roomId) ? room.gameObject.name : roomId.Trim();
            string sanitizedDisplayName = string.IsNullOrWhiteSpace(displayName) ? sanitizedRoomId : displayName.Trim();

            Undo.RecordObject(room.Data, "Edit Room Identity");
            var serialized = new SerializedObject(room.Data);
            serialized.FindProperty("_roomID").stringValue = sanitizedRoomId;
            serialized.FindProperty("_displayName").stringValue = sanitizedDisplayName;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(room.Data);

            Repaint();
            SceneView.RepaintAll();
        }

        private void PerformStableRename(Room room)
        {
            if (room == null || room.Data == null) return;
            if (!RoomFactory.ApplyStableIdentity(room)) return;

            Selection.activeGameObject = room.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Repaint();
            SceneView.RepaintAll();
        }

        private void SelectConnectedRooms(Room room, Door[] doors)
        {
            if (room == null) return;

            _selectedRooms.Clear();
            _selectedConnection = null;
            _selectedRooms.Add(room);
            TrackRecentRoom(room);

            foreach (var door in doors)
            {
                var targetRoom = door != null ? door.TargetRoom : null;
                if (targetRoom == null || _selectedRooms.Contains(targetRoom)) continue;
                _selectedRooms.Add(targetRoom);
            }

            Selection.activeGameObject = room.gameObject;
            Repaint();
            SceneView.RepaintAll();
        }

        private List<Room> GetFilteredRooms()
        {
            var filteredRooms = new List<Room>();
            var rooms = FindObjectsByType<Room>();
            foreach (var room in rooms)
            {
                if (MatchesRoomFilters(room))
                {
                    filteredRooms.Add(room);
                }
            }

            filteredRooms.Sort((a, b) => string.Compare(a.RoomID, b.RoomID, System.StringComparison.Ordinal));
            return filteredRooms;
        }

        private bool MatchesRoomFilters(Room room)
        {
            if (room == null) return false;

            if (_activeFloorLevel != int.MinValue)
            {
                int roomFloor = room.Data != null ? room.Data.FloorLevel : 0;
                if (roomFloor != _activeFloorLevel)
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(_roomSearchQuery))
            {
                return true;
            }

            string query = _roomSearchQuery.Trim();
            if (room.RoomID.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (room.Data != null)
            {
                if (!string.IsNullOrWhiteSpace(room.Data.DisplayName) &&
                    room.Data.DisplayName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (room.Data.NodeType.ToString().IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (room.Data.FloorLevel.ToString().IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetRoomListLabel(Room room)
        {
            if (room == null) return string.Empty;
            if (room.Data == null || string.IsNullOrWhiteSpace(room.Data.DisplayName) || room.Data.DisplayName == room.RoomID)
            {
                return room.RoomID;
            }

            return $"{room.RoomID} · {room.Data.DisplayName}";
        }

        private string GetQuickAccessLabel(Room room)
        {
            if (room == null)
            {
                return string.Empty;
            }

            string floorLabel = room.Data != null ? $"F{room.Data.FloorLevel}" : "F?";
            return $"{GetRoomListLabel(room)} · {room.NodeType} · {floorLabel}";
        }

        private List<Room> GetQuickAccessRooms(List<Room> source)
        {
            var results = new List<Room>();
            if (source == null)
            {
                return results;
            }

            var seen = new HashSet<int>();
            foreach (var room in source)
            {
                if (room == null)
                {
                    continue;
                }

                int roomId = room.GetInstanceID();
                if (!seen.Add(roomId))
                {
                    continue;
                }

                if (!MatchesRoomFilters(room))
                {
                    continue;
                }

                results.Add(room);
            }

            return results;
        }

        private Room GetPreviousRecentRoom()
        {
            SanitizeQuickAccessRooms();

            Room currentRoom = _selectedRooms.Count == 1 ? _selectedRooms[0] : null;
            foreach (var room in _recentRooms)
            {
                if (room == null || room == currentRoom)
                {
                    continue;
                }

                if (!MatchesRoomFilters(room))
                {
                    continue;
                }

                return room;
            }

            return null;
        }

        private bool IsPinnedRoom(Room room)
        {
            return room != null && _pinnedRooms != null && _pinnedRooms.Contains(room);
        }

        private void TogglePinnedRoom(Room room)
        {
            if (room == null)
            {
                return;
            }

            SanitizeQuickAccessRooms();

            if (_pinnedRooms.Contains(room))
            {
                _pinnedRooms.Remove(room);
            }
            else
            {
                _pinnedRooms.Insert(0, room);
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void TrackRecentRoom(Room room)
        {
            if (room == null)
            {
                return;
            }

            SanitizeQuickAccessRooms();
            _recentRooms.Remove(room);
            _recentRooms.Insert(0, room);
            if (_recentRooms.Count > MAX_RECENT_ROOMS)
            {
                _recentRooms.RemoveRange(MAX_RECENT_ROOMS, _recentRooms.Count - MAX_RECENT_ROOMS);
            }
        }

        private void OpenRoomFromQuickAccess(Room room, bool frameInScene)
        {
            if (room == null)
            {
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(room);
            _selectedConnection = null;
            TrackRecentRoom(room);

            Selection.activeGameObject = room.gameObject;
            if (frameInScene)
            {
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void RecallPreviousRoom()
        {
            var previousRoom = GetPreviousRecentRoom();
            if (previousRoom == null)
            {
                return;
            }

            OpenRoomFromQuickAccess(previousRoom, true);
        }

        private void SanitizeQuickAccessRooms()
        {
            RemoveInvalidRooms(_pinnedRooms, false);
            RemoveInvalidRooms(_recentRooms, true);
        }

        private static void RemoveInvalidRooms(List<Room> rooms, bool trimToRecentLimit)
        {
            if (rooms == null)
            {
                return;
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < rooms.Count;)
            {
                var room = rooms[i];
                if (room == null || !seen.Add(room.GetInstanceID()))
                {
                    rooms.RemoveAt(i);
                    continue;
                }

                i++;
            }

            if (trimToRecentLimit && rooms.Count > MAX_RECENT_ROOMS)
            {
                rooms.RemoveRange(MAX_RECENT_ROOMS, rooms.Count - MAX_RECENT_ROOMS);
            }
        }

        // ──────────────────── Scene Input Processing ────────────────────

        private void ProcessSceneInput(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null) return;

            // Update hovered room / connection
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                UpdateHoveredRoom(e);
                UpdateHoveredConnection(sceneView, e);
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

        private void UpdateHoveredConnection(SceneView sceneView, Event e)
        {
            _hoveredConnection = null;

            if (sceneView == null || _currentMode != ToolMode.Select || !AllowSceneSelectionInteraction)
            {
                return;
            }

            if (IsPointerOverSceneOverlay(sceneView, e.mousePosition) || RoomBlockoutRenderer.HasActiveInteraction)
            {
                return;
            }

            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            var rooms = FindObjectsByType<Room>();
            if (ConnectionGizmoDrawer.TryPickConnection(rooms, worldPos, out _, out Door hoveredDoor))
            {
                _hoveredConnection = hoveredDoor;
            }
        }

        // ──────────────────── Mode Management ────────────────────

        public void SetMode(ToolMode mode)
        {
            CancelAllSceneToolInteractions();

            if (_currentMode == mode)
            {
                Repaint();
                SceneView.RepaintAll();
                return;
            }

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

            _selectedConnection = null;

            if (_selectedRooms.Count == 1)
            {
                TrackRecentRoom(_selectedRooms[0]);
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

            TrackRecentRoom(room);
            _selectedConnection = null;
            Repaint();
            SceneView.RepaintAll();
        }

        public void SelectConnection(Room ownerRoom, Door door, bool frameInScene = false)
        {
            if (ownerRoom == null || door == null)
            {
                return;
            }

            _selectedRooms.Clear();
            _selectedRooms.Add(ownerRoom);
            TrackRecentRoom(ownerRoom);
            _selectedConnection = door;
            Selection.activeGameObject = ownerRoom.gameObject;
            if (frameInScene)
            {
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            Repaint();
            SceneView.RepaintAll();
        }

        public void DeselectRoom(Room room)
        {
            _selectedRooms.Remove(room);
            _selectedConnection = null;
            Repaint();
            SceneView.RepaintAll();
        }

        public void ClearSelection()
        {
            _selectedRooms.Clear();
            _selectedConnection = null;
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
            if (_selectedConnection == null) _selectedConnection = null; // Unity null check for destroyed Door
            if (_hoveredRoom == null) _hoveredRoom = null; // Unity null check
            SanitizeQuickAccessRooms();

            _roomAuthoringStates.Clear();

            Repaint();
            SceneView.RepaintAll();
        }

        private void OnUndoRedo()
        {
            _roomAuthoringStates.Clear();
            if (_selectedConnection == null) _selectedConnection = null;
            SanitizeQuickAccessRooms();
            DoorWiringService.SynchronizeAllRoomConnections();
            Repaint();
            SceneView.RepaintAll();
        }

    }
}
