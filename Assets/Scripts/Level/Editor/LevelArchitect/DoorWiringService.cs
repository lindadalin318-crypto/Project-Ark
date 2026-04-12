using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Smart door connection service. Automatically creates, updates, and removes
    /// bidirectional Door pairs between rooms based on spatial adjacency.
    /// </summary>
    public static class DoorWiringService
    {
        // ──────────────────── Constants ────────────────────

        private const float SHARED_EDGE_THRESHOLD = 0.5f;
        private const float DOOR_COLLIDER_SIZE = 2f;
        private const float AUTO_CONNECT_SPAWN_OFFSET = 2f;
        private const string AUTO_CONNECT_SPAWN_NAME_PREFIX = "DoorSpawn_from_";

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
            var spawnA = CreateDoorSpawnPoint(roomA, midpoint - dirAtoB * AUTO_CONNECT_SPAWN_OFFSET, "DoorSpawn_from_" + roomB.RoomID);
            var spawnB = CreateDoorSpawnPoint(roomB, midpoint + dirAtoB * AUTO_CONNECT_SPAWN_OFFSET, "DoorSpawn_from_" + roomA.RoomID);

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
        /// Find the reciprocal door for a given door, if one exists.
        /// </summary>
        public static Door FindReverseDoor(Door door)
        {
            if (door == null || door.TargetRoom == null)
            {
                return null;
            }

            var ownerRoom = door.GetComponentInParent<Room>();
            if (ownerRoom == null)
            {
                return null;
            }

            return FindDoorTargetingRoom(door.TargetRoom, ownerRoom);
        }

        /// <summary>
        /// Update connection type for a door and keep the reciprocal door in sync when available.
        /// </summary>
        public static void SetConnectionType(Door door, ConnectionType connectionType)
        {
            if (door == null)
            {
                return;
            }

            ApplyConnectionType(door, connectionType);

            var reverseDoor = FindReverseDoor(door);
            if (reverseDoor != null && reverseDoor != door)
            {
                ApplyConnectionType(reverseDoor, connectionType);
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Recompute door position, ceremony, and auto-authored landing points for a connection pair.
        /// </summary>
        public static void RecalculateConnection(Door door)
        {
            if (door == null)
            {
                return;
            }

            var ownerRoom = door.GetComponentInParent<Room>();
            if (ownerRoom == null)
            {
                Debug.LogError($"[DoorWiringService] Cannot recalculate connection '{door.gameObject.name}': owner room is missing.");
                return;
            }

            SynchronizeRoomConnections(ownerRoom);
            if (door.TargetRoom != null)
            {
                SynchronizeRoomConnections(door.TargetRoom);
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Synchronize all authoring-owned door connections for a room.
        /// Recomputes door positions, reverse door positions, auto-connect spawn points,
        /// and door ceremony based on current room geometry/floor metadata.
        /// </summary>
        public static void SynchronizeRoomConnections(Room room)
        {
            if (room == null) return;
            if (!TryGetRoomWorldRect(room, out Rect roomRect)) return;

            var processedDoors = new HashSet<Door>();
            var doors = room.GetComponentsInChildren<Door>(true);
            foreach (var door in doors)
            {
                SynchronizeDoorConnection(room, roomRect, door, processedDoors);
            }
        }

        /// <summary>
        /// Synchronize all room door connections in the current scene.
        /// </summary>
        public static void SynchronizeAllRoomConnections()
        {
            var rooms = Object.FindObjectsByType<Room>();
            foreach (var room in rooms)
            {
                SynchronizeRoomConnections(room);
            }
        }

        /// <summary>
        /// Update door positions for all doors belonging to a room (after room move/resize).
        /// Kept for backward compatibility; now performs full connection synchronization.
        /// </summary>
        public static void UpdateDoorPositions(Room room)
        {
            SynchronizeRoomConnections(room);
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
            var allRooms = Object.FindObjectsByType<Room>();

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
                        var rooms = Object.FindObjectsByType<Room>();
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
                        var rooms = Object.FindObjectsByType<Room>();
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
                var allRooms = Object.FindObjectsByType<Room>();
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

            var navigationDoorsRoot = ownerRoom.transform.Find("Navigation/Doors");
            var navigationRoot = ownerRoom.transform.Find("Navigation");
            var parent = navigationDoorsRoot != null ? navigationDoorsRoot : (navigationRoot != null ? navigationRoot : ownerRoom.transform);
            doorGO.transform.SetParent(parent);
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

            var spawnRoot = room.transform.Find("Navigation/SpawnPoints");
            var navigationRoot = room.transform.Find("Navigation");
            var parent = spawnRoot != null ? spawnRoot : (navigationRoot != null ? navigationRoot : room.transform);
            spawnGO.transform.SetParent(parent);
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
            serialized.FindProperty("_ceremony").enumValueIndex = (int)GetExpectedCeremony(ownerRoom, targetRoomForFloorCheck);

            // Set player layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;
            }

            serialized.ApplyModifiedProperties();
        }

        private static void SynchronizeDoorConnection(Room ownerRoom, Rect ownerRect, Door door, HashSet<Door> processedDoors)
        {
            if (door == null || door.TargetRoom == null || processedDoors.Contains(door))
            {
                return;
            }

            if (!TryGetRoomWorldRect(door.TargetRoom, out Rect targetRect))
            {
                Debug.LogWarning($"[DoorWiringService] Cannot sync '{door.gameObject.name}': target room '{door.TargetRoom.RoomID}' has no BoxCollider2D.");
                return;
            }

            if (!FindSharedEdge(ownerRect, targetRect, out Vector2 midpoint, out Vector2 directionToTarget))
            {
                Debug.LogWarning($"[DoorWiringService] Cannot sync '{ownerRoom.RoomID}' ↔ '{door.TargetRoom.RoomID}': rooms no longer share an edge.");
                return;
            }

            var reverseDoor = FindDoorTargetingRoom(door.TargetRoom, ownerRoom);

            UpdateDoorTransform(door, midpoint);
            UpdateDoorCeremony(door, ownerRoom, door.TargetRoom);
            UpdateAutoConnectSpawnPoint(door.TargetSpawnPoint, midpoint + directionToTarget * AUTO_CONNECT_SPAWN_OFFSET);
            processedDoors.Add(door);

            if (reverseDoor != null)
            {
                UpdateDoorTransform(reverseDoor, midpoint);
                UpdateDoorCeremony(reverseDoor, door.TargetRoom, ownerRoom);
                UpdateAutoConnectSpawnPoint(reverseDoor.TargetSpawnPoint, midpoint - directionToTarget * AUTO_CONNECT_SPAWN_OFFSET);
                processedDoors.Add(reverseDoor);
            }
        }

        private static bool TryGetRoomWorldRect(Room room, out Rect rect)
        {
            rect = default;
            if (room == null)
            {
                return false;
            }

            var box = room.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                return false;
            }

            rect = LevelArchitectWindow.GetRoomWorldRect(room, box);
            return true;
        }

        private static Door FindDoorTargetingRoom(Room sourceRoom, Room targetRoom)
        {
            if (sourceRoom == null || targetRoom == null)
            {
                return null;
            }

            var doors = sourceRoom.GetComponentsInChildren<Door>(true);
            foreach (var door in doors)
            {
                if (door != null && door.TargetRoom == targetRoom)
                {
                    return door;
                }
            }

            return null;
        }

        private static void ApplyConnectionType(Door door, ConnectionType connectionType)
        {
            if (door == null || door.ConnectionType == connectionType)
            {
                return;
            }

            Undo.RecordObject(door, "Set Connection Type");
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_connectionType").enumValueIndex = (int)connectionType;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);
        }

        private static void UpdateDoorTransform(Door door, Vector2 newPosition)
        {
            if (door == null || Approximately((Vector2)door.transform.position, newPosition))
            {
                return;
            }

            Undo.RecordObject(door.transform, "Sync Door Position");
            door.transform.position = newPosition;
            EditorUtility.SetDirty(door.transform);
        }

        private static void UpdateDoorCeremony(Door door, Room ownerRoom, Room targetRoom)
        {
            if (door == null)
            {
                return;
            }

            var expectedCeremony = GetExpectedCeremony(ownerRoom, targetRoom);
            if (door.Ceremony == expectedCeremony)
            {
                return;
            }

            Undo.RecordObject(door, "Sync Door Ceremony");
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_ceremony").enumValueIndex = (int)expectedCeremony;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);
        }

        private static void UpdateAutoConnectSpawnPoint(Transform spawnPoint, Vector2 newPosition)
        {
            if (!IsAutoConnectSpawnPoint(spawnPoint) || Approximately((Vector2)spawnPoint.position, newPosition))
            {
                return;
            }

            Undo.RecordObject(spawnPoint, "Sync Door SpawnPoint");
            spawnPoint.position = newPosition;
            EditorUtility.SetDirty(spawnPoint);
        }

        private static bool IsAutoConnectSpawnPoint(Transform spawnPoint)
        {
            return spawnPoint != null && spawnPoint.name.StartsWith(AUTO_CONNECT_SPAWN_NAME_PREFIX);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }

        internal static TransitionCeremony GetExpectedCeremony(Room ownerRoom, Room targetRoom)
        {
            int ownerFloor = ownerRoom != null && ownerRoom.Data != null ? ownerRoom.Data.FloorLevel : 0;
            int targetFloor = targetRoom != null && targetRoom.Data != null ? targetRoom.Data.FloorLevel : 0;
            return ownerFloor != targetFloor ? TransitionCeremony.Layer : TransitionCeremony.Standard;
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
