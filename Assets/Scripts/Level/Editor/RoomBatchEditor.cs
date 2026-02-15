#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Comprehensive batch editor for level rooms.
    /// Features:
    /// - Room list with multi-select and search
    /// - Bulk property editing
    /// - Room topology visualization
    /// - Validation checks
    /// - JSON import/export
    /// 
    /// Menu: Window > ProjectArk > Room Batch Editor
    /// </summary>
    public class RoomBatchEditor : EditorWindow
    {
        private const string WindowTitle = "Room Batch Editor";
        private const string MenuPath = "Window/ProjectArk/Room Batch Editor";

        private List<Room> _allRooms = new List<Room>();
        private List<Room> _filteredRooms = new List<Room>();
        private HashSet<Room> _selectedRooms = new HashSet<Room>();
        private string _searchText = "";
        private Vector2 _roomListScroll;
        private Vector2 _propertyScroll;
        private bool _showTopology = false;
        private Vector2 _topologyScroll;
        private float _zoomLevel = 1f;
        private Vector2 _panOffset = Vector2.zero;
        private List<ValidationIssue> _validationIssues = new List<ValidationIssue>();
        private bool _showValidation = false;

        private bool _editRoomType = false;
        private RoomType _bulkRoomType = RoomType.Normal;
        private bool _editFloorLevel = false;
        private int _bulkFloorLevel = 0;
        private bool _editRoomSO = false;
        private RoomSO _bulkRoomSO = null;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<RoomBatchEditor>(WindowTitle);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshRoomList();
        }

        private void OnFocus()
        {
            RefreshRoomList();
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRoomListPanel();
                DrawPropertyPanel();
            }

            if (_showTopology)
            {
                DrawTopologyPanel();
            }

            if (_showValidation)
            {
                DrawValidationPanel();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshRoomList();
                }

                if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
                {
                    SelectAllRooms();
                }

                if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton))
                {
                    DeselectAllRooms();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    ValidateRooms();
                }

                if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton))
                {
                    ExportToJson();
                }

                if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton))
                {
                    ImportFromJson();
                }

                _showTopology = GUILayout.Toggle(_showTopology, "Topology", EditorStyles.toolbarButton);
                _showValidation = GUILayout.Toggle(_showValidation, "Validation", EditorStyles.toolbarButton);
            }
        }

        private void DrawRoomListPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(300)))
            {
                EditorGUILayout.LabelField("Room List", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                _searchText = EditorGUILayout.TextField("Search", _searchText);
                if (EditorGUI.EndChangeCheck())
                {
                    FilterRooms();
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(_roomListScroll))
                {
                    _roomListScroll = scrollView.scrollPosition;

                    foreach (var room in _filteredRooms)
                    {
                        DrawRoomListItem(room);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Total: {_allRooms.Count} | Selected: {_selectedRooms.Count}");
            }
        }

        private void DrawRoomListItem(Room room)
        {
            bool wasSelected = _selectedRooms.Contains(room);
            bool isSelected = EditorGUILayout.ToggleLeft(
                GetRoomDisplayString(room),
                wasSelected,
                EditorStyles.label
            );

            if (isSelected != wasSelected)
            {
                if (isSelected)
                    _selectedRooms.Add(room);
                else
                    _selectedRooms.Remove(room);
            }

            var lastEvent = Event.current;
            if (lastEvent.type == EventType.MouseDown && lastEvent.button == 0 && lastEvent.clickCount == 2)
            {
                var rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(lastEvent.mousePosition))
                {
                    EditorGUIUtility.PingObject(room.gameObject);
                    Selection.activeGameObject = room.gameObject;
                    lastEvent.Use();
                }
            }
        }

        private string GetRoomDisplayString(Room room)
        {
            string roomID = room.Data != null ? room.Data.RoomID : "No RoomSO";
            string typeName = room.Data != null ? room.Data.Type.ToString() : "Unknown";
            string floorStr = room.Data != null ? $"F{room.Data.FloorLevel}" : "F?";
            return $"{room.gameObject.name} | {typeName} | {floorStr} | {roomID}";
        }

        private void DrawPropertyPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Bulk Property Editing", EditorStyles.boldLabel);

                if (_selectedRooms.Count == 0)
                {
                    EditorGUILayout.HelpBox("Please select rooms first", MessageType.Info);
                    return;
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(_propertyScroll))
                {
                    _propertyScroll = scrollView.scrollPosition;

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Selected {_selectedRooms.Count} rooms", EditorStyles.helpBox);
                    EditorGUILayout.Space();

                    _editRoomType = EditorGUILayout.Toggle("Edit Room Type", _editRoomType);
                    using (new EditorGUI.DisabledScope(!_editRoomType))
                    {
                        _bulkRoomType = (RoomType)EditorGUILayout.EnumPopup("Room Type", _bulkRoomType);
                    }

                    EditorGUILayout.Space();

                    _editFloorLevel = EditorGUILayout.Toggle("Edit Floor Level", _editFloorLevel);
                    using (new EditorGUI.DisabledScope(!_editFloorLevel))
                    {
                        _bulkFloorLevel = EditorGUILayout.IntField("Floor Level", _bulkFloorLevel);
                    }

                    EditorGUILayout.Space();

                    _editRoomSO = EditorGUILayout.Toggle("Edit RoomSO", _editRoomSO);
                    using (new EditorGUI.DisabledScope(!_editRoomSO))
                    {
                        _bulkRoomSO = (RoomSO)EditorGUILayout.ObjectField("RoomSO", _bulkRoomSO, typeof(RoomSO), false);
                    }

                    EditorGUILayout.Space(20);

                    using (new EditorGUI.DisabledScope(!_editRoomType && !_editFloorLevel && !_editRoomSO))
                    {
                        if (GUILayout.Button("Apply to Selected", GUILayout.Height(30)))
                        {
                            ApplyBulkChanges();
                        }
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

                    if (GUILayout.Button("Focus in Scene"))
                    {
                        FocusSelectedRooms();
                    }

                    if (GUILayout.Button("Select in Hierarchy"))
                    {
                        SelectInHierarchy();
                    }

                    if (GUILayout.Button("Activate Selected"))
                    {
                        SetActiveState(true);
                    }

                    if (GUILayout.Button("Deactivate Selected"))
                    {
                        SetActiveState(false);
                    }
                }
            }
        }

        private void DrawTopologyPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Room Topology", EditorStyles.boldLabel);

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_topologyScroll))
            {
                _topologyScroll = scrollView.scrollPosition;

                var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.MinHeight(400));

                if (Event.current.type == EventType.Repaint)
                {
                    DrawTopology(rect);
                }

                HandleTopologyInput(rect);
            }
        }

        private void DrawTopology(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.15f));

            var center = rect.center;
            var matrix = Matrix4x4.TRS(center + _panOffset, Quaternion.identity, Vector3.one * _zoomLevel) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

            var oldMatrix = GUI.matrix;
            GUI.matrix = matrix;

            try
            {
                DrawRoomNodes(rect);
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
        }

        private void DrawRoomNodes(Rect rect)
        {
            float nodeSize = 60f;
            float spacing = 100f;

            for (int i = 0; i < _allRooms.Count; i++)
            {
                var room = _allRooms[i];
                float x = rect.x + 50 + (i % 5) * spacing;
                float y = rect.y + 50 + (i / 5) * spacing;

                var nodeRect = new Rect(x, y, nodeSize, nodeSize);

                Color nodeColor = GetRoomTypeColor(room);
                EditorGUI.DrawRect(nodeRect, nodeColor);

                if (_selectedRooms.Contains(room))
                {
                    EditorGUI.DrawRect(new Rect(nodeRect.x - 2, nodeRect.y - 2, nodeRect.width + 4, nodeRect.height + 4), Color.yellow);
                }

                GUI.Label(new Rect(nodeRect.x, nodeRect.y + nodeSize + 2, nodeSize, 20), room.gameObject.name, EditorStyles.centeredGreyMiniLabel);
            }
        }

        private Color GetRoomTypeColor(Room room)
        {
            if (room.Data == null) return Color.gray;

            switch (room.Data.Type)
            {
                case RoomType.Normal:
                    return new Color(0.3f, 0.5f, 0.7f);
                case RoomType.Arena:
                    return new Color(0.8f, 0.4f, 0.3f);
                case RoomType.Boss:
                    return new Color(0.7f, 0.2f, 0.2f);
                case RoomType.Safe:
                    return new Color(0.3f, 0.7f, 0.4f);
                default:
                    return Color.gray;
            }
        }

        private void HandleTopologyInput(Rect rect)
        {
            var evt = Event.current;

            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                _zoomLevel *= evt.delta.y > 0 ? 0.9f : 1.1f;
                _zoomLevel = Mathf.Clamp(_zoomLevel, 0.3f, 3f);
                evt.Use();
                Repaint();
            }

            if (evt.type == EventType.MouseDrag && evt.button == 1)
            {
                _panOffset += evt.delta;
                evt.Use();
                Repaint();
            }
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

            if (_validationIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("Click \"Validate\" button to start checking", MessageType.Info);
                return;
            }

            foreach (var issue in _validationIssues)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent icon;
                    switch (issue.Severity)
                    {
                        case ValidationIssueSeverity.Error:
                            icon = EditorGUIUtility.IconContent("console.erroricon");
                            break;
                        case ValidationIssueSeverity.Warning:
                            icon = EditorGUIUtility.IconContent("console.warnicon");
                            break;
                        default:
                            icon = EditorGUIUtility.IconContent("console.infoicon");
                            break;
                    }

                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    EditorGUILayout.LabelField(issue.Message);

                    if (issue.Room != null && GUILayout.Button("Locate", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(issue.Room.gameObject);
                        Selection.activeGameObject = issue.Room.gameObject;
                    }
                }
            }
        }

        private void RefreshRoomList()
        {
            _allRooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            _filteredRooms = new List<Room>(_allRooms);
            FilterRooms();
            Repaint();
        }

        private void FilterRooms()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredRooms = new List<Room>(_allRooms);
            }
            else
            {
                string searchLower = _searchText.ToLower();
                _filteredRooms = _allRooms.Where(r =>
                    r.gameObject.name.ToLower().Contains(searchLower) ||
                    (r.Data != null && r.Data.RoomID.ToLower().Contains(searchLower))
                ).ToList();
            }
            Repaint();
        }

        private void SelectAllRooms()
        {
            _selectedRooms = new HashSet<Room>(_filteredRooms);
            Repaint();
        }

        private void DeselectAllRooms()
        {
            _selectedRooms.Clear();
            Repaint();
        }

        private void ApplyBulkChanges()
        {
            if (_selectedRooms.Count == 0) return;

            Undo.RecordObjects(_selectedRooms.Select(r => r.gameObject).ToArray(), "Bulk Room Edit");

            foreach (var room in _selectedRooms)
            {
                var serializedRoom = new SerializedObject(room);

                if (_editRoomSO && _bulkRoomSO != null)
                {
                    serializedRoom.FindProperty("_data").objectReferenceValue = _bulkRoomSO;
                }

                serializedRoom.ApplyModifiedProperties();

                if (room.Data != null)
                {
                    var serializedSO = new SerializedObject(room.Data);

                    if (_editRoomType)
                    {
                        serializedSO.FindProperty("_type").enumValueIndex = (int)_bulkRoomType;
                    }

                    if (_editFloorLevel)
                    {
                        serializedSO.FindProperty("_floorLevel").intValue = _bulkFloorLevel;
                    }

                    serializedSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(room.Data);
                }

                EditorUtility.SetDirty(room);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RoomBatchEditor] Applied bulk changes to {_selectedRooms.Count} rooms");
        }

        private void FocusSelectedRooms()
        {
            if (_selectedRooms.Count == 0) return;

            var bounds = new Bounds(_selectedRooms.First().transform.position, Vector3.zero);
            foreach (var room in _selectedRooms)
            {
                bounds.Encapsulate(room.transform.position);
            }

            SceneView.lastActiveSceneView.Frame(bounds);
        }

        private void SelectInHierarchy()
        {
            Selection.objects = _selectedRooms.Select(r => r.gameObject).ToArray();
            Selection.activeGameObject = _selectedRooms.First().gameObject;
        }

        private void SetActiveState(bool active)
        {
            Undo.RecordObjects(_selectedRooms.Select(r => r.gameObject).ToArray(), active ? "Activate Rooms" : "Deactivate Rooms");
            foreach (var room in _selectedRooms)
            {
                room.gameObject.SetActive(active);
            }
        }

        private void ValidateRooms()
        {
            _validationIssues.Clear();

            foreach (var room in _allRooms)
            {
                if (room.Data == null)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Room = room,
                        Severity = ValidationIssueSeverity.Error,
                        Message = $"{room.gameObject.name}: Missing RoomSO reference"
                    });
                }

                var boxCollider = room.GetComponent<BoxCollider2D>();
                if (boxCollider != null && !boxCollider.isTrigger)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Room = room,
                        Severity = ValidationIssueSeverity.Warning,
                        Message = $"{room.gameObject.name}: BoxCollider2D is not Trigger"
                    });
                }

                var confinerChild = room.transform.Find("CameraConfiner");
                if (confinerChild != null && confinerChild.gameObject.layer != 2)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Room = room,
                        Severity = ValidationIssueSeverity.Warning,
                        Message = $"{room.gameObject.name}: CameraConfiner not on Ignore Raycast layer"
                    });
                }
            }

            _showValidation = true;
            Repaint();

            Debug.Log($"[RoomBatchEditor] Validation complete: {_validationIssues.Count} issues found");
        }

        private void ExportToJson()
        {
            var path = EditorUtility.SaveFilePanel("Export Room Data", Application.dataPath, "room_data.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = new RoomExportData
            {
                Rooms = _allRooms.Select(r => new RoomData
                {
                    Name = r.gameObject.name,
                    RoomID = r.Data?.RoomID ?? "",
                    Type = r.Data?.Type ?? RoomType.Normal,
                    FloorLevel = r.Data?.FloorLevel ?? 0,
                    Position = r.transform.position,
                    HasEncounter = r.Data?.HasEncounter ?? false
                }).ToList()
            };

            var json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);

            Debug.Log($"[RoomBatchEditor] Exported room data to {path}");
            AssetDatabase.Refresh();
        }

        private void ImportFromJson()
        {
            var path = EditorUtility.OpenFilePanel("Import Room Data", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;

            var json = System.IO.File.ReadAllText(path);
            var data = JsonUtility.FromJson<RoomExportData>(json);

            if (data == null || data.Rooms == null)
            {
                Debug.LogError("[RoomBatchEditor] Failed to parse JSON");
                return;
            }

            EditorUtility.DisplayDialog(
                "Import Complete",
                $"Loaded {data.Rooms.Count} rooms from JSON.\n\nNote: This is a data-only import. To create actual room GameObjects, use the Scaffold tool.",
                "OK"
            );

            Debug.Log($"[RoomBatchEditor] Imported {data.Rooms.Count} rooms from {path}");
        }

        [Serializable]
        private class RoomExportData
        {
            public List<RoomData> Rooms;
        }

        [Serializable]
        private class RoomData
        {
            public string Name;
            public string RoomID;
            public RoomType Type;
            public int FloorLevel;
            public Vector3 Position;
            public bool HasEncounter;
        }

        private class ValidationIssue
        {
            public Room Room;
            public ValidationIssueSeverity Severity;
            public string Message;
        }

        private enum ValidationIssueSeverity
        {
            Info,
            Warning,
            Error
        }
    }
}
#endif
