using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Handles the Blockout Mode tools: rectangle brush, corridor brush, chain drawing.
    /// Provides rapid room placement via drag-to-draw in SceneView.
    /// </summary>
    public static class BlockoutModeHandler
    {
        // ──────────────────── Brush Types ────────────────────

        public enum BrushType
        {
            Rectangle,
            Corridor
        }

        // ──────────────────── State ────────────────────

        private static BrushType _activeBrush = BrushType.Rectangle;
        private static bool _isDrawing;
        private static Vector2 _drawStartWorld;
        private static Vector2 _drawEndWorld;
        private static bool _isChainDraw; // Shift held = chain from existing room
        private static Room _chainSourceRoom;

        private const float CORRIDOR_WIDTH = 3f;
        private const float MIN_ROOM_SIZE = 3f;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Active brush type. </summary>
        public static BrushType ActiveBrush => _activeBrush;

        /// <summary> Whether currently drawing. </summary>
        public static bool IsDrawing => _isDrawing;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Set the active brush type.
        /// </summary>
        public static void SetBrush(BrushType brush)
        {
            _activeBrush = brush;
        }

        /// <summary>
        /// Handle Blockout Mode input in SceneView.
        /// </summary>
        public static void HandleBlockoutInput(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null) return;

            // Draw brush toolbar
            DrawBrushToolbar(sceneView);

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        _isDrawing = true;
                        _drawStartWorld = worldPos;
                        _drawEndWorld = worldPos;

                        // Check for chain draw (Shift + click from existing room edge)
                        _isChainDraw = e.shift;
                        _chainSourceRoom = null;

                        if (_isChainDraw)
                        {
                            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
                            _chainSourceRoom = GetNearestRoomEdge(worldPos, rooms, out _drawStartWorld);
                        }

                        GUIUtility.hotControl = controlID;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDrawing)
                    {
                        _drawEndWorld = worldPos;
                        GUIUtility.hotControl = controlID;
                        e.Use();
                        sceneView.Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDrawing && e.button == 0)
                    {
                        _isDrawing = false;

                        Rect drawRect = CalculateDrawRect();

                        if (drawRect.width >= MIN_ROOM_SIZE && drawRect.height >= MIN_ROOM_SIZE)
                        {
                            CreateRoomFromDraw(drawRect);
                        }

                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape && _isDrawing)
                    {
                        _isDrawing = false;
                        e.Use();
                    }
                    break;
            }

            // Draw preview
            if (_isDrawing)
            {
                DrawPreviewRect();
            }

            // Draw crosshair cursor when not drawing
            if (!_isDrawing)
            {
                DrawCrosshairCursor(worldPos);
            }
        }

        /// <summary>
        /// Ensure required managers exist and enter Play Mode.
        /// </summary>
        public static void QuickPlay()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Check for required managers
            var roomManager = Object.FindAnyObjectByType<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogWarning("[LevelArchitect] QuickPlay: No RoomManager found. Creating temporary one.");
                var go = new GameObject("_QuickPlay_RoomManager");
                go.AddComponent<RoomManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create QuickPlay RoomManager");
            }

            var doorTransition = Object.FindAnyObjectByType<DoorTransitionController>();
            if (doorTransition == null)
            {
                Debug.LogWarning("[LevelArchitect] QuickPlay: No DoorTransitionController found. Creating temporary one.");
                var go = new GameObject("_QuickPlay_DoorTransition");
                go.AddComponent<DoorTransitionController>();
                Undo.RegisterCreatedObjectUndo(go, "Create QuickPlay DoorTransition");
            }

            EditorApplication.EnterPlaymode();
        }

        // ──────────────────── Brush Toolbar ────────────────────

        private static void DrawBrushToolbar(SceneView sceneView)
        {
            Handles.BeginGUI();

            float toolbarWidth = 240f;
            float toolbarX = (sceneView.position.width - toolbarWidth) / 2f;
            float toolbarY = 44f; // Below the main toolbar

            var toolbarRect = new Rect(toolbarX, toolbarY, toolbarWidth, 28f);
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

            GUILayout.BeginArea(new Rect(toolbarRect.x + 4, toolbarRect.y + 2, toolbarRect.width - 8, toolbarRect.height - 4));
            GUILayout.BeginHorizontal();

            DrawBrushButton("▭ Room", BrushType.Rectangle);
            DrawBrushButton("═ Corridor", BrushType.Corridor);

            GUILayout.FlexibleSpace();

            var tipStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            GUILayout.Label("Shift = Chain", tipStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private static void DrawBrushButton(string label, BrushType brush)
        {
            var prevBg = GUI.backgroundColor;
            if (_activeBrush == brush)
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f);

            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _activeBrush = brush;
            }

            GUI.backgroundColor = prevBg;
        }

        // ──────────────────── Drawing Logic ────────────────────

        private static Rect CalculateDrawRect()
        {
            Vector2 start = _drawStartWorld;
            Vector2 end = _drawEndWorld;

            if (_activeBrush == BrushType.Corridor)
            {
                // Determine if horizontal or vertical corridor
                float dx = Mathf.Abs(end.x - start.x);
                float dy = Mathf.Abs(end.y - start.y);

                if (dx > dy)
                {
                    // Horizontal corridor
                    float xMin = Mathf.Min(start.x, end.x);
                    float xMax = Mathf.Max(start.x, end.x);
                    float yCenter = (start.y + end.y) / 2f;
                    return new Rect(xMin, yCenter - CORRIDOR_WIDTH / 2f, xMax - xMin, CORRIDOR_WIDTH);
                }
                else
                {
                    // Vertical corridor
                    float yMin = Mathf.Min(start.y, end.y);
                    float yMax = Mathf.Max(start.y, end.y);
                    float xCenter = (start.x + end.x) / 2f;
                    return new Rect(xCenter - CORRIDOR_WIDTH / 2f, yMin, CORRIDOR_WIDTH, yMax - yMin);
                }
            }

            // Rectangle brush
            float rXMin = Mathf.Min(start.x, end.x);
            float rYMin = Mathf.Min(start.y, end.y);
            float rWidth = Mathf.Abs(end.x - start.x);
            float rHeight = Mathf.Abs(end.y - start.y);

            return new Rect(rXMin, rYMin, rWidth, rHeight);
        }

        private static void CreateRoomFromDraw(Rect drawRect)
        {
            // Find or create a default preset based on brush type
            RoomPresetSO preset = FindDefaultPreset();

            Vector3 centerPos = new Vector3(drawRect.center.x, drawRect.center.y, 0);
            Vector2 size = new Vector2(drawRect.width, drawRect.height);

            Room newRoom = RoomFactory.CreateRoomFromPreset(preset, centerPos, size);

            if (newRoom != null)
            {
                // Auto-connect with adjacent rooms
                int connections = DoorWiringService.AutoConnectAllAdjacent(newRoom);

                // Chain draw: auto-connect with source room
                if (_isChainDraw && _chainSourceRoom != null && !DoorWiringService.AreRoomsConnected(newRoom, _chainSourceRoom))
                {
                    DoorWiringService.AutoConnectRooms(newRoom, _chainSourceRoom);
                    connections++;
                }

                if (connections > 0)
                {
                    Debug.Log($"[BlockoutMode] Auto-connected {connections} adjacent room(s).");
                }
            }

            _chainSourceRoom = null;
        }

        private static RoomPresetSO FindDefaultPreset()
        {
            // Try to find an existing Normal preset
            var presets = RoomFactory.FindAllPresets();
            foreach (var p in presets)
            {
                if (p != null && p.RoomTypeValue == RoomType.Normal)
                    return p;
            }

            // If no presets exist, create one on the fly
            if (presets.Length == 0)
            {
                return CreateBuiltInPresets();
            }

            return presets[0];
        }

        /// <summary>
        /// Create the 5 built-in presets if they don't exist.
        /// Returns the Normal preset.
        /// </summary>
        public static RoomPresetSO CreateBuiltInPresets()
        {
            string path = "Assets/_Data/Level/RoomPresets/";
            EnsureDirectoryExists(path);

            RoomPresetSO normalPreset = null;

            // Safe room
            normalPreset = CreatePresetIfMissing(path, "Preset_Safe", "Safe Room",
                "A safe zone with no enemies. May contain checkpoint, shop, or NPC.",
                RoomType.Safe, new Vector2(15, 12), 2, false, false);

            // Normal room
            var normal = CreatePresetIfMissing(path, "Preset_Normal", "Normal Room",
                "Standard room with optional enemies.",
                RoomType.Normal, new Vector2(20, 15), 4, false, false);
            if (normal != null) normalPreset = normal;

            // Arena room
            CreatePresetIfMissing(path, "Preset_Arena", "Arena Room",
                "Combat arena — doors lock on entry, unlock after all waves cleared.",
                RoomType.Arena, new Vector2(25, 20), 6, true, true);

            // Boss room
            CreatePresetIfMissing(path, "Preset_Boss", "Boss Room",
                "Boss encounter room — larger than arena, special rewards on clear.",
                RoomType.Boss, new Vector2(35, 25), 6, true, true);

            // Corridor
            CreatePresetIfMissing(path, "Preset_Corridor", "Corridor",
                "Narrow connecting passage between rooms.",
                RoomType.Normal, new Vector2(15, 3), 0, false, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return normalPreset;
        }

        private static RoomPresetSO CreatePresetIfMissing(string basePath, string fileName, string presetName,
            string description, RoomType roomType, Vector2 defaultSize, int spawnPoints,
            bool includeArena, bool includeSpawner)
        {
            string fullPath = $"{basePath}{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoomPresetSO>(fullPath);
            if (existing != null) return existing;

            var preset = ScriptableObject.CreateInstance<RoomPresetSO>();

            var serialized = new SerializedObject(preset);
            serialized.FindProperty("_presetName").stringValue = presetName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_roomType").enumValueIndex = (int)roomType;
            serialized.FindProperty("_defaultSize").vector2Value = defaultSize;
            serialized.FindProperty("_spawnPointCount").intValue = spawnPoints;
            serialized.FindProperty("_includeArenaController").boolValue = includeArena;
            serialized.FindProperty("_includeEnemySpawner").boolValue = includeSpawner;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(preset, fullPath);
            Debug.Log($"[BlockoutMode] Created built-in preset: {fullPath}");

            return preset;
        }

        // ──────────────────── Preview Drawing ────────────────────

        private static void DrawPreviewRect()
        {
            Rect rect = CalculateDrawRect();

            Color previewFill = new Color(0.3f, 0.7f, 0.3f, 0.15f);
            Color previewOutline = new Color(0.3f, 0.9f, 0.3f, 0.8f);

            Vector3[] corners = new Vector3[]
            {
                new Vector3(rect.xMin, rect.yMin, 0),
                new Vector3(rect.xMax, rect.yMin, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.xMin, rect.yMax, 0)
            };

            Handles.DrawSolidRectangleWithOutline(corners, previewFill, previewOutline);

            // Size label
            string sizeLabel = $"{rect.width:F1} × {rect.height:F1}";
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.9f, 0.3f, 1f) }
            };
            Handles.Label(new Vector3(rect.center.x, rect.yMax + 1.5f, 0), sizeLabel, style);

            // Chain indicator
            if (_isChainDraw && _chainSourceRoom != null)
            {
                Handles.color = new Color(0.9f, 0.8f, 0.2f, 0.7f);
                Handles.DrawAAPolyLine(2f, _chainSourceRoom.transform.position, new Vector3(rect.center.x, rect.center.y, 0));
                Handles.color = Color.white;
            }
        }

        private static void DrawCrosshairCursor(Vector2 worldPos)
        {
            float size = 1f;
            Handles.color = new Color(0.3f, 0.9f, 0.3f, 0.5f);
            Handles.DrawLine(new Vector3(worldPos.x - size, worldPos.y, 0), new Vector3(worldPos.x + size, worldPos.y, 0));
            Handles.DrawLine(new Vector3(worldPos.x, worldPos.y - size, 0), new Vector3(worldPos.x, worldPos.y + size, 0));
            Handles.color = Color.white;
        }

        // ──────────────────── Utility ────────────────────

        private static Room GetNearestRoomEdge(Vector2 worldPos, Room[] rooms, out Vector2 snappedEdgePoint)
        {
            snappedEdgePoint = worldPos;
            Room nearestRoom = null;
            float nearestDist = float.MaxValue;

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var box = room.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                Rect rect = LevelArchitectWindow.GetRoomWorldRect(room, box);

                // Check each edge
                Vector2[] edgePoints = new Vector2[]
                {
                    new Vector2(rect.xMin, Mathf.Clamp(worldPos.y, rect.yMin, rect.yMax)), // left
                    new Vector2(rect.xMax, Mathf.Clamp(worldPos.y, rect.yMin, rect.yMax)), // right
                    new Vector2(Mathf.Clamp(worldPos.x, rect.xMin, rect.xMax), rect.yMin), // bottom
                    new Vector2(Mathf.Clamp(worldPos.x, rect.xMin, rect.xMax), rect.yMax)  // top
                };

                foreach (var ep in edgePoints)
                {
                    float dist = Vector2.Distance(worldPos, ep);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestRoom = room;
                        snappedEdgePoint = ep;
                    }
                }
            }

            return nearestRoom;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                var parts = path.TrimEnd('/').Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
