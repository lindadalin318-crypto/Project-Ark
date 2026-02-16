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
    /// - In-room element placement
    /// - One-click generation to Unity scene
    /// </summary>
    [System.Obsolete("Use LevelArchitectWindow instead. Open via Window > ProjectArk > Level Architect.")]
    public class LevelDesignerWindow : EditorWindow
    {
        private const string WindowTitle = "Level Designer";
        private const string MenuPath = "Window/ProjectArk/Level Designer";

        private enum EditorMode
        {
            Topology,
            RoomElements
        }

        private enum ElementTransformTool
        {
            Select,
            Move,
            Rotate,
            Scale
        }

        // ──────────────────── State ────────────────────

        [SerializeField] private LevelScaffoldData _currentScaffold;
        [SerializeField] private LevelElementLibrary _elementLibrary;

        private EditorMode _currentMode = EditorMode.Topology;
        private ElementTransformTool _elementTool = ElementTransformTool.Move;
        private ScaffoldRoom _selectedRoom;
        private ScaffoldElement _selectedElement;
        private ScaffoldElementType _selectedElementType;
        private Vector2 _scrollPosition;
        private Vector2 _topologyScrollPosition;
        private float _zoomLevel = 1f;
        private Vector2 _panOffset = Vector2.zero;
        private bool _isDraggingRoom;
        private ScaffoldRoom _draggingRoom;
        private Vector2 _dragStartMousePos;
        private Vector3 _dragStartRoomPos;
        private bool _isDraggingElement;
        private ScaffoldElement _draggingElement;
        private Vector2 _elementDragStartMouse;
        private Vector3 _elementDragStartPos;
        private float _elementDragStartRotation;
        private Vector3 _elementDragStartScale;
        private bool _isConnectingRooms;
        private ScaffoldRoom _connectionStartRoom;
        private Vector2 _connectionStartPos;

        [Header("Snap Settings")]
        private float _roomSnapThreshold = 3f;

        // ──────────────────── Menu Item ────────────────────

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDesignerWindow>(WindowTitle);
            window.minSize = new Vector2(1000, 700);
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
            DrawModeTabs();

            if (_currentMode == EditorMode.Topology)
            {
                DrawRoomList();
                DrawTopologyView();
            }
            else
            {
                DrawElementLibrary();
                DrawRoomElementCanvas();
            }

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

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Room Snap Settings", EditorStyles.boldLabel);
            _roomSnapThreshold = EditorGUILayout.Slider("Snap Threshold", _roomSnapThreshold, 0.5f, 10f);
            EditorGUILayout.HelpBox("Threshold for room snapping (in world units). Higher = easier to snap.", MessageType.Info);

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
        /// Draws the mode selection tabs.
        /// </summary>
        private void DrawModeTabs()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(_currentMode == EditorMode.Topology, "Topology", "ButtonLeft"))
            {
                _currentMode = EditorMode.Topology;
            }

            EditorGUI.BeginDisabledGroup(_selectedRoom == null);
            if (GUILayout.Toggle(_currentMode == EditorMode.RoomElements, "Room Elements", "ButtonRight"))
            {
                _currentMode = EditorMode.RoomElements;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_currentMode == EditorMode.RoomElements && _selectedRoom == null)
            {
                EditorGUILayout.HelpBox("Select a room first to edit elements.", MessageType.Info);
            }

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the element library panel.
        /// </summary>
        private void DrawElementLibrary()
        {
            if (_currentScaffold == null)
                return;

            EditorGUILayout.LabelField("Element Library", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var elementTypes = Enum.GetValues(typeof(ScaffoldElementType)).Cast<ScaffoldElementType>().ToList();

            for (int i = 0; i < elementTypes.Count; i++)
            {
                bool isSelected = _selectedElementType == elementTypes[i];
                Color bgColor = isSelected ? new Color(0.3f, 0.6f, 0.9f, 0.3f) : Color.white;

                GUIStyle btnStyle = new GUIStyle(EditorStyles.miniButton);
                if (isSelected)
                {
                    btnStyle.normal.background = MakeTex(2, 2, bgColor);
                }

                if (GUILayout.Button(elementTypes[i].ToString(), btnStyle))
                {
                    _selectedElementType = elementTypes[i];
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the room element editing canvas.
        /// </summary>
        private void DrawRoomElementCanvas()
        {
            if (_currentScaffold == null || _selectedRoom == null)
                return;

            EditorGUILayout.LabelField("Room Elements - " + _selectedRoom.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _elementTool = (ElementTransformTool)GUILayout.Toolbar((int)_elementTool, new[] { "Q", "W", "E", "R" }, GUILayout.Height(25));
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            Rect viewRect = EditorGUILayout.GetControlRect(false, 400);
            Handles.BeginGUI();

            _topologyScrollPosition = GUI.BeginScrollView(
                viewRect,
                _topologyScrollPosition,
                new Rect(0, 0, viewRect.width, viewRect.height));

            DrawRoomCanvasBackground(viewRect);
            DrawRoomCanvasElements(viewRect);
            HandleRoomCanvasInput(viewRect);

            GUI.EndScrollView();

            Handles.EndGUI();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected Element"))
            {
                AddElementToRoom();
            }
            if (GUILayout.Button("Remove Selected"))
            {
                RemoveSelectedElement();
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedElement != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Element Properties", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                _selectedElement.GizmoShape = (ElementGizmoShape)EditorGUILayout.EnumPopup("Gizmo Shape", _selectedElement.GizmoShape);
                _selectedElement.LocalPosition = EditorGUILayout.Vector3Field("Position", _selectedElement.LocalPosition);
                _selectedElement.Rotation = EditorGUILayout.Slider("Rotation", _selectedElement.Rotation, -180, 180);
                _selectedElement.Scale = EditorGUILayout.Vector3Field("Scale", _selectedElement.Scale);

                if (_selectedElement.ElementType == ScaffoldElementType.Door)
                {
                    _selectedElement.EnsureDoorConfigExists();
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Door Configuration", EditorStyles.boldLabel);
                    _selectedElement.DoorConfig.InitialState = (DoorState)EditorGUILayout.EnumPopup("Initial State", _selectedElement.DoorConfig.InitialState);
                    _selectedElement.DoorConfig.RequiredKeyID = EditorGUILayout.TextField("Required Key ID", _selectedElement.DoorConfig.RequiredKeyID);

                    EditorGUILayout.LabelField("Open During Phases (Locked_Schedule only)");
                    EditorGUI.indentLevel++;
                    int oldSize = _selectedElement.DoorConfig.OpenDuringPhases?.Length ?? 0;
                    int newSize = EditorGUILayout.IntField("Size", oldSize);
                    if (newSize != oldSize)
                    {
                        int[] newPhases = new int[newSize];
                        if (_selectedElement.DoorConfig.OpenDuringPhases != null)
                        {
                            Array.Copy(_selectedElement.DoorConfig.OpenDuringPhases, newPhases, Math.Min(oldSize, newSize));
                        }
                        _selectedElement.DoorConfig.OpenDuringPhases = newPhases;
                    }
                    if (_selectedElement.DoorConfig.OpenDuringPhases != null)
                    {
                        for (int i = 0; i < _selectedElement.DoorConfig.OpenDuringPhases.Length; i++)
                        {
                            _selectedElement.DoorConfig.OpenDuringPhases[i] = EditorGUILayout.IntField($"Phase {i + 1}", _selectedElement.DoorConfig.OpenDuringPhases[i]);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the canvas background and room outline.
        /// </summary>
        private void DrawRoomCanvasBackground(Rect viewRect)
        {
            Handles.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Handles.DrawSolidRectangleWithOutline(viewRect, new Color(0.15f, 0.15f, 0.15f, 1f), new Color(0.3f, 0.3f, 0.3f, 1f));

            float roomWidth = _selectedRoom.Size.x * 20;
            float roomHeight = _selectedRoom.Size.y * 20;
            Vector2 center = new Vector2(viewRect.width / 2, viewRect.height / 2);

            Rect roomRect = new Rect(
                center.x - roomWidth / 2,
                center.y - roomHeight / 2,
                roomWidth,
                roomHeight);

            Color roomColor = GetRoomColor(_selectedRoom.RoomType, true);
            Handles.DrawSolidRectangleWithOutline(roomRect, new Color(roomColor.r, roomColor.g, roomColor.b, 0.3f), Color.white);

            Handles.color = Color.gray;
            Handles.Label(center + new Vector2(-_selectedRoom.DisplayName.Length * 5, -roomHeight / 2 - 20), _selectedRoom.DisplayName);
        }

        /// <summary>
        /// Draws elements in the canvas.
        /// </summary>
        private void DrawRoomCanvasElements(Rect viewRect)
        {
            float roomWidth = _selectedRoom.Size.x * 20;
            float roomHeight = _selectedRoom.Size.y * 20;
            Vector2 center = new Vector2(viewRect.width / 2, viewRect.height / 2);

            foreach (var element in _selectedRoom.Elements)
            {
                Vector2 elementPos = center + new Vector2(element.LocalPosition.x * 20, -element.LocalPosition.y * 20);
                bool isSelected = _selectedElement == element;
                float size = isSelected ? 15f : 10f;

                Color elementColor = isSelected ? Color.yellow : GetElementColor(element.ElementType);
                Handles.color = elementColor;

                switch (element.GizmoShape)
                {
                    case ElementGizmoShape.Square:
                        Rect rect = new Rect(elementPos.x - size, elementPos.y - size, size * 2, size * 2);
                        Handles.DrawSolidRectangleWithOutline(rect, elementColor, Color.clear);
                        break;
                    case ElementGizmoShape.Circle:
                        Handles.DrawSolidDisc(elementPos, Vector3.forward, size);
                        break;
                    case ElementGizmoShape.Diamond:
                        for (int i = 0; i < 4; i++)
                        {
                            float angle = i * Mathf.PI / 2 + Mathf.PI / 4;
                            Vector2 p1 = new Vector2(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size);
                            float angle2 = (i + 1) * Mathf.PI / 2 + Mathf.PI / 4;
                            Vector2 p2 = new Vector2(Mathf.Cos(angle2) * size, Mathf.Sin(angle2) * size);
                            Handles.DrawLine((Vector3)elementPos + (Vector3)p1, (Vector3)elementPos + (Vector3)p2);
                        }
                        break;
                }

                if (isSelected)
                {
                    DrawElementTransformGizmo(elementPos, element, size);
                }

                Handles.color = Color.white;
                Handles.Label(elementPos + new Vector2(-element.ElementType.ToString().Length * 4, -size - 8), element.ElementType.ToString());
            }
        }

        /// <summary>
        /// Draws the transform gizmo for selected element.
        /// </summary>
        private void DrawElementTransformGizmo(Vector2 elementPos, ScaffoldElement element, float size)
        {
            Handles.color = Color.white;

            if (_elementTool == ElementTransformTool.Move)
            {
                Handles.color = Color.red;
                Handles.DrawLine(elementPos, elementPos + Vector2.right * 30);
                Handles.color = Color.green;
                Handles.DrawLine(elementPos, elementPos + Vector2.up * 30);
            }
            else if (_elementTool == ElementTransformTool.Rotate)
            {
                Handles.DrawWireDisc(elementPos, Vector3.forward, size + 15);
            }
            else if (_elementTool == ElementTransformTool.Scale)
            {
                Handles.color = Color.red;
                Handles.DrawLine(elementPos, elementPos + Vector2.right * 25);
                Handles.DrawSolidDisc(elementPos + Vector2.right * 25, Vector3.forward, 5);
                Handles.color = Color.green;
                Handles.DrawLine(elementPos, elementPos + Vector2.up * 25);
                Handles.DrawSolidDisc(elementPos + Vector2.up * 25, Vector3.forward, 5);
            }
        }

        /// <summary>
        /// Handles input in the room canvas.
        /// </summary>
        private void HandleRoomCanvasInput(Rect viewRect)
        {
            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;

            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Q)
                {
                    _elementTool = ElementTransformTool.Select;
                    evt.Use();
                    Repaint();
                }
                else if (evt.keyCode == KeyCode.W)
                {
                    _elementTool = ElementTransformTool.Move;
                    evt.Use();
                    Repaint();
                }
                else if (evt.keyCode == KeyCode.E)
                {
                    _elementTool = ElementTransformTool.Rotate;
                    evt.Use();
                    Repaint();
                }
                else if (evt.keyCode == KeyCode.R)
                {
                    _elementTool = ElementTransformTool.Scale;
                    evt.Use();
                    Repaint();
                }
            }

            float roomWidth = _selectedRoom.Size.x * 20;
            float roomHeight = _selectedRoom.Size.y * 20;
            Vector2 center = new Vector2(viewRect.width / 2, viewRect.height / 2);

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                foreach (var element in _selectedRoom.Elements)
                {
                    Vector2 elementPos = center + new Vector2(element.LocalPosition.x * 20, -element.LocalPosition.y * 20);
                    if (Vector2.Distance(mousePos, elementPos) < 15f)
                    {
                        _selectedElement = element;
                        _isDraggingElement = true;
                        _draggingElement = element;
                        _elementDragStartMouse = mousePos;
                        _elementDragStartPos = element.LocalPosition;
                        _elementDragStartRotation = element.Rotation;
                        _elementDragStartScale = element.Scale;
                        evt.Use();
                        break;
                    }
                }
                Repaint();
            }
            else if (evt.type == EventType.MouseDrag && _isDraggingElement && _draggingElement != null)
            {
                Vector2 elementPos = center + new Vector2(_draggingElement.LocalPosition.x * 20, -_draggingElement.LocalPosition.y * 20);
                Vector2 delta = (mousePos - _elementDragStartMouse);

                Undo.RecordObject(_currentScaffold, "Transform Element");

                if (_elementTool == ElementTransformTool.Move)
                {
                    Vector2 moveDelta = delta / 20f;
                    _draggingElement.LocalPosition = _elementDragStartPos + new Vector3(moveDelta.x, -moveDelta.y, 0);
                }
                else if (_elementTool == ElementTransformTool.Rotate)
                {
                    Vector2 startDir = (_elementDragStartMouse - elementPos).normalized;
                    Vector2 currentDir = (mousePos - elementPos).normalized;
                    float angleDelta = Vector2.SignedAngle(startDir, currentDir);
                    _draggingElement.Rotation = _elementDragStartRotation + angleDelta;
                }
                else if (_elementTool == ElementTransformTool.Scale)
                {
                    float scaleDelta = (delta.x + delta.y) / 200f;
                    _draggingElement.Scale = _elementDragStartScale + Vector3.one * scaleDelta;
                    _draggingElement.Scale = new Vector3(
                        Mathf.Max(0.1f, _draggingElement.Scale.x),
                        Mathf.Max(0.1f, _draggingElement.Scale.y),
                        Mathf.Max(0.1f, _draggingElement.Scale.z));
                }

                EditorUtility.SetDirty(_currentScaffold);
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                _isDraggingElement = false;
                _draggingElement = null;
                evt.Use();
            }
        }

        /// <summary>
        /// Adds the selected element type to the room.
        /// </summary>
        private void AddElementToRoom()
        {
            if (_selectedRoom == null)
                return;

            Undo.RecordObject(_currentScaffold, "Add Element");

            ScaffoldElement newElement = new ScaffoldElement
            {
                ElementType = _selectedElementType,
                LocalPosition = Vector3.zero,
                Rotation = 0f,
                Scale = Vector3.one
            };
            newElement.SetDefaultGizmoShapeForType();

            _selectedRoom.AddElement(newElement);
            _selectedElement = newElement;

            EditorUtility.SetDirty(_currentScaffold);
            Repaint();
        }

        /// <summary>
        /// Removes the selected element.
        /// </summary>
        private void RemoveSelectedElement()
        {
            if (_selectedElement == null || _selectedRoom == null)
                return;

            Undo.RecordObject(_currentScaffold, "Remove Element");
            _selectedRoom.RemoveElement(_selectedElement);
            _selectedElement = null;

            EditorUtility.SetDirty(_currentScaffold);
            Repaint();
        }

        /// <summary>
        /// Gets the color for an element type.
        /// </summary>
        private Color GetElementColor(ScaffoldElementType type)
        {
            return type switch
            {
                ScaffoldElementType.Wall => new Color(0.6f, 0.6f, 0.6f, 1f),
                ScaffoldElementType.WallCorner => new Color(0.5f, 0.5f, 0.5f, 1f),
                ScaffoldElementType.CrateWooden => new Color(0.8f, 0.5f, 0.2f, 1f),
                ScaffoldElementType.CrateMetal => new Color(0.4f, 0.4f, 0.5f, 1f),
                ScaffoldElementType.Door => new Color(0.3f, 0.7f, 0.8f, 1f),
                ScaffoldElementType.Checkpoint => new Color(0.3f, 0.9f, 0.4f, 1f),
                ScaffoldElementType.PlayerSpawn => new Color(0.9f, 0.8f, 0.2f, 1f),
                ScaffoldElementType.EnemySpawn => new Color(0.9f, 0.3f, 0.3f, 1f),
                ScaffoldElementType.Hazard => new Color(0.9f, 0.2f, 0.8f, 1f),
                _ => Color.white
            };
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
                EditorGUILayout.LabelField($"Type: {room.RoomType} | Pos: {room.Position} | Elements: {room.Elements.Count}");
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
                            _selectedElement = null;
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
        /// Draws a dropdown to select RoomSO.
        /// </summary>
        private void DrawRoomSOPopup(ScaffoldRoom room)
        {
            string[] guids = AssetDatabase.FindAssets("t:RoomSO");
            List<RoomSO> roomSOList = new List<RoomSO>();
            List<string> roomSONames = new List<string>();

            roomSOList.Add(null);
            roomSONames.Add("(None)");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomSO roomSO = AssetDatabase.LoadAssetAtPath<RoomSO>(path);
                if (roomSO != null)
                {
                    roomSOList.Add(roomSO);
                    roomSONames.Add(roomSO.name);
                }
            }

            int currentIndex = roomSOList.IndexOf(room.RoomSO);
            if (currentIndex < 0) currentIndex = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Room SO", currentIndex, roomSONames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_currentScaffold, "Change Room SO");
                room.RoomSO = roomSOList[newIndex];
                EditorUtility.SetDirty(_currentScaffold);
            }
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

            DrawRoomSOPopup(room);

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

            EditorGUI.BeginChangeCheck();
            connection.DoorPosition = EditorGUILayout.Vector3Field("Door Position", connection.DoorPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_currentScaffold, "Change Door Position");
                SyncDoorPosition(room, connection);
                EditorUtility.SetDirty(_currentScaffold);
            }

            connection.DoorDirection = EditorGUILayout.Vector2Field("Door Direction", connection.DoorDirection);
            connection.IsLayerTransition = EditorGUILayout.Toggle("Is Layer Transition", connection.IsLayerTransition);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            ScaffoldElement boundDoor = FindBoundDoor(room, connection);
            if (boundDoor != null)
            {
                EditorGUILayout.HelpBox("Door placed ✓", MessageType.Info);
                if (GUILayout.Button("Remove Door", EditorStyles.miniButton))
                {
                    Undo.RecordObject(_currentScaffold, "Remove Bound Door");
                    room.RemoveElement(boundDoor);
                    EditorUtility.SetDirty(_currentScaffold);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(connection.TargetRoomID));
                if (GUILayout.Button("Place Door", GUILayout.Height(25)))
                {
                    PlaceDoorForConnection(room, connection);
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Finds the door element bound to a connection.
        /// </summary>
        private ScaffoldElement FindBoundDoor(ScaffoldRoom room, ScaffoldDoorConnection connection)
        {
            return room.Elements.FirstOrDefault(e =>
                e.ElementType == ScaffoldElementType.Door &&
                e.BoundConnectionID == connection.ConnectionID);
        }

        /// <summary>
        /// Places a door for the given connection.
        /// </summary>
        private void PlaceDoorForConnection(ScaffoldRoom room, ScaffoldDoorConnection connection)
        {
            Undo.RecordObject(_currentScaffold, "Place Door");

            ScaffoldElement newDoor = new ScaffoldElement
            {
                ElementType = ScaffoldElementType.Door,
                LocalPosition = connection.DoorPosition,
                Rotation = Vector2.SignedAngle(Vector2.right, connection.DoorDirection),
                Scale = Vector3.one,
                BoundConnectionID = connection.ConnectionID
            };
            newDoor.EnsureDoorConfigExists();

            room.AddElement(newDoor);
            EditorUtility.SetDirty(_currentScaffold);
            Repaint();
        }

        /// <summary>
        /// Syncs the door element position with the connection door position.
        /// </summary>
        private void SyncDoorPosition(ScaffoldRoom room, ScaffoldDoorConnection connection)
        {
            ScaffoldElement boundDoor = FindBoundDoor(room, connection);
            if (boundDoor != null)
            {
                boundDoor.LocalPosition = connection.DoorPosition;
            }
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
                Vector3 newPosition = _dragStartRoomPos + new Vector3(delta.x, -delta.y, 0);
                
                Vector3 snappedPosition = SnapToOtherRooms(_draggingRoom, newPosition);
                
                Undo.RecordObject(_currentScaffold, "Move Room");
                _draggingRoom.Position = snappedPosition;
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
            return type switch
            {
                RoomType.Normal => new Color(0.3f, 0.5f, 0.7f, alpha),
                RoomType.Arena => new Color(0.8f, 0.6f, 0.3f, alpha),
                RoomType.Boss => new Color(0.8f, 0.3f, 0.3f, alpha),
                RoomType.Safe => new Color(0.3f, 0.8f, 0.5f, alpha),
                _ => new Color(0.5f, 0.5f, 0.5f, alpha)
            };
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

        /// <summary>
        /// Snaps a room to other rooms' edges when close enough.
        /// </summary>
        private Vector3 SnapToOtherRooms(ScaffoldRoom draggingRoom, Vector3 desiredPosition)
        {
            Vector3 snappedPosition = desiredPosition;

            foreach (var otherRoom in _currentScaffold.Rooms)
            {
                if (otherRoom == draggingRoom) continue;

                float left = otherRoom.Position.x - otherRoom.Size.x / 2;
                float right = otherRoom.Position.x + otherRoom.Size.x / 2;
                float top = otherRoom.Position.y + otherRoom.Size.y / 2;
                float bottom = otherRoom.Position.y - otherRoom.Size.y / 2;

                float draggingLeft = desiredPosition.x - draggingRoom.Size.x / 2;
                float draggingRight = desiredPosition.x + draggingRoom.Size.x / 2;
                float draggingTop = desiredPosition.y + draggingRoom.Size.y / 2;
                float draggingBottom = desiredPosition.y - draggingRoom.Size.y / 2;

                if (Mathf.Abs(draggingRight - left) < _roomSnapThreshold)
                {
                    snappedPosition.x = left - draggingRoom.Size.x / 2;
                }
                if (Mathf.Abs(draggingLeft - right) < _roomSnapThreshold)
                {
                    snappedPosition.x = right + draggingRoom.Size.x / 2;
                }
                if (Mathf.Abs(draggingBottom - top) < _roomSnapThreshold)
                {
                    snappedPosition.y = top + draggingRoom.Size.y / 2;
                }
                if (Mathf.Abs(draggingTop - bottom) < _roomSnapThreshold)
                {
                    snappedPosition.y = bottom - draggingRoom.Size.y / 2;
                }
            }

            return snappedPosition;
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
