using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Visual level designer window for building levels like building blocks.
    /// Features:
    /// - Drag-and-drop room placement
    /// - Visual topology view
    /// - Room connection management
    /// - One-click generation to Unity scene
    /// </summary>
    public class LevelDesignerWindow : EditorWindow
    {
        private const string WindowTitle = "Level Designer";
        private const string MenuPath = "Window/ProjectArk/Level Designer";

        // ──────────────────── State ────────────────────

        [SerializeField] private LevelScaffoldData _currentScaffold;
        [SerializeField] private LevelElementLibrary _elementLibrary;

        private ScaffoldRoom _selectedRoom;
        private Vector2 _scrollPosition;
        private Vector2 _topologyScrollPosition;
        private float _zoomLevel = 1f;
        private Vector2 _panOffset = Vector2.zero;
        private bool _isDraggingRoom;
        private ScaffoldRoom _draggingRoom;
        private Vector2 _dragStartMousePos;
        private Vector3 _dragStartRoomPos;
        private bool _isConnectingRooms;
        private ScaffoldRoom _connectionStartRoom;
        private Vector2 _connectionStartPos;

        // ──────────────────── Menu Item ────────────────────

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDesignerWindow>(WindowTitle);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        // ──────────────────── Lifecycle ────────────────────

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ──────────────────── GUI ────────────────────

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawSettingsPanel();
            DrawToolbar();
            DrawRoomList();
            DrawTopologyView();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the header section.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draws the settings panel for scaffold and library.
        /// </summary>
        private void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _currentScaffold = (LevelScaffoldData)EditorGUILayout.ObjectField(
                "Level Scaffold", _currentScaffold, typeof(LevelScaffoldData), false);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedRoom = null;
            }

            _elementLibrary = (LevelElementLibrary)EditorGUILayout.ObjectField(
                "Element Library", _elementLibrary, typeof(LevelElementLibrary), false);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the toolbar with quick actions.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("New Scaffold", GUILayout.Height(30)))
            {
                CreateNewScaffold();
            }

            if (GUILayout.Button("Add Room", GUILayout.Height(30)))
            {
                AddNewRoom();
            }

            EditorGUI.BeginDisabledGroup(_currentScaffold == null || _elementLibrary == null);
            if (GUILayout.Button("Generate to Scene", GUILayout.Height(30)))
            {
                GenerateToScene();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the list of rooms in the current scaffold.
        /// </summary>
        private void DrawRoomList()
        {
            if (_currentScaffold == null)
            {
                EditorGUILayout.HelpBox("Select a Level Scaffold to edit rooms.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Rooms", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_currentScaffold.Rooms.Count == 0)
            {
                EditorGUILayout.HelpBox("No rooms in this scaffold. Click 'Add Room' to create one.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _currentScaffold.Rooms.Count; i++)
                {
                    DrawRoomListItem(_currentScaffold.Rooms[i], i);
                }
            }

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws a single room in the room list.
        /// </summary>
        private void DrawRoomListItem(ScaffoldRoom room, int index)
        {
            bool isSelected = _selectedRoom == room;
            Color bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.white;

            GUIStyle listItemStyle = new GUIStyle(EditorStyles.helpBox);
            if (isSelected)
            {
                listItemStyle.normal.background = MakeTex(2, 2, bgColor);
            }

            using (new EditorGUILayout.VerticalScope(listItemStyle))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(isSelected ? "▼" : "▶", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    _selectedRoom = isSelected ? null : room;
                }

                EditorGUILayout.BeginVertical();
                room.DisplayName = EditorGUILayout.TextField("Name", room.DisplayName);
                EditorGUILayout.LabelField($"Type: {room.RoomType} | Pos: {room.Position}");
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Delete Room", $"Are you sure you want to delete '{room.DisplayName}'?", "Delete", "Cancel"))
                    {
                        Undo.RecordObject(_currentScaffold, "Delete Room");
                        _currentScaffold.RemoveRoom(room);
                        if (_selectedRoom == room)
                        {
                            _selectedRoom = null;
                        }
                        EditorUtility.SetDirty(_currentScaffold);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (isSelected)
                {
                    EditorGUILayout.Space(5);
                    DrawRoomEditor(room);
                }
            }

            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Creates a solid color texture for GUI backgrounds.
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Draws the detailed editor for a selected room.
        /// </summary>
        private void DrawRoomEditor(ScaffoldRoom room)
        {
            EditorGUI.indentLevel++;

            room.RoomType = (RoomType)EditorGUILayout.EnumPopup("Room Type", room.RoomType);
            room.Position = EditorGUILayout.Vector3Field("Position", room.Position);
            room.Size = EditorGUILayout.Vector2Field("Size", room.Size);
            room.RoomSO = (RoomSO)EditorGUILayout.ObjectField("Room SO", room.RoomSO, typeof(RoomSO), false);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);

            if (room.Connections.Count == 0)
            {
                EditorGUILayout.HelpBox("No connections. Click 'Add Connection' or use the topology view.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < room.Connections.Count; i++)
                {
                    DrawConnectionEditor(room, room.Connections[i], i);
                }
            }

            if (GUILayout.Button("Add Connection"))
            {
                Undo.RecordObject(_currentScaffold, "Add Connection");
                room.AddConnection(new ScaffoldDoorConnection());
                EditorUtility.SetDirty(_currentScaffold);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draws the editor for a single door connection.
        /// </summary>
        private void DrawConnectionEditor(ScaffoldRoom room, ScaffoldDoorConnection connection, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Connection {index + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                Undo.RecordObject(_currentScaffold, "Remove Connection");
                room.RemoveConnection(connection);
                EditorUtility.SetDirty(_currentScaffold);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            var targetRooms = _currentScaffold.Rooms.Where(r => r != room).ToList();
            int currentTargetIndex = targetRooms.FindIndex(r => r.RoomID == connection.TargetRoomID);
            string[] targetNames = targetRooms.Select(r => r.DisplayName).ToArray();

            EditorGUI.BeginChangeCheck();
            int newTargetIndex = EditorGUILayout.Popup("Target Room", currentTargetIndex, targetNames);
            if (EditorGUI.EndChangeCheck() && newTargetIndex >= 0)
            {
                Undo.RecordObject(_currentScaffold, "Change Target Room");
                connection.TargetRoomID = targetRooms[newTargetIndex].RoomID;
                EditorUtility.SetDirty(_currentScaffold);
            }

            connection.DoorPosition = EditorGUILayout.Vector3Field("Door Position", connection.DoorPosition);
            connection.DoorDirection = EditorGUILayout.Vector2Field("Door Direction", connection.DoorDirection);
            connection.IsLayerTransition = EditorGUILayout.Toggle("Is Layer Transition", connection.IsLayerTransition);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the visual topology view.
        /// </summary>
        private void DrawTopologyView()
        {
            if (_currentScaffold == null)
                return;

            EditorGUILayout.LabelField("Topology View", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            Rect viewRect = EditorGUILayout.GetControlRect(false, 400);
            Handles.BeginGUI();

            _topologyScrollPosition = GUI.BeginScrollView(
                viewRect,
                _topologyScrollPosition,
                new Rect(0, 0, 2000, 2000));

            DrawTopologyBackground();
            DrawRoomConnections();
            DrawTopologyRooms(viewRect);
            HandleTopologyInput(viewRect);

            GUI.EndScrollView();

            Handles.EndGUI();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset View"))
            {
                _topologyScrollPosition = Vector2.zero;
                _zoomLevel = 1f;
            }
            if (GUILayout.Button("Focus All Rooms"))
            {
                FocusAllRooms();
            }
            _zoomLevel = EditorGUILayout.Slider("Zoom", _zoomLevel, 0.25f, 2f);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Adjusts scroll position and zoom to focus all rooms.
        /// </summary>
        private void FocusAllRooms()
        {
            if (_currentScaffold == null || _currentScaffold.Rooms.Count == 0)
                return;

            _topologyScrollPosition = Vector2.zero;
            _zoomLevel = 1f;
        }

        /// <summary>
        /// Draws the topology view background grid.
        /// </summary>
        private void DrawTopologyBackground()
        {
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            float gridSize = 50f * _zoomLevel;

            for (float x = 0; x < 2000; x += gridSize)
            {
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, 2000));
            }

            for (float y = 0; y < 2000; y += gridSize)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(2000, y));
            }
        }

        /// <summary>
        /// Draws connections between rooms in the topology view.
        /// </summary>
        private void DrawRoomConnections()
        {
            foreach (var room in _currentScaffold.Rooms)
            {
                Vector2 roomPos = WorldToTopology(room.Position);

                foreach (var connection in room.Connections)
                {
                    var targetRoom = _currentScaffold.Rooms.FirstOrDefault(r => r.RoomID == connection.TargetRoomID);
                    if (targetRoom == null)
                        continue;

                    Vector2 targetPos = WorldToTopology(targetRoom.Position);
                    Handles.color = new Color(0.5f, 0.8f, 1f, 0.8f);
                    Handles.DrawLine(roomPos, targetPos);
                }
            }
        }

        /// <summary>
        /// Draws rooms in the topology view.
        /// </summary>
        private void DrawTopologyRooms(Rect viewRect)
        {
            if (_currentScaffold.Rooms.Count == 0)
            {
                Handles.color = Color.gray;
                Handles.Label(new Vector2(1000, 1000), "No rooms yet. Click 'Add Room' to create one.");
                return;
            }

            float topologyScale = 10f;

            foreach (var room in _currentScaffold.Rooms)
            {
                Vector2 roomPos = WorldToTopology(room.Position);
                float roomWidth = room.Size.x * topologyScale * _zoomLevel;
                float roomHeight = room.Size.y * topologyScale * _zoomLevel;

                Rect roomRect = new Rect(
                    roomPos.x - roomWidth / 2,
                    roomPos.y - roomHeight / 2,
                    roomWidth,
                    roomHeight);

                bool isSelected = _selectedRoom == room;
                Color roomColor = GetRoomColor(room.RoomType, isSelected);

                Handles.DrawSolidRectangleWithOutline(roomRect, roomColor, isSelected ? Color.white : Color.gray);

                Handles.color = Color.white;
                Vector2 labelPos = roomPos + new Vector2(-room.DisplayName.Length * 3, 5);
                Handles.Label(labelPos, room.DisplayName);
            }
        }

        /// <summary>
        /// Handles input in the topology view.
        /// </summary>
        private void HandleTopologyInput(Rect viewRect)
        {
            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;
            float topologyScale = 10f;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                foreach (var room in _currentScaffold.Rooms)
                {
                    Vector2 roomPos = WorldToTopology(room.Position);
                    float roomWidth = room.Size.x * topologyScale * _zoomLevel;
                    float roomHeight = room.Size.y * topologyScale * _zoomLevel;

                    Rect roomRect = new Rect(
                        roomPos.x - roomWidth / 2,
                        roomPos.y - roomHeight / 2,
                        roomWidth,
                        roomHeight);

                    if (roomRect.Contains(mousePos))
                    {
                        _selectedRoom = room;
                        _isDraggingRoom = true;
                        _draggingRoom = room;
                        _dragStartMousePos = mousePos;
                        _dragStartRoomPos = room.Position;
                        evt.Use();
                        break;
                    }
                }
                Repaint();
            }
            else if (evt.type == EventType.MouseDrag && _isDraggingRoom && _draggingRoom != null)
            {
                Vector2 delta = (mousePos - _dragStartMousePos) / (topologyScale * _zoomLevel);
                Undo.RecordObject(_currentScaffold, "Move Room");
                _draggingRoom.Position = _dragStartRoomPos + new Vector3(delta.x, -delta.y, 0);
                EditorUtility.SetDirty(_currentScaffold);
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                _isDraggingRoom = false;
                _draggingRoom = null;
                evt.Use();
            }
        }

        /// <summary>
        /// Gets the color for a room type.
        /// </summary>
        private Color GetRoomColor(RoomType type, bool isSelected)
        {
            float alpha = isSelected ? 0.9f : 0.6f;
            switch (type)
            {
                case RoomType.Normal:
                    return new Color(0.3f, 0.5f, 0.7f, alpha);
                case RoomType.Arena:
                    return new Color(0.8f, 0.6f, 0.3f, alpha);
                case RoomType.Boss:
                    return new Color(0.8f, 0.3f, 0.3f, alpha);
                case RoomType.Safe:
                    return new Color(0.3f, 0.8f, 0.5f, alpha);
                default:
                    return new Color(0.5f, 0.5f, 0.5f, alpha);
            }
        }

        /// <summary>
        /// Converts world coordinates to topology view coordinates.
        /// </summary>
        private Vector2 WorldToTopology(Vector3 worldPos)
        {
            return new Vector2(
                1000 + worldPos.x * _zoomLevel,
                1000 - worldPos.y * _zoomLevel);
        }

        /// <summary>
        /// Converts topology view coordinates to world coordinates.
        /// </summary>
        private Vector3 TopologyToWorld(Vector2 topologyPos)
        {
            return new Vector3(
                (topologyPos.x - 1000) / _zoomLevel,
                -(topologyPos.y - 1000) / _zoomLevel,
                0);
        }

        // ──────────────────── Scene GUI ────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_currentScaffold == null)
                return;

            foreach (var room in _currentScaffold.Rooms)
            {
                DrawRoomGizmo(room);
            }
        }

        /// <summary>
        /// Draws a gizmo for a room in the Scene view.
        /// </summary>
        private void DrawRoomGizmo(ScaffoldRoom room)
        {
            bool isSelected = _selectedRoom == room;
            Color color = GetRoomColor(room.RoomType, isSelected);

            Handles.color = color;
            Handles.DrawWireCube(room.Position, new Vector3(room.Size.x, room.Size.y, 1));

            if (isSelected)
            {
                Handles.color = Color.white;
                Handles.Label(room.Position + Vector3.up * (room.Size.y / 2 + 1), room.DisplayName);
            }
        }

        // ──────────────────── Actions ────────────────────

        /// <summary>
        /// Creates a new LevelScaffoldData asset.
        /// </summary>
        private void CreateNewScaffold()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Level Scaffold",
                "NewLevelScaffold",
                "asset",
                "Create a new level scaffold");

            if (string.IsNullOrEmpty(path))
                return;

            LevelScaffoldData scaffold = ScriptableObject.CreateInstance<LevelScaffoldData>();
            AssetDatabase.CreateAsset(scaffold, path);
            AssetDatabase.SaveAssets();

            _currentScaffold = scaffold;
            _selectedRoom = null;

            Debug.Log($"[LevelDesigner] Created new scaffold: {path}");
        }

        /// <summary>
        /// Adds a new room to the current scaffold.
        /// </summary>
        private void AddNewRoom()
        {
            if (_currentScaffold == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a Level Scaffold first.", "OK");
                return;
            }

            Undo.RecordObject(_currentScaffold, "Add Room");

            int roomCount = _currentScaffold.Rooms.Count;
            float gridSpacing = 30f;
            
            Vector3 newPosition;
            if (roomCount == 0)
            {
                newPosition = Vector3.zero;
            }
            else
            {
                int row = roomCount / 3;
                int col = roomCount % 3;
                newPosition = new Vector3(col * gridSpacing, -row * gridSpacing, 0);
            }

            ScaffoldRoom newRoom = new ScaffoldRoom
            {
                DisplayName = $"Room {roomCount + 1}",
                Position = newPosition,
                Size = new Vector2(20, 15)
            };

            _currentScaffold.AddRoom(newRoom);
            _selectedRoom = newRoom;
            
            if (roomCount == 0)
            {
                _topologyScrollPosition = new Vector2(0, 0);
            }
            
            EditorUtility.SetDirty(_currentScaffold);

            Repaint();
        }

        /// <summary>
        /// Generates the current scaffold to the Unity scene.
        /// </summary>
        private void GenerateToScene()
        {
            if (_currentScaffold == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a Level Scaffold first.", "OK");
                return;
            }

            if (_elementLibrary == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a Level Element Library first.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Generate Level",
                $"Generate level '{_currentScaffold.LevelName}' to the current scene?\n\nThis will create new GameObjects.",
                "Generate",
                "Cancel"))
            {
                return;
            }

            LevelGenerator.GenerateLevel(_currentScaffold, _elementLibrary);
        }
    }
}
