using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Maintains bidirectional binding between LevelScaffoldData and Scene Room GameObjects.
    /// Detects changes on either side and synchronizes them, keeping data consistent.
    /// </summary>
    public class ScaffoldSceneBinder
    {
        // ──────────────────── State ────────────────────

        private LevelScaffoldData _scaffoldData;
        private Dictionary<string, Room> _idToSceneRoom = new Dictionary<string, Room>();
        private Dictionary<string, Vector3> _lastKnownPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Vector2> _lastKnownSizes = new Dictionary<string, Vector2>();
        private double _lastSyncTime;

        private const float SYNC_INTERVAL = 0.5f; // seconds between Scaffold→Scene checks
        private const float POSITION_EPSILON = 0.01f;
        private const float SIZE_EPSILON = 0.01f;

        // ──────────────────── Sync Status ────────────────────

        /// <summary> Rooms in scene but not in Scaffold. </summary>
        public List<Room> UnregisteredRooms { get; private set; } = new List<Room>();

        /// <summary> Rooms in Scaffold but not in scene. </summary>
        public List<ScaffoldRoom> MissingRooms { get; private set; } = new List<ScaffoldRoom>();

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Initialize the binder with a scaffold data asset. Builds the ID→Room mapping.
        /// </summary>
        public void Initialize(LevelScaffoldData scaffoldData)
        {
            _scaffoldData = scaffoldData;
            RebuildMapping();
        }

        /// <summary>
        /// Rebuild the mapping between ScaffoldRoom IDs and scene Room components.
        /// Call after hierarchy changes.
        /// </summary>
        public void RebuildMapping()
        {
            _idToSceneRoom.Clear();
            _lastKnownPositions.Clear();
            _lastKnownSizes.Clear();
            UnregisteredRooms.Clear();
            MissingRooms.Clear();

            var sceneRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

            // Build scene room lookup
            var sceneRoomByID = new Dictionary<string, Room>();
            foreach (var room in sceneRooms)
            {
                if (room == null) continue;
                string id = room.RoomID;
                if (!sceneRoomByID.ContainsKey(id))
                {
                    sceneRoomByID[id] = room;
                }
            }

            if (_scaffoldData == null)
            {
                // No scaffold — all scene rooms are "unregistered"
                UnregisteredRooms.AddRange(sceneRooms.Where(r => r != null));
                return;
            }

            // Match scaffold rooms to scene rooms
            var matchedSceneIDs = new HashSet<string>();

            foreach (var scaffoldRoom in _scaffoldData.Rooms)
            {
                string id = scaffoldRoom.RoomID;

                if (sceneRoomByID.TryGetValue(id, out var sceneRoom))
                {
                    _idToSceneRoom[id] = sceneRoom;
                    matchedSceneIDs.Add(id);

                    // Cache current state
                    _lastKnownPositions[id] = sceneRoom.transform.position;
                    var box = sceneRoom.GetComponent<BoxCollider2D>();
                    _lastKnownSizes[id] = box != null ? box.size : Vector2.one;
                }
                else
                {
                    MissingRooms.Add(scaffoldRoom);
                }
            }

            // Find unregistered scene rooms
            foreach (var room in sceneRooms)
            {
                if (room == null) continue;
                if (!matchedSceneIDs.Contains(room.RoomID))
                {
                    UnregisteredRooms.Add(room);
                }
            }
        }

        /// <summary>
        /// Tick the sync loop. Call every SceneView frame.
        /// Performs Scene→Scaffold sync (immediate) and Scaffold→Scene sync (periodic).
        /// </summary>
        public void Tick()
        {
            if (_scaffoldData == null) return;

            // Scene → Scaffold (every frame, detect Transform changes)
            SyncSceneToScaffold();

            // Scaffold → Scene (periodic)
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastSyncTime >= SYNC_INTERVAL)
            {
                _lastSyncTime = now;
                SyncScaffoldToScene();
            }
        }

        /// <summary>
        /// Register a scene Room into the Scaffold data.
        /// </summary>
        public void RegisterRoomToScaffold(Room room)
        {
            if (_scaffoldData == null || room == null) return;

            var box = room.GetComponent<BoxCollider2D>();
            Vector2 size = box != null ? box.size : new Vector2(20, 15);

            var scaffoldRoom = new ScaffoldRoom();

            // Set fields via SerializedObject (since fields are private)
            var tempSO = ScriptableObject.CreateInstance<LevelScaffoldData>();
            tempSO.AddRoom(scaffoldRoom);
            var serialized = new SerializedObject(tempSO);
            var roomProp = serialized.FindProperty("_rooms").GetArrayElementAtIndex(0);

            roomProp.FindPropertyRelative("_roomID").stringValue = room.RoomID;
            roomProp.FindPropertyRelative("_displayName").stringValue = room.RoomID;
            roomProp.FindPropertyRelative("_roomType").enumValueIndex = (int)room.Type;
            roomProp.FindPropertyRelative("_position").vector3Value = room.transform.position;
            roomProp.FindPropertyRelative("_size").vector2Value = size;

            if (room.Data != null)
            {
                roomProp.FindPropertyRelative("_roomSO").objectReferenceValue = room.Data;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Now add to real scaffold data
            Undo.RecordObject(_scaffoldData, "Register Room to Scaffold");
            _scaffoldData.AddRoom(tempSO.Rooms[0]);
            EditorUtility.SetDirty(_scaffoldData);

            Object.DestroyImmediate(tempSO);

            // Rebuild mapping
            RebuildMapping();

            Debug.Log($"[ScaffoldSceneBinder] Registered room '{room.RoomID}' to scaffold.");
        }

        /// <summary>
        /// Remove a ScaffoldRoom that has no matching scene object.
        /// </summary>
        public void RemoveFromScaffold(ScaffoldRoom scaffoldRoom)
        {
            if (_scaffoldData == null || scaffoldRoom == null) return;

            Undo.RecordObject(_scaffoldData, "Remove Room from Scaffold");
            _scaffoldData.RemoveRoom(scaffoldRoom);
            EditorUtility.SetDirty(_scaffoldData);

            RebuildMapping();

            Debug.Log($"[ScaffoldSceneBinder] Removed room '{scaffoldRoom.RoomID}' from scaffold.");
        }

        // ──────────────────── Sync Logic ────────────────────

        private void SyncSceneToScaffold()
        {
            foreach (var pair in _idToSceneRoom)
            {
                string id = pair.Key;
                Room room = pair.Value;
                if (room == null) continue;

                var scaffoldRoom = _scaffoldData.Rooms.FirstOrDefault(r => r.RoomID == id);
                if (scaffoldRoom == null) continue;

                Vector3 currentPos = room.transform.position;
                var box = room.GetComponent<BoxCollider2D>();
                Vector2 currentSize = box != null ? box.size : Vector2.one;

                bool posChanged = !_lastKnownPositions.ContainsKey(id) ||
                    Vector3.Distance(currentPos, _lastKnownPositions[id]) > POSITION_EPSILON;
                bool sizeChanged = !_lastKnownSizes.ContainsKey(id) ||
                    Vector2.Distance(currentSize, _lastKnownSizes[id]) > SIZE_EPSILON;

                if (posChanged || sizeChanged)
                {
                    Undo.RecordObject(_scaffoldData, "Sync Room to Scaffold");

                    if (posChanged)
                    {
                        scaffoldRoom.Position = currentPos;
                        _lastKnownPositions[id] = currentPos;
                    }

                    if (sizeChanged)
                    {
                        scaffoldRoom.Size = currentSize;
                        _lastKnownSizes[id] = currentSize;
                    }

                    EditorUtility.SetDirty(_scaffoldData);
                }
            }
        }

        private void SyncScaffoldToScene()
        {
            foreach (var scaffoldRoom in _scaffoldData.Rooms)
            {
                if (!_idToSceneRoom.TryGetValue(scaffoldRoom.RoomID, out var sceneRoom))
                    continue;
                if (sceneRoom == null) continue;

                Vector3 scenePos = sceneRoom.transform.position;
                var box = sceneRoom.GetComponent<BoxCollider2D>();
                Vector2 sceneSize = box != null ? box.size : Vector2.one;

                // Check if scaffold data differs from scene (external change via Inspector)
                bool possDiffers = Vector3.Distance(scaffoldRoom.Position, scenePos) > POSITION_EPSILON;
                bool sizeDiffers = Vector2.Distance(scaffoldRoom.Size, sceneSize) > SIZE_EPSILON;

                if (possDiffers)
                {
                    Undo.RecordObject(sceneRoom.transform, "Sync Scaffold → Scene Position");
                    sceneRoom.transform.position = scaffoldRoom.Position;
                    _lastKnownPositions[scaffoldRoom.RoomID] = scaffoldRoom.Position;
                }

                if (sizeDiffers && box != null)
                {
                    Undo.RecordObject(box, "Sync Scaffold → Scene Size");
                    box.size = scaffoldRoom.Size;
                    _lastKnownSizes[scaffoldRoom.RoomID] = scaffoldRoom.Size;
                }
            }
        }
    }
}
