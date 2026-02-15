using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectArk.Level
{
    /// <summary>
    /// Generates Unity scene objects from LevelScaffoldData.
    /// Creates Room GameObjects, configures components, and sets up door connections.
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// Generates a complete level from LevelScaffoldData.
        /// </summary>
        /// <param name="scaffold">The scaffold data to generate from</param>
        /// <param name="elementLibrary">Library of prefabs to use</param>
        /// <param name="parentTransform">Parent transform for all generated objects</param>
        public static void GenerateLevel(
            LevelScaffoldData scaffold,
            LevelElementLibrary elementLibrary,
            Transform parentTransform = null)
        {
            if (scaffold == null)
            {
                Debug.LogError("[LevelGenerator] Scaffold data is null!");
                return;
            }

            if (elementLibrary == null)
            {
                Debug.LogError("[LevelGenerator] Element library is null!");
                return;
            }

            Undo.SetCurrentGroupName($"Generate Level: {scaffold.LevelName}");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                GameObject levelRoot = CreateLevelRoot(scaffold, parentTransform);
                Dictionary<string, Room> generatedRooms = GenerateRooms(scaffold, elementLibrary, levelRoot);
                SetupDoorConnections(scaffold, generatedRooms, elementLibrary);

                Debug.Log($"[LevelGenerator] Successfully generated level: {scaffold.LevelName}");
                EditorUtility.SetDirty(levelRoot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelGenerator] Failed to generate level: {e.Message}");
                Undo.RevertAllDownToGroup(undoGroup);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        /// <summary>
        /// Creates the root GameObject for the entire level.
        /// </summary>
        private static GameObject CreateLevelRoot(LevelScaffoldData scaffold, Transform parentTransform)
        {
            GameObject levelRoot = new GameObject($"--- {scaffold.LevelName} ---");
            Undo.RegisterCreatedObjectUndo(levelRoot, "Create Level Root");

            if (parentTransform != null)
            {
                Undo.SetTransformParent(levelRoot.transform, parentTransform, "Set Level Parent");
            }

            return levelRoot;
        }

        /// <summary>
        /// Generates all Room GameObjects from the scaffold.
        /// </summary>
        private static Dictionary<string, Room> GenerateRooms(
            LevelScaffoldData scaffold,
            LevelElementLibrary elementLibrary,
            GameObject levelRoot)
        {
            Dictionary<string, Room> roomMap = new Dictionary<string, Room>();

            foreach (ScaffoldRoom scaffoldRoom in scaffold.Rooms)
            {
                Room room = GenerateSingleRoom(scaffoldRoom, elementLibrary, levelRoot);
                roomMap[scaffoldRoom.RoomID] = room;
            }

            return roomMap;
        }

        /// <summary>
        /// Generates a single Room GameObject.
        /// </summary>
        private static Room GenerateSingleRoom(
            ScaffoldRoom scaffoldRoom,
            LevelElementLibrary elementLibrary,
            GameObject levelRoot)
        {
            GameObject roomObj;

            if (elementLibrary.RoomTemplate != null)
            {
                roomObj = (GameObject)PrefabUtility.InstantiatePrefab(elementLibrary.RoomTemplate);
                Undo.RegisterCreatedObjectUndo(roomObj, "Instantiate Room");
            }
            else
            {
                roomObj = new GameObject(scaffoldRoom.DisplayName);
                Undo.RegisterCreatedObjectUndo(roomObj, "Create Room");
                Undo.AddComponent<Room>(roomObj);
            }

            roomObj.name = scaffoldRoom.DisplayName;
            Undo.SetTransformParent(roomObj.transform, levelRoot.transform, "Set Room Parent");
            roomObj.transform.position = scaffoldRoom.Position;

            Room room = roomObj.GetComponent<Room>();
            if (room == null)
            {
                room = Undo.AddComponent<Room>(roomObj);
            }

            SetupRoomCollider(roomObj, scaffoldRoom);
            SetupRoomData(room, scaffoldRoom);

            return room;
        }

        /// <summary>
        /// Sets up the room's collider for camera confinement.
        /// </summary>
        private static void SetupRoomCollider(GameObject roomObj, ScaffoldRoom scaffoldRoom)
        {
            BoxCollider2D confiner = roomObj.GetComponent<BoxCollider2D>();
            if (confiner == null)
            {
                confiner = Undo.AddComponent<BoxCollider2D>(roomObj);
            }

            confiner.isTrigger = true;
            confiner.size = scaffoldRoom.Size;
            confiner.offset = Vector2.zero;
        }

        /// <summary>
        /// Sets up the Room component's data.
        /// </summary>
        private static void SetupRoomData(Room room, ScaffoldRoom scaffoldRoom)
        {
            SerializedObject serializedRoom = new SerializedObject(room);

            if (scaffoldRoom.RoomSO != null)
            {
                serializedRoom.FindProperty("_data").objectReferenceValue = scaffoldRoom.RoomSO;
            }
            else
            {
                RoomSO roomSO = CreateRoomSO(scaffoldRoom);
                serializedRoom.FindProperty("_data").objectReferenceValue = roomSO;
            }

            serializedRoom.ApplyModifiedProperties();
            EditorUtility.SetDirty(room);
        }

        /// <summary>
        /// Creates a new RoomSO for a room.
        /// </summary>
        private static RoomSO CreateRoomSO(ScaffoldRoom scaffoldRoom)
        {
            RoomSO roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{scaffoldRoom.DisplayName}_Data";

            SerializedObject serializedSO = new SerializedObject(roomSO);
            serializedSO.FindProperty("_type").enumValueIndex = (int)scaffoldRoom.RoomType;
            serializedSO.FindProperty("_floorLevel").intValue = scaffoldRoom.Position.z != 0
                ? Mathf.RoundToInt(scaffoldRoom.Position.z)
                : 0;
            serializedSO.FindProperty("_roomID").stringValue = scaffoldRoom.RoomID;
            serializedSO.ApplyModifiedProperties();

            string path = $"Assets/_Data/Level/Rooms/{roomSO.name}.asset";
            AssetDatabase.CreateAsset(roomSO, path);
            AssetDatabase.SaveAssets();

            return roomSO;
        }

        /// <summary>
        /// Sets up door connections between rooms.
        /// </summary>
        private static void SetupDoorConnections(
            LevelScaffoldData scaffold,
            Dictionary<string, Room> roomMap,
            LevelElementLibrary elementLibrary)
        {
            foreach (ScaffoldRoom scaffoldRoom in scaffold.Rooms)
            {
                if (!roomMap.TryGetValue(scaffoldRoom.RoomID, out Room room))
                {
                    continue;
                }

                foreach (ScaffoldDoorConnection connection in scaffoldRoom.Connections)
                {
                    if (!roomMap.TryGetValue(connection.TargetRoomID, out Room targetRoom))
                    {
                        continue;
                    }

                    CreateDoor(room, targetRoom, connection, elementLibrary);
                }
            }
        }

        /// <summary>
        /// Creates a door between two rooms.
        /// </summary>
        private static void CreateDoor(
            Room sourceRoom,
            Room targetRoom,
            ScaffoldDoorConnection connection,
            LevelElementLibrary elementLibrary)
        {
            GameObject doorObj;

            if (elementLibrary.DoorBasic != null)
            {
                doorObj = (GameObject)PrefabUtility.InstantiatePrefab(elementLibrary.DoorBasic);
                Undo.RegisterCreatedObjectUndo(doorObj, "Instantiate Door");
            }
            else
            {
                doorObj = new GameObject("Door");
                Undo.RegisterCreatedObjectUndo(doorObj, "Create Door");
                Undo.AddComponent<Door>(doorObj);
                Undo.AddComponent<BoxCollider2D>(doorObj);
            }

            Undo.SetTransformParent(doorObj.transform, sourceRoom.transform, "Set Door Parent");
            doorObj.transform.localPosition = connection.DoorPosition;

            Door door = doorObj.GetComponent<Door>();
            if (door == null)
            {
                door = Undo.AddComponent<Door>(doorObj);
            }

            BoxCollider2D collider = doorObj.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
                collider.size = new Vector2(1, 3);
            }

            Transform spawnPoint = CreateSpawnPoint(targetRoom, connection);

            SerializedObject serializedDoor = new SerializedObject(door);
            serializedDoor.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serializedDoor.FindProperty("_targetSpawnPoint").objectReferenceValue = spawnPoint;
            serializedDoor.FindProperty("_isLayerTransition").boolValue = connection.IsLayerTransition;
            serializedDoor.ApplyModifiedProperties();

            EditorUtility.SetDirty(door);
        }

        /// <summary>
        /// Creates a spawn point in the target room for a door.
        /// </summary>
        private static Transform CreateSpawnPoint(Room targetRoom, ScaffoldDoorConnection connection)
        {
            GameObject spawnPointObj = new GameObject("SpawnPoint");
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Create Spawn Point");
            Undo.SetTransformParent(spawnPointObj.transform, targetRoom.transform, "Set Spawn Point Parent");

            Vector3 spawnOffset = -connection.DoorDirection * 2;
            spawnPointObj.transform.localPosition = connection.DoorPosition + spawnOffset;

            return spawnPointObj.transform;
        }
    }
}
