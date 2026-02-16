using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Handles rendering room blockout rectangles in SceneView and all room interaction
    /// (selection, dragging, snapping, box-selection).
    /// Driven by LevelArchitectWindow's SceneView callback.
    /// </summary>
    public static class RoomBlockoutRenderer
    {
        // ──────────────────── Constants ────────────────────

        private const float SNAP_THRESHOLD = 0.5f;
        private const float SELECTION_OUTLINE_WIDTH = 3f;
        private const float HOVER_OUTLINE_WIDTH = 2f;
        private const float LABEL_OFFSET_Y = 1.2f;
        private const float DOOR_ICON_SIZE = 0.8f;

        // ──────────────────── Drag State ────────────────────

        private static bool _isDragging;
        private static Vector2 _dragStartWorldPos;
        private static Dictionary<Room, Vector3> _dragStartPositions = new Dictionary<Room, Vector3>();
        private static Vector2 _snapOffset;
        private static bool _isSnapping;

        // ──────────────────── Box Select State ────────────────────

        private static bool _isBoxSelecting;
        private static Vector2 _boxSelectStartScreen;
        private static Vector2 _boxSelectEndScreen;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Main render call — draw all room blockouts and handle interaction.
        /// Called from LevelArchitectWindow.OnSceneGUI().
        /// </summary>
        public static void DrawAndInteract(SceneView sceneView)
        {
            var window = LevelArchitectWindow.Instance;
            if (window == null || !window.IsActive) return;

            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            if (rooms.Length == 0) return;

            // Draw all room blockouts
            foreach (var room in rooms)
            {
                if (room == null) continue;
                DrawRoomBlockout(room, window);
            }

            // Draw door connections between rooms
            DrawDoorConnections(rooms);

            // Draw pacing/critical path/lock-key overlays
            PacingOverlayRenderer.DrawOverlays(rooms);

            // Handle interaction (only in Select mode)
            if (window.CurrentMode == LevelArchitectWindow.ToolMode.Select)
            {
                HandleSelectModeInput(sceneView, rooms, window);
            }

            // Draw box selection rectangle
            if (_isBoxSelecting)
            {
                DrawBoxSelectionRect();
            }

            // Draw hover tooltip
            if (window.HoveredRoom != null && !_isDragging)
            {
                DrawRoomTooltip(window.HoveredRoom, sceneView);
            }
        }

        // ──────────────────── Room Rendering ────────────────────

        private static void DrawRoomBlockout(Room room, LevelArchitectWindow window)
        {
            var box = room.GetComponent<BoxCollider2D>();
            if (box == null) return;

            Rect worldRect = LevelArchitectWindow.GetRoomWorldRect(room, box);
            RoomType type = room.Type;

            // Floor level filter — dim non-active floors
            float alphaMultiplier = 1f;
            if (window.ActiveFloorLevel != int.MinValue)
            {
                int roomFloor = room.Data != null ? room.Data.FloorLevel : 0;
                if (roomFloor != window.ActiveFloorLevel)
                {
                    alphaMultiplier = 0.15f;
                }
            }

            // ── Fill ──
            Color fillColor = LevelArchitectWindow.GetRoomTypeColor(type);
            fillColor.a *= alphaMultiplier;

            Color outlineColor = LevelArchitectWindow.GetRoomTypeOutlineColor(type);
            outlineColor.a *= alphaMultiplier;

            Vector3[] corners = RectToCorners(worldRect);

            Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);

            // ── Selection highlight ──
            bool isSelected = window.SelectedRooms.Contains(room);
            if (isSelected)
            {
                Color selectionColor = Color.white;
                selectionColor.a = alphaMultiplier;
                DrawThickOutline(corners, selectionColor, SELECTION_OUTLINE_WIDTH);
            }

            // ── Hover highlight ──
            if (room == window.HoveredRoom && !isSelected)
            {
                Color hoverColor = new Color(1f, 1f, 1f, 0.5f * alphaMultiplier);
                DrawThickOutline(corners, hoverColor, HOVER_OUTLINE_WIDTH);
            }

            // ── Snap guide lines ──
            if (_isSnapping && isSelected && _isDragging)
            {
                DrawSnapGuides(worldRect);
            }

            // ── Label ──
            DrawRoomLabel(room, worldRect, alphaMultiplier);

            // ── Door icons ──
            DrawDoorIcons(room, alphaMultiplier);

            // ── Fatal validation overlay ──
            if (LevelValidator.FatalRoomIDs.Contains(room.GetInstanceID()))
            {
                DrawFatalWarning(worldRect);
            }
        }

        private static void DrawRoomLabel(Room room, Rect worldRect, float alpha)
        {
            Vector3 labelPos = new Vector3(
                worldRect.center.x,
                worldRect.yMax + LABEL_OFFSET_Y,
                0
            );

            string label = room.RoomID;
            string typeTag = room.Type.ToString().Substring(0, 1); // N/A/B/S

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };

            Handles.Label(labelPos, $"[{typeTag}] {label}", style);
        }

        private static void DrawDoorIcons(Room room, float alpha)
        {
            var doors = room.GetComponentsInChildren<Door>(true);
            foreach (var door in doors)
            {
                if (door == null) continue;

                Vector3 pos = door.transform.position;
                float size = DOOR_ICON_SIZE;

                Color doorColor;
                switch (door.CurrentState)
                {
                    case DoorState.Open:
                        doorColor = new Color(0.2f, 0.9f, 0.2f, alpha);
                        break;
                    case DoorState.Locked_Key:
                        doorColor = new Color(0.9f, 0.8f, 0.1f, alpha);
                        break;
                    case DoorState.Locked_Combat:
                        doorColor = new Color(0.9f, 0.3f, 0.1f, alpha);
                        break;
                    default:
                        doorColor = new Color(0.7f, 0.7f, 0.7f, alpha);
                        break;
                }

                Handles.color = doorColor;
                Handles.DrawSolidDisc(pos, Vector3.forward, size * 0.5f);
                Handles.color = Color.white;
            }
        }

        // ──────────────────── Fatal Warning Overlay ────────────────────

        private static void DrawFatalWarning(Rect worldRect)
        {
            // Red exclamation mark at top-right corner
            Vector3 iconPos = new Vector3(worldRect.xMax - 1f, worldRect.yMax - 1f, 0);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(1f, 0.2f, 0.2f, 1f) }
            };

            Handles.Label(iconPos, "⚠", style);
        }

        // ──────────────────── Door Connection Lines ────────────────────

        private static void DrawDoorConnections(Room[] rooms)
        {
            var drawnPairs = new HashSet<string>();

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    // Avoid drawing duplicate lines
                    string pairKey = GetPairKey(room, door.TargetRoom);
                    if (drawnPairs.Contains(pairKey)) continue;
                    drawnPairs.Add(pairKey);

                    // Draw connection line
                    Color lineColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);
                    if (door.IsLayerTransition)
                        lineColor = new Color(0.6f, 0.3f, 0.9f, 0.6f);

                    Handles.color = lineColor;
                    Handles.DrawDottedLine(
                        room.transform.position,
                        door.TargetRoom.transform.position,
                        4f
                    );
                    Handles.color = Color.white;
                }
            }
        }

        private static string GetPairKey(Room a, Room b)
        {
            string idA = a.GetInstanceID().ToString();
            string idB = b.GetInstanceID().ToString();
            return string.Compare(idA, idB) < 0 ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }

        // ──────────────────── Select Mode Input ────────────────────

        private static void HandleSelectModeInput(SceneView sceneView, Room[] rooms, LevelArchitectWindow window)
        {
            Event e = Event.current;
            if (e == null) return;

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        HandleMouseDown(e, rooms, window, controlID);
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        HandleMouseDrag(e, window, controlID);
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        HandleMouseUp(e, rooms, window, controlID);
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        window.ClearSelection();
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                    {
                        DeleteSelectedRooms(window);
                        e.Use();
                    }
                    break;
            }
        }

        private static void HandleMouseDown(Event e, Room[] rooms, LevelArchitectWindow window, int controlID)
        {
            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            Room hitRoom = GetRoomAtPosition(worldPos, rooms);

            if (hitRoom != null)
            {
                // Click on a room
                if (e.shift)
                {
                    // Shift+click: toggle selection
                    if (window.SelectedRooms.Contains(hitRoom))
                        window.DeselectRoom(hitRoom);
                    else
                        window.SelectRoom(hitRoom, true);
                }
                else if (!window.SelectedRooms.Contains(hitRoom))
                {
                    // Click on unselected room: select it
                    window.SelectRoom(hitRoom, false);
                }

                // Start drag
                _isDragging = true;
                _isSnapping = false;
                _dragStartWorldPos = worldPos;
                _dragStartPositions.Clear();

                foreach (var room in window.SelectedRooms)
                {
                    _dragStartPositions[room] = room.transform.position;
                }

                GUIUtility.hotControl = controlID;
                e.Use();
            }
            else
            {
                // Click on empty space: start box selection
                if (!e.shift)
                {
                    window.ClearSelection();
                }

                _isBoxSelecting = true;
                _boxSelectStartScreen = e.mousePosition;
                _boxSelectEndScreen = e.mousePosition;

                GUIUtility.hotControl = controlID;
                e.Use();
            }
        }

        private static void HandleMouseDrag(Event e, LevelArchitectWindow window, int controlID)
        {
            if (_isDragging && window.SelectedRooms.Count > 0)
            {
                Vector2 currentWorldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
                Vector2 delta = currentWorldPos - _dragStartWorldPos;

                // Record undo for all selected rooms
                var roomObjects = new Object[window.SelectedRooms.Count];
                for (int i = 0; i < window.SelectedRooms.Count; i++)
                    roomObjects[i] = window.SelectedRooms[i].transform;
                Undo.RecordObjects(roomObjects, "Move Rooms");

                // Apply delta + snapping
                _isSnapping = false;
                foreach (var room in window.SelectedRooms)
                {
                    if (!_dragStartPositions.TryGetValue(room, out var startPos)) continue;

                    Vector3 newPos = startPos + (Vector3)delta;

                    // Snap to other rooms
                    var snapResult = CalculateSnap(room, newPos, window);
                    if (snapResult.snapped)
                    {
                        newPos += (Vector3)snapResult.offset;
                        _isSnapping = true;
                    }

                    room.transform.position = newPos;
                }

                GUIUtility.hotControl = controlID;
                e.Use();
            }
            else if (_isBoxSelecting)
            {
                _boxSelectEndScreen = e.mousePosition;
                GUIUtility.hotControl = controlID;
                e.Use();
            }
        }

        private static void HandleMouseUp(Event e, Room[] rooms, LevelArchitectWindow window, int controlID)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _isSnapping = false;
                _dragStartPositions.Clear();

                GUIUtility.hotControl = 0;
                e.Use();
            }
            else if (_isBoxSelecting)
            {
                // Finalize box selection
                _isBoxSelecting = false;

                Rect screenRect = GetNormalizedRect(_boxSelectStartScreen, _boxSelectEndScreen);

                // Only process if the rect is big enough (prevent accidental clicks)
                if (screenRect.width > 5 && screenRect.height > 5)
                {
                    foreach (var room in rooms)
                    {
                        if (room == null) continue;

                        Vector2 screenPos = HandleUtility.WorldToGUIPoint(room.transform.position);
                        if (screenRect.Contains(screenPos))
                        {
                            window.SelectRoom(room, true);
                        }
                    }
                }

                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        // ──────────────────── Snapping ────────────────────

        private struct SnapResult
        {
            public bool snapped;
            public Vector2 offset;
        }

        private static SnapResult CalculateSnap(Room movingRoom, Vector3 proposedPos, LevelArchitectWindow window)
        {
            var box = movingRoom.GetComponent<BoxCollider2D>();
            if (box == null) return new SnapResult { snapped = false };

            Vector2 center = (Vector2)proposedPos + box.offset;
            Vector2 halfSize = box.size / 2f;

            float movingLeft = center.x - halfSize.x;
            float movingRight = center.x + halfSize.x;
            float movingBottom = center.y - halfSize.y;
            float movingTop = center.y + halfSize.y;

            float bestXOffset = float.MaxValue;
            float bestYOffset = float.MaxValue;
            bool snappedX = false;
            bool snappedY = false;

            var allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

            foreach (var other in allRooms)
            {
                if (other == null || other == movingRoom) continue;
                if (window.SelectedRooms.Contains(other)) continue;

                var otherBox = other.GetComponent<BoxCollider2D>();
                if (otherBox == null) continue;

                Rect otherRect = LevelArchitectWindow.GetRoomWorldRect(other, otherBox);

                // Check X-axis snapping (left/right edges)
                TrySnapEdge(movingRight, otherRect.xMin, SNAP_THRESHOLD, ref bestXOffset, ref snappedX); // right → left
                TrySnapEdge(movingLeft, otherRect.xMax, SNAP_THRESHOLD, ref bestXOffset, ref snappedX);  // left → right
                TrySnapEdge(movingLeft, otherRect.xMin, SNAP_THRESHOLD, ref bestXOffset, ref snappedX);  // left → left
                TrySnapEdge(movingRight, otherRect.xMax, SNAP_THRESHOLD, ref bestXOffset, ref snappedX); // right → right

                // Check Y-axis snapping (top/bottom edges)
                TrySnapEdge(movingTop, otherRect.yMin, SNAP_THRESHOLD, ref bestYOffset, ref snappedY);    // top → bottom
                TrySnapEdge(movingBottom, otherRect.yMax, SNAP_THRESHOLD, ref bestYOffset, ref snappedY); // bottom → top
                TrySnapEdge(movingBottom, otherRect.yMin, SNAP_THRESHOLD, ref bestYOffset, ref snappedY); // bottom → bottom
                TrySnapEdge(movingTop, otherRect.yMax, SNAP_THRESHOLD, ref bestYOffset, ref snappedY);    // top → top
            }

            Vector2 offset = Vector2.zero;
            if (snappedX) offset.x = bestXOffset;
            if (snappedY) offset.y = bestYOffset;

            return new SnapResult
            {
                snapped = snappedX || snappedY,
                offset = offset
            };
        }

        private static void TrySnapEdge(float movingEdge, float targetEdge, float threshold,
            ref float bestOffset, ref bool snapped)
        {
            float diff = targetEdge - movingEdge;
            if (Mathf.Abs(diff) < threshold && Mathf.Abs(diff) < Mathf.Abs(bestOffset))
            {
                bestOffset = diff;
                snapped = true;
            }
        }

        // ──────────────────── Snap Guides ────────────────────

        private static void DrawSnapGuides(Rect worldRect)
        {
            Color guideColor = new Color(0f, 1f, 0.5f, 0.5f);
            Handles.color = guideColor;

            float ext = 20f;

            // Horizontal guides at top and bottom
            Handles.DrawLine(
                new Vector3(worldRect.xMin - ext, worldRect.yMin, 0),
                new Vector3(worldRect.xMax + ext, worldRect.yMin, 0)
            );
            Handles.DrawLine(
                new Vector3(worldRect.xMin - ext, worldRect.yMax, 0),
                new Vector3(worldRect.xMax + ext, worldRect.yMax, 0)
            );

            // Vertical guides at left and right
            Handles.DrawLine(
                new Vector3(worldRect.xMin, worldRect.yMin - ext, 0),
                new Vector3(worldRect.xMin, worldRect.yMax + ext, 0)
            );
            Handles.DrawLine(
                new Vector3(worldRect.xMax, worldRect.yMin - ext, 0),
                new Vector3(worldRect.xMax, worldRect.yMax + ext, 0)
            );

            Handles.color = Color.white;
        }

        // ──────────────────── Box Selection ────────────────────

        private static void DrawBoxSelectionRect()
        {
            Handles.BeginGUI();

            Rect screenRect = GetNormalizedRect(_boxSelectStartScreen, _boxSelectEndScreen);

            // Fill
            EditorGUI.DrawRect(screenRect, new Color(0.3f, 0.6f, 1f, 0.15f));

            // Border
            Color borderColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.y, screenRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.yMax - 1, screenRect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(screenRect.x, screenRect.y, 1, screenRect.height), borderColor);
            EditorGUI.DrawRect(new Rect(screenRect.xMax - 1, screenRect.y, 1, screenRect.height), borderColor);

            Handles.EndGUI();
        }

        // ──────────────────── Room Tooltip ────────────────────

        private static void DrawRoomTooltip(Room room, SceneView sceneView)
        {
            Handles.BeginGUI();

            Vector2 screenPos = HandleUtility.WorldToGUIPoint(room.transform.position);
            float tooltipWidth = 200f;
            float tooltipX = screenPos.x + 20f;
            float tooltipY = screenPos.y - 10f;

            // Build tooltip text
            var lines = new List<string>();
            lines.Add($"<b>{room.RoomID}</b>");
            lines.Add($"Type: {room.Type}");

            if (room.Data != null)
            {
                lines.Add($"Floor: {room.Data.FloorLevel}");
                if (room.Data.Encounter != null)
                {
                    lines.Add($"Encounter: {room.Data.Encounter.name}");
                    lines.Add($"  Waves: {room.Data.Encounter.WaveCount}");
                }
            }

            var box = room.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                lines.Add($"Size: {box.size.x:F0}×{box.size.y:F0}");
            }

            var doors = room.GetComponentsInChildren<Door>(true);
            lines.Add($"Doors: {doors.Length}");

            string tooltipText = string.Join("\n", lines);

            float lineHeight = 16f;
            float tooltipHeight = lines.Count * lineHeight + 12f;

            // Clamp to screen
            if (tooltipX + tooltipWidth > sceneView.position.width)
                tooltipX = screenPos.x - tooltipWidth - 20f;
            if (tooltipY + tooltipHeight > sceneView.position.height)
                tooltipY = sceneView.position.height - tooltipHeight - 10f;

            Rect tooltipRect = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
            EditorGUI.DrawRect(tooltipRect, new Color(0.15f, 0.15f, 0.15f, 0.92f));

            var style = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11,
                normal = { textColor = Color.white },
                padding = new RectOffset(6, 6, 4, 4),
                wordWrap = true
            };

            GUI.Label(tooltipRect, tooltipText, style);

            Handles.EndGUI();
        }

        // ──────────────────── Delete ────────────────────

        private static void DeleteSelectedRooms(LevelArchitectWindow window)
        {
            if (window.SelectedRooms.Count == 0) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete Rooms",
                $"Delete {window.SelectedRooms.Count} selected room(s)?\nThis action can be undone.",
                "Delete", "Cancel"
            );

            if (!confirm) return;

            Undo.SetCurrentGroupName("Delete Rooms");

            foreach (var room in window.SelectedRooms)
            {
                if (room != null)
                {
                    Undo.DestroyObjectImmediate(room.gameObject);
                }
            }

            window.ClearSelection();
        }

        // ──────────────────── Utility ────────────────────

        private static Room GetRoomAtPosition(Vector2 worldPos, Room[] rooms)
        {
            // Check in reverse to respect visual layering (last drawn = on top)
            for (int i = rooms.Length - 1; i >= 0; i--)
            {
                var room = rooms[i];
                if (room == null) continue;

                var box = room.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                Rect rect = LevelArchitectWindow.GetRoomWorldRect(room, box);
                if (rect.Contains(worldPos))
                    return room;
            }
            return null;
        }

        private static Vector3[] RectToCorners(Rect rect)
        {
            return new Vector3[]
            {
                new Vector3(rect.xMin, rect.yMin, 0),
                new Vector3(rect.xMax, rect.yMin, 0),
                new Vector3(rect.xMax, rect.yMax, 0),
                new Vector3(rect.xMin, rect.yMax, 0)
            };
        }

        private static void DrawThickOutline(Vector3[] corners, Color color, float width)
        {
            Handles.color = color;
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                Handles.DrawAAPolyLine(width, corners[i], corners[next]);
            }
            Handles.color = Color.white;
        }

        private static Rect GetNormalizedRect(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float width = Mathf.Abs(a.x - b.x);
            float height = Mathf.Abs(a.y - b.y);
            return new Rect(xMin, yMin, width, height);
        }
    }
}
