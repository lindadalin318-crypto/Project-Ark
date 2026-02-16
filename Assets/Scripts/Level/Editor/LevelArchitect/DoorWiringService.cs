using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Smart door connection service. Automatically creates, updates, and removes
    /// bidirectional Door pairs between rooms based on spatial adjacency.
    /// </summary>
    public static class DoorWiringService
    {
        // ──────────────────── Constants ────────────────────

        private const float SHARED_EDGE_THRESHOLD = 0.5f;
        private const float DOOR_COLLIDER_SIZE = 2f;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Auto-connect two rooms: create a pair of bidirectional Door GameObjects
        /// at the shared edge midpoint, configure all serialized fields.
        /// </summary>
        public static (Door doorA, Door doorB) AutoConnectRooms(Room roomA, Room roomB)
        {
            if (roomA == null || roomB == null)
            {
                Debug.LogError("[DoorWiringService] Cannot connect: one or both rooms are null.");
                return (null, null);
            }

            // Check if already connected
            if (AreRoomsConnected(roomA, roomB))
            {
                Debug.LogWarning($"[DoorWiringService] Rooms '{roomA.RoomID}' and '{roomB.RoomID}' are already connected.");
                return (null, null);
            }

            var boxA = roomA.GetComponent<BoxCollider2D>();
            var boxB = roomB.GetComponent<BoxCollider2D>();
            if (boxA == null || boxB == null)
            {
                Debug.LogError("[DoorWiringService] Cannot connect: one or both rooms lack BoxCollider2D.");
                return (null, null);
            }

            Rect rectA = LevelArchitectWindow.GetRoomWorldRect(roomA, boxA);
            Rect rectB = LevelArchitectWindow.GetRoomWorldRect(roomB, boxB);

            // Calculate shared edge midpoint and direction
            if (!FindSharedEdge(rectA, rectB, out Vector2 midpoint, out Vector2 dirAtoB))
            {
                // No shared edge — use closest edge points
                midpoint = (rectA.center + rectB.center) / 2f;
                dirAtoB = ((Vector2)roomB.transform.position - (Vector2)roomA.transform.position).normalized;
            }

            Undo.SetCurrentGroupName("Connect Rooms");

            // ── Create Door in Room A (leading to Room B) ──
            var doorAGO = CreateDoorGameObject(roomA, roomB, midpoint, dirAtoB, "Door_to_" + roomB.RoomID);
            var doorA = doorAGO.GetComponent<Door>();

            // ── Create Door in Room B (leading to Room A) ──
            var doorBGO = CreateDoorGameObject(roomB, roomA, midpoint, -dirAtoB, "Door_to_" + roomA.RoomID);
            var doorB = doorBGO.GetComponent<Door>();

            // ── Create SpawnPoints at door positions ──
            var spawnA = CreateDoorSpawnPoint(roomA, midpoint + dirAtoB * 2f, "DoorSpawn_from_" + roomB.RoomID);
            var spawnB = CreateDoorSpawnPoint(roomB, midpoint - dirAtoB * 2f, "DoorSpawn_from_" + roomA.RoomID);

            // ── Wire references ──
            ConfigureDoor(doorA, roomB, spawnB, roomA, roomB);
            ConfigureDoor(doorB, roomA, spawnA, roomB, roomA);

            EditorUtility.SetDirty(roomA);
            EditorUtility.SetDirty(roomB);

            Debug.Log($"[DoorWiringService] Connected '{roomA.RoomID}' ↔ '{roomB.RoomID}' at {midpoint}");

            return (doorA, doorB);
        }

        /// <summary>
        /// Disconnect two rooms by removing all Door pairs between them.
        /// </summary>
        public static void DisconnectRooms(Room roomA, Room roomB)
        {
            if (roomA == null || roomB == null) return;

            Undo.SetCurrentGroupName("Disconnect Rooms");

            // Find and remove doors from A → B
            RemoveDoorsTargeting(roomA, roomB);

            // Find and remove doors from B → A
            RemoveDoorsTargeting(roomB, roomA);

            Debug.Log($"[DoorWiringService] Disconnected '{roomA.RoomID}' ↔ '{roomB.RoomID}'");
        }

        /// <summary>
        /// Update door positions for all doors belonging to a room (after room move/resize).
        /// </summary>
        public static void UpdateDoorPositions(Room room)
        {
            if (room == null) return;

            var box = room.GetComponent<BoxCollider2D>();
            if (box == null) return;

            Rect roomRect = LevelArchitectWindow.GetRoomWorldRect(room, box);
            var doors = room.GetComponentsInChildren<Door>(true);

            foreach (var door in doors)
            {
                if (door == null || door.TargetRoom == null) continue;

                var targetBox = door.TargetRoom.GetComponent<BoxCollider2D>();
                if (targetBox == null) continue;

                Rect targetRect = LevelArchitectWindow.GetRoomWorldRect(door.TargetRoom, targetBox);

                if (FindSharedEdge(roomRect, targetRect, out Vector2 newMidpoint, out Vector2 dir))
                {
                    Undo.RecordObject(door.transform, "Update Door Position");
                    door.transform.position = newMidpoint;
                }
            }
        }

        /// <summary>
        /// Check if two rooms have any door connections between them.
        /// </summary>
        public static bool AreRoomsConnected(Room roomA, Room roomB)
        {
            var doorsA = roomA.GetComponentsInChildren<Door>(true);
            foreach (var door in doorsA)
            {
                if (door.TargetRoom == roomB) return true;
            }
            return false;
        }

        /// <summary>
        /// Find all rooms that share an edge with the given room (within threshold).
        /// </summary>
        public static List<Room> FindAdjacentRooms(Room room)
        {
            var adjacent = new List<Room>();
            if (room == null) return adjacent;

            var box = room.GetComponent<BoxCollider2D>();
            if (box == null) return adjacent;

            Rect roomRect = LevelArchitectWindow.GetRoomWorldRect(room, box);
            var allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

            foreach (var other in allRooms)
            {
                if (other == null || other == room) continue;

                var otherBox = other.GetComponent<BoxCollider2D>();
                if (otherBox == null) continue;

                Rect otherRect = LevelArchitectWindow.GetRoomWorldRect(other, otherBox);

                if (FindSharedEdge(roomRect, otherRect, out _, out _))
                {
                    adjacent.Add(other);
                }
            }

            return adjacent;
        }

        /// <summary>
        /// Auto-detect and connect all adjacent rooms that aren't already connected.
        /// Useful after blockout placement.
        /// </summary>
        public static int AutoConnectAllAdjacent(Room room)
        {
            int connectionsCreated = 0;
            var adjacent = FindAdjacentRooms(room);

            foreach (var neighbor in adjacent)
            {
                if (!AreRoomsConnected(room, neighbor))
                {
                    AutoConnectRooms(room, neighbor);
                    connectionsCreated++;
                }
            }

            return connectionsCreated;
        }

        // ──────────────────── Shared Edge Detection ────────────────────

        /// <summary>
        /// Find the shared edge between two rects. Returns true if they share an edge
        /// (within threshold). Out parameters: midpoint and direction from A to B.
        /// </summary>
        public static bool FindSharedEdge(Rect rectA, Rect rectB, out Vector2 midpoint, out Vector2 direction)
        {
            midpoint = Vector2.zero;
            direction = Vector2.zero;

            float threshold = SHARED_EDGE_THRESHOLD;

            // Check right edge of A ↔ left edge of B
            if (Mathf.Abs(rectA.xMax - rectB.xMin) < threshold)
            {
                float overlapMin = Mathf.Max(rectA.yMin, rectB.yMin);
                float overlapMax = Mathf.Min(rectA.yMax, rectB.yMax);
                if (overlapMax > overlapMin)
                {
                    midpoint = new Vector2(rectA.xMax, (overlapMin + overlapMax) / 2f);
                    direction = Vector2.right;
                    return true;
                }
            }

            // Check left edge of A ↔ right edge of B
            if (Mathf.Abs(rectA.xMin - rectB.xMax) < threshold)
            {
                float overlapMin = Mathf.Max(rectA.yMin, rectB.yMin);
                float overlapMax = Mathf.Min(rectA.yMax, rectB.yMax);
                if (overlapMax > overlapMin)
                {
                    midpoint = new Vector2(rectA.xMin, (overlapMin + overlapMax) / 2f);
                    direction = Vector2.left;
                    return true;
                }
            }

            // Check top edge of A ↔ bottom edge of B
            if (Mathf.Abs(rectA.yMax - rectB.yMin) < threshold)
            {
                float overlapMin = Mathf.Max(rectA.xMin, rectB.xMin);
                float overlapMax = Mathf.Min(rectA.xMax, rectB.xMax);
                if (overlapMax > overlapMin)
                {
                    midpoint = new Vector2((overlapMin + overlapMax) / 2f, rectA.yMax);
                    direction = Vector2.up;
                    return true;
                }
            }

            // Check bottom edge of A ↔ top edge of B
            if (Mathf.Abs(rectA.yMin - rectB.yMax) < threshold)
            {
                float overlapMin = Mathf.Max(rectA.xMin, rectB.xMin);
                float overlapMax = Mathf.Min(rectA.xMax, rectB.xMax);
                if (overlapMax > overlapMin)
                {
                    midpoint = new Vector2((overlapMin + overlapMax) / 2f, rectA.yMin);
                    direction = Vector2.down;
                    return true;
                }
            }

            return false;
        }

        // ──────────────────── SceneView Connect Mode ────────────────────

        // State for the drag-to-connect interaction
        private static Room _connectSourceRoom;
        private static bool _isConnecting;

        /// <summary> Whether a connect drag is in progress. </summary>
        public static bool IsConnecting => _isConnecting;

        /// <summary> The source room of the current connect drag. </summary>
        public static Room ConnectSourceRoom => _connectSourceRoom;

        /// <summary>
        /// Handle Connect Mode input in SceneView.
        /// Called from LevelArchitectWindow when in Connect mode.
        /// </summary>
        public static void HandleConnectModeInput(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null) return;

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
                        var hitRoom = GetRoomAtPosition(worldPos, rooms);

                        if (hitRoom != null)
                        {
                            _connectSourceRoom = hitRoom;
                            _isConnecting = true;
                            GUIUtility.hotControl = controlID;
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isConnecting)
                    {
                        // Draw connection line preview
                        GUIUtility.hotControl = controlID;
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isConnecting && e.button == 0)
                    {
                        var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
                        var targetRoom = GetRoomAtPosition(worldPos, rooms);

                        if (targetRoom != null && targetRoom != _connectSourceRoom)
                        {
                            AutoConnectRooms(_connectSourceRoom, targetRoom);
                        }

                        _isConnecting = false;
                        _connectSourceRoom = null;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape && _isConnecting)
                    {
                        _isConnecting = false;
                        _connectSourceRoom = null;
                        e.Use();
                    }
                    break;
            }

            // Draw connection line while dragging
            if (_isConnecting && _connectSourceRoom != null)
            {
                Vector3 startPos = _connectSourceRoom.transform.position;
                Vector3 endPos = worldPos;

                Handles.color = new Color(0.2f, 0.8f, 0.4f, 0.8f);
                Handles.DrawAAPolyLine(3f, startPos, endPos);

                // Draw target highlight
                var allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
                var hoverTarget = GetRoomAtPosition(worldPos, allRooms);
                if (hoverTarget != null && hoverTarget != _connectSourceRoom)
                {
                    var targetBox = hoverTarget.GetComponent<BoxCollider2D>();
                    if (targetBox != null)
                    {
                        Rect targetRect = LevelArchitectWindow.GetRoomWorldRect(hoverTarget, targetBox);
                        var corners = new Vector3[]
                        {
                            new Vector3(targetRect.xMin, targetRect.yMin, 0),
                            new Vector3(targetRect.xMax, targetRect.yMin, 0),
                            new Vector3(targetRect.xMax, targetRect.yMax, 0),
                            new Vector3(targetRect.xMin, targetRect.yMax, 0)
                        };
                        Handles.DrawSolidRectangleWithOutline(corners, new Color(0.2f, 0.8f, 0.4f, 0.1f), new Color(0.2f, 0.8f, 0.4f, 0.8f));
                    }
                }

                Handles.color = Color.white;
            }
        }

        // ──────────────────── Private Helpers ────────────────────

        private static GameObject CreateDoorGameObject(Room ownerRoom, Room targetRoom,
            Vector2 position, Vector2 facingDirection, string doorName)
        {
            var doorGO = new GameObject(doorName);
            Undo.RegisterCreatedObjectUndo(doorGO, "Create Door");

            doorGO.transform.SetParent(ownerRoom.transform);
            doorGO.transform.position = position;

            // Add collider for trigger detection
            var collider = doorGO.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(DOOR_COLLIDER_SIZE, DOOR_COLLIDER_SIZE);

            // Add Door component
            doorGO.AddComponent<Door>();

            return doorGO;
        }

        private static Transform CreateDoorSpawnPoint(Room room, Vector2 position, string spawnName)
        {
            var spawnGO = new GameObject(spawnName);
            Undo.RegisterCreatedObjectUndo(spawnGO, "Create Door SpawnPoint");

            spawnGO.transform.SetParent(room.transform);
            spawnGO.transform.position = position;

            return spawnGO.transform;
        }

        private static void ConfigureDoor(Door door, Room targetRoom, Transform targetSpawnPoint,
            Room ownerRoom, Room targetRoomForFloorCheck)
        {
            var serialized = new SerializedObject(door);

            serialized.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serialized.FindProperty("_targetSpawnPoint").objectReferenceValue = targetSpawnPoint;
            serialized.FindProperty("_initialState").enumValueIndex = (int)DoorState.Open;

            // Check floor level difference for layer transition
            int ownerFloor = ownerRoom.Data != null ? ownerRoom.Data.FloorLevel : 0;
            int targetFloor = targetRoomForFloorCheck.Data != null ? targetRoomForFloorCheck.Data.FloorLevel : 0;
            serialized.FindProperty("_isLayerTransition").boolValue = ownerFloor != targetFloor;

            // Set player layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;
            }

            serialized.ApplyModifiedProperties();
        }

        private static void RemoveDoorsTargeting(Room sourceRoom, Room targetRoom)
        {
            var doors = sourceRoom.GetComponentsInChildren<Door>(true);

            foreach (var door in doors)
            {
                if (door != null && door.TargetRoom == targetRoom)
                {
                    // Also try to remove the associated spawn point in target room
                    if (door.TargetSpawnPoint != null)
                    {
                        Undo.DestroyObjectImmediate(door.TargetSpawnPoint.gameObject);
                    }

                    Undo.DestroyObjectImmediate(door.gameObject);
                }
            }
        }

        private static Room GetRoomAtPosition(Vector2 worldPos, Room[] rooms)
        {
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
    }
}
