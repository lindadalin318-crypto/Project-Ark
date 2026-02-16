using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Scans existing scene Room/Door GameObjects and constructs a new LevelScaffoldData
    /// from them. Used for migrating hand-built or legacy-tool-generated scenes
    /// into the Level Architect workflow.
    /// </summary>
    public static class SceneScanner
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_PATH = "Assets/_Data/Level/Rooms/";

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Scan the current scene, collect all Room and Door components,
        /// build a LevelScaffoldData asset and save it.
        /// </summary>
        public static LevelScaffoldData ScanScene()
        {
            var rooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

            if (rooms.Length == 0)
            {
                EditorUtility.DisplayDialog("Scene Scanner",
                    "No Room components found in the scene.", "OK");
                return null;
            }

            // Build scaffold data
            var scaffoldData = ScriptableObject.CreateInstance<LevelScaffoldData>();

            var serializedScaffold = new SerializedObject(scaffoldData);
            serializedScaffold.FindProperty("_levelName").stringValue = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Detect floor level from rooms
            int minFloor = int.MaxValue;
            foreach (var room in rooms)
            {
                if (room.Data != null && room.Data.FloorLevel < minFloor)
                    minFloor = room.Data.FloorLevel;
            }
            serializedScaffold.FindProperty("_floorLevel").intValue = minFloor != int.MaxValue ? minFloor : 0;
            serializedScaffold.ApplyModifiedPropertiesWithoutUndo();

            // Build room ID → Room lookup
            var roomLookup = new Dictionary<string, Room>();
            var processedRooms = new List<(Room room, ScaffoldRoom scaffold)>();

            foreach (var room in rooms)
            {
                if (room == null) continue;

                // Ensure room has a RoomSO
                RoomSO roomSO = room.Data;
                if (roomSO == null)
                {
                    roomSO = CreateRoomSOForScannedRoom(room);
                    AssignRoomSO(room, roomSO);
                }

                string roomID = room.RoomID;
                roomLookup[roomID] = room;

                // Create ScaffoldRoom
                var box = room.GetComponent<BoxCollider2D>();
                Vector2 size = box != null ? box.size : new Vector2(20, 15);

                var scaffoldRoom = CreateScaffoldRoom(roomID, room, size, roomSO);
                scaffoldData.AddRoom(scaffoldRoom);

                processedRooms.Add((room, scaffoldRoom));
            }

            // Build connections from Door components
            foreach (var (room, scaffoldRoom) in processedRooms)
            {
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    string targetID = door.TargetRoom.RoomID;

                    // Check if this connection already exists
                    bool alreadyExists = false;
                    foreach (var conn in scaffoldRoom.Connections)
                    {
                        if (conn.TargetRoomID == targetID)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        var connection = CreateScaffoldConnection(door, targetID);
                        scaffoldRoom.AddConnection(connection);
                    }
                }
            }

            // Save scaffold asset
            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Level Scaffold",
                "ScannedLevel",
                "asset",
                "Choose where to save the scanned level scaffold data.",
                "Assets/_Data/Level"
            );

            if (string.IsNullOrEmpty(savePath))
            {
                Object.DestroyImmediate(scaffoldData);
                return null;
            }

            AssetDatabase.CreateAsset(scaffoldData, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneScanner] Scanned {processedRooms.Count} rooms, saved scaffold to {savePath}");

            EditorUtility.DisplayDialog("Scene Scanner",
                $"Scan complete!\n\n" +
                $"Rooms found: {processedRooms.Count}\n" +
                $"Scaffold saved to: {savePath}",
                "OK");

            EditorGUIUtility.PingObject(scaffoldData);

            return scaffoldData;
        }

        // ──────────────────── Private Helpers ────────────────────

        private static RoomSO CreateRoomSOForScannedRoom(Room room)
        {
            EnsureDirectoryExists(ROOM_DATA_PATH);

            var roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{room.gameObject.name}_Data";

            var serialized = new SerializedObject(roomSO);
            serialized.FindProperty("_roomID").stringValue = room.gameObject.name;
            serialized.FindProperty("_displayName").stringValue = room.gameObject.name;
            serialized.FindProperty("_type").enumValueIndex = (int)room.Type;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ROOM_DATA_PATH}{roomSO.name}.asset";

            // Check for existing
            var existing = AssetDatabase.LoadAssetAtPath<RoomSO>(path);
            if (existing != null)
            {
                Object.DestroyImmediate(roomSO);
                return existing;
            }

            AssetDatabase.CreateAsset(roomSO, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneScanner] Auto-created RoomSO at {path}");

            return roomSO;
        }

        private static void AssignRoomSO(Room room, RoomSO roomSO)
        {
            var serialized = new SerializedObject(room);
            serialized.FindProperty("_data").objectReferenceValue = roomSO;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(room);
        }

        private static ScaffoldRoom CreateScaffoldRoom(string roomID, Room room, Vector2 size, RoomSO roomSO)
        {
            var scaffoldRoom = new ScaffoldRoom();

            // We need to use reflection or a temporary SO to set private fields
            // Using a temp LevelScaffoldData + SerializedObject approach
            var tempData = ScriptableObject.CreateInstance<LevelScaffoldData>();
            tempData.AddRoom(scaffoldRoom);

            var serialized = new SerializedObject(tempData);
            var roomProp = serialized.FindProperty("_rooms").GetArrayElementAtIndex(0);

            roomProp.FindPropertyRelative("_roomID").stringValue = roomID;
            roomProp.FindPropertyRelative("_displayName").stringValue =
                room.Data != null ? room.Data.DisplayName : room.gameObject.name;
            roomProp.FindPropertyRelative("_roomType").enumValueIndex = (int)room.Type;
            roomProp.FindPropertyRelative("_position").vector3Value = room.transform.position;
            roomProp.FindPropertyRelative("_size").vector2Value = size;
            roomProp.FindPropertyRelative("_roomSO").objectReferenceValue = roomSO;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            var result = tempData.Rooms[0];
            Object.DestroyImmediate(tempData);

            return result;
        }

        private static ScaffoldDoorConnection CreateScaffoldConnection(Door door, string targetRoomID)
        {
            var connection = new ScaffoldDoorConnection();

            connection.TargetRoomID = targetRoomID;
            connection.DoorPosition = door.transform.localPosition;
            connection.IsLayerTransition = door.IsLayerTransition;

            // Calculate direction from door to target
            if (door.TargetRoom != null)
            {
                Vector2 dir = ((Vector2)door.TargetRoom.transform.position -
                              (Vector2)door.transform.position).normalized;
                connection.DoorDirection = dir;
            }

            return connection;
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
