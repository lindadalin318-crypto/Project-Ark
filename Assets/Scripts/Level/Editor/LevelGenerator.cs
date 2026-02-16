using System;
using System.Collections.Generic;
using System.Linq;
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
                GenerateRoomElements(scaffold, generatedRooms, elementLibrary);

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

        /// <summary>
        /// Generates a single element inside a room.
        /// </summary>
        private static void GenerateSingleElement(
            ScaffoldElement element,
            Room parentRoom,
            LevelElementLibrary elementLibrary,
            LevelScaffoldData scaffold,
            Dictionary<string, Room> roomMap)
        {
            GameObject prefab = elementLibrary.GetPrefabForType(element.ElementType);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelGenerator] No prefab found for element type: {element.ElementType}");
                return;
            }

            GameObject elementObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(elementObj, $"Instantiate {element.ElementType}");

            Undo.SetTransformParent(elementObj.transform, parentRoom.transform, "Set Element Parent");
            elementObj.transform.localPosition = element.LocalPosition;
            elementObj.transform.localRotation = Quaternion.Euler(0, 0, element.Rotation);
            elementObj.transform.localScale = element.Scale;

            if (element.ElementType == ScaffoldElementType.Door && !string.IsNullOrEmpty(element.BoundConnectionID))
            {
                SetupDoorFromBinding(element, elementObj, parentRoom, scaffold, roomMap);
            }

            EditorUtility.SetDirty(elementObj);
        }

        /// <summary>
        /// Sets up a Door element from its bound connection.
        /// </summary>
        private static void SetupDoorFromBinding(
            ScaffoldElement element,
            GameObject doorObj,
            Room sourceRoom,
            LevelScaffoldData scaffold,
            Dictionary<string, Room> roomMap)
        {
            ScaffoldRoom sourceScaffoldRoom = scaffold.Rooms.FirstOrDefault(r => r.RoomID == GetRoomID(sourceRoom, roomMap));
            if (sourceScaffoldRoom == null) return;

            ScaffoldDoorConnection connection = sourceScaffoldRoom.Connections.FirstOrDefault(c => c.ConnectionID == element.BoundConnectionID);
            if (connection == null) return;

            if (!roomMap.TryGetValue(connection.TargetRoomID, out Room targetRoom))
            {
                return;
            }

            Door door = doorObj.GetComponent<Door>();
            if (door == null)
            {
                door = Undo.AddComponent<Door>(doorObj);
            }

            Transform spawnPoint = CreateSpawnPoint(targetRoom, connection);

            SerializedObject serializedDoor = new SerializedObject(door);
            serializedDoor.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serializedDoor.FindProperty("_targetSpawnPoint").objectReferenceValue = spawnPoint;
            serializedDoor.FindProperty("_isLayerTransition").boolValue = connection.IsLayerTransition;

            if (element.DoorConfig != null)
            {
                serializedDoor.FindProperty("_initialState").enumValueIndex = (int)element.DoorConfig.InitialState;
                serializedDoor.FindProperty("_requiredKeyID").stringValue = element.DoorConfig.RequiredKeyID;

                if (element.DoorConfig.OpenDuringPhases != null)
                {
                    SerializedProperty phasesProp = serializedDoor.FindProperty("_openDuringPhases");
                    phasesProp.ClearArray();
                    for (int i = 0; i < element.DoorConfig.OpenDuringPhases.Length; i++)
                    {
                        phasesProp.InsertArrayElementAtIndex(i);
                        phasesProp.GetArrayElementAtIndex(i).intValue = element.DoorConfig.OpenDuringPhases[i];
                    }
                }
            }

            serializedDoor.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);
        }

        /// <summary>
        /// Gets the room ID from a Room component by looking it up in the room map.
        /// </summary>
        private static string GetRoomID(Room room, Dictionary<string, Room> roomMap)
        {
            foreach (var kvp in roomMap)
            {
                if (kvp.Value == room)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Generates all elements placed inside rooms.
        /// </summary>
        private static void GenerateRoomElements(
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

                foreach (ScaffoldElement element in scaffoldRoom.Elements)
                {
                    GenerateSingleElement(element, room, elementLibrary, scaffold, roomMap);
                }
            }
        }
    }
}
