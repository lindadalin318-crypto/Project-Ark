using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1 — sub-service of LevelArchitectWindow]
    /// One-way binder: Scene → LevelScaffoldData.
    /// Detects Transform/Size changes in scene Room GameObjects and syncs them into the ScaffoldData asset.
    /// The scene is always the source of truth; ScaffoldData is a snapshot/export target.
    /// </summary>
    public class ScaffoldSceneBinder
    {
        // ──────────────────── State ────────────────────

        private LevelScaffoldData _scaffoldData;
        private Dictionary<string, Room> _idToSceneRoom = new Dictionary<string, Room>();
        private Dictionary<string, Vector3> _lastKnownPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, Vector2> _lastKnownSizes = new Dictionary<string, Vector2>();

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

            var sceneRooms = Object.FindObjectsByType<Room>();

            var sceneRoomByID = new Dictionary<string, Room>();
            foreach (var room in sceneRooms)
            {
                if (room == null) continue;
                string id = room.RoomID;
                if (!sceneRoomByID.ContainsKey(id))
                    sceneRoomByID[id] = room;
            }

            if (_scaffoldData == null)
            {
                UnregisteredRooms.AddRange(sceneRooms.Where(r => r != null));
                return;
            }

            var matchedSceneIDs = new HashSet<string>();

            foreach (var scaffoldRoom in _scaffoldData.Rooms)
            {
                string id = scaffoldRoom.RoomID;
                if (sceneRoomByID.TryGetValue(id, out var sceneRoom))
                {
                    _idToSceneRoom[id] = sceneRoom;
                    matchedSceneIDs.Add(id);

                    _lastKnownPositions[id] = sceneRoom.transform.position;
                    var box = sceneRoom.GetComponent<BoxCollider2D>();
                    _lastKnownSizes[id] = box != null ? box.size : Vector2.one;
                }
                else
                {
                    MissingRooms.Add(scaffoldRoom);
                }
            }

            foreach (var room in sceneRooms)
            {
                if (room == null) continue;
                if (!matchedSceneIDs.Contains(room.RoomID))
                    UnregisteredRooms.Add(room);
            }
        }

        /// <summary>
        /// Tick the sync loop. Call every SceneView frame.
        /// Performs Scene→Scaffold sync only (scene is the source of truth).
        /// </summary>
        public void Tick()
        {
            if (_scaffoldData == null) return;
            SyncSceneToScaffold();
        }

        /// <summary>
        /// Register a scene Room into the Scaffold data.
        /// </summary>
        public void RegisterRoomToScaffold(Room room)
        {
            if (_scaffoldData == null || room == null) return;

            var box = room.GetComponent<BoxCollider2D>();
            Vector2 size = box != null ? box.size : new Vector2(20, 15);

            // Directly populate ScaffoldRoom via SerializedObject on the actual asset
            Undo.RecordObject(_scaffoldData, "Register Room to Scaffold");

            var scaffoldRoom = new ScaffoldRoom();
            _scaffoldData.AddRoom(scaffoldRoom);

            // Now write fields via SerializedObject on the real asset
            var serialized = new SerializedObject(_scaffoldData);
            int lastIndex = _scaffoldData.Rooms.Count - 1;
            var roomProp = serialized.FindProperty("_rooms").GetArrayElementAtIndex(lastIndex);

            roomProp.FindPropertyRelative("_roomID").stringValue = room.RoomID;
            roomProp.FindPropertyRelative("_displayName").stringValue = room.RoomID;
            roomProp.FindPropertyRelative("_nodeType").enumValueIndex = (int)room.NodeType;
            roomProp.FindPropertyRelative("_position").vector3Value = room.transform.position;
            roomProp.FindPropertyRelative("_size").vector2Value = size;

            if (room.Data != null)
                roomProp.FindPropertyRelative("_roomSO").objectReferenceValue = room.Data;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_scaffoldData);

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

        // ──────────────────── Sync Logic (Scene → Scaffold only) ────────────────────

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
    }
}
