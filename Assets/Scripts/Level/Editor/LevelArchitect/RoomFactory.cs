using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Factory for creating fully-configured Room GameObjects from RoomPresetSO templates.
    /// Handles creation of all required child objects (Confiner, SpawnPoints, Doors, Spawner).
    /// </summary>
    public static class RoomFactory
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_PATH = "Assets/_Data/Level/Rooms/";
        private const string ROOM_PRESET_PATH = "Assets/_Data/Level/RoomPresets/";
        private const int IGNORE_RAYCAST_LAYER = 2; // Unity built-in "Ignore Raycast" layer
        private const string NAVIGATION_ROOT_NAME = "Navigation";
        private const string ELEMENTS_ROOT_NAME = "Elements";
        private const string ENCOUNTERS_ROOT_NAME = "Encounters";
        private const string HAZARDS_ROOT_NAME = "Hazards";
        private const string DECORATION_ROOT_NAME = "Decoration";
        private const string TRIGGERS_ROOT_NAME = "Triggers";

        public readonly struct StableRoomIdentity
        {
            public StableRoomIdentity(string roomId, string displayName)
            {
                RoomId = roomId;
                DisplayName = displayName;
            }

            public string RoomId { get; }
            public string DisplayName { get; }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Create a complete Room GameObject from a preset at the given world position.
        /// Includes BoxCollider2D, standard child roots, spawn points, and optionally
        /// CameraConfiner / ArenaController / EnemySpawner when the preset semantics need them.
        /// Also auto-creates a RoomSO asset.
        /// </summary>

        public static Room CreateRoomFromPreset(RoomPresetSO preset, Vector3 position)
        {
            if (preset == null)
            {
                Debug.LogError("[RoomFactory] Cannot create room: preset is null.");
                return null;
            }

            EnsureDirectoryExists(ROOM_DATA_PATH);

            const int defaultFloorLevel = 0;
            StableRoomIdentity identity = GenerateStableIdentity(preset.NodeTypeValue, defaultFloorLevel);

            Undo.SetCurrentGroupName($"Create Room ({preset.PresetName})");

            // ── Root GameObject ──
            var roomGO = new GameObject(identity.RoomId);
            Undo.RegisterCreatedObjectUndo(roomGO, "Create Room");

            roomGO.transform.position = position;

            // ── Room Component ──
            var room = roomGO.AddComponent<Room>();

            // ── BoxCollider2D (Trigger) ──
            var boxCollider = roomGO.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
                boxCollider = roomGO.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = preset.DefaultSize;

            RoomCameraPolicy cameraPolicy = GetDefaultCameraPolicy(preset.NodeTypeValue);

            // ── Standard hierarchy roots (Batch 2) ──
            var navigationRoot = CreateChildObject(roomGO.transform, NAVIGATION_ROOT_NAME);
            CreateChildObject(roomGO.transform, ELEMENTS_ROOT_NAME);
            var encountersRoot = CreateChildObject(roomGO.transform, ENCOUNTERS_ROOT_NAME);
            CreateChildObject(roomGO.transform, HAZARDS_ROOT_NAME);
            CreateChildObject(roomGO.transform, DECORATION_ROOT_NAME);
            CreateChildObject(roomGO.transform, TRIGGERS_ROOT_NAME);

            // ── Optional Camera Confiner Child ──
            PolygonCollider2D polyCollider = null;
            if (cameraPolicy == RoomCameraPolicy.HardConfine)
            {
                var confinerGO = CreateChildObject(roomGO.transform, "CameraConfiner");
                confinerGO.layer = IGNORE_RAYCAST_LAYER;

                polyCollider = confinerGO.AddComponent<PolygonCollider2D>();
                polyCollider.isTrigger = true;
                SetConfinerBounds(polyCollider, preset.DefaultSize);
            }


            // ── Standard Navigation placeholders ──
            CreateChildObject(navigationRoot.transform, "Doors");
            CreateChildObject(navigationRoot.transform, "SpawnPoints");

            // ── SpawnPoints Container ──
            var spawnPointsGO = CreateChildObject(encountersRoot.transform, "SpawnPoints");
            Transform[] spawnPoints = CreateSpawnPoints(spawnPointsGO.transform, preset);

            // ── ArenaController (for Arena/Boss) ──
            if (preset.IncludeArenaController)
            {
                roomGO.AddComponent<ArenaController>();
            }

            // ── EnemySpawner (for combat rooms) ──
            EnemySpawner spawner = null;
            if (preset.IncludeEnemySpawner)
            {
                var spawnerGO = CreateChildObject(encountersRoot.transform, "EnemySpawner");
                spawner = spawnerGO.AddComponent<EnemySpawner>();
            }

            // ── RoomSO Asset ──
            RoomSO roomSO = CreateRoomSOForPreset(preset, identity, defaultFloorLevel);

            // ── Configure Room via SerializedObject ──
            ConfigureRoom(room, roomSO, cameraPolicy, polyCollider, spawnPoints);


            // ── Configure EnemySpawner spawn points ──
            if (spawner != null && spawnPoints.Length > 0)
            {
                ConfigureSpawner(spawner, spawnPoints);
            }

            // Mark scene dirty
            EditorUtility.SetDirty(roomGO);

            Selection.activeGameObject = roomGO;
            SceneView.RepaintAll();

            Debug.Log($"[RoomFactory] Created room '{identity.RoomId}' from preset '{preset.PresetName}' at {position}");

            return room;
        }

        /// <summary>
        /// Create a Room from a preset with a custom size (overrides preset default).
        /// Used by Blockout Mode brush tools.
        /// </summary>
        public static Room CreateRoomFromPreset(RoomPresetSO preset, Vector3 position, Vector2 customSize)
        {
            var room = CreateRoomFromPreset(preset, position);
            if (room == null) return null;

            // Override size
            var box = room.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Undo.RecordObject(box, "Resize Room");
                box.size = customSize;
            }

            // Update confiner
            var confiner = room.transform.Find("CameraConfiner");
            if (confiner != null)
            {
                var poly = confiner.GetComponent<PolygonCollider2D>();
                if (poly != null)
                {
                    Undo.RecordObject(poly, "Resize Confiner");
                    SetConfinerBounds(poly, customSize);
                }
            }

            return room;
        }

        /// <summary>
        /// Duplicate an authored room in-scene, clone its RoomSO asset,
        /// and strip existing door links so the copy can be rewired safely.
        /// </summary>
        public static Room DuplicateRoom(Room sourceRoom, Vector3 position)
        {
            if (sourceRoom == null)
            {
                Debug.LogError("[RoomFactory] Cannot duplicate room: source is null.");
                return null;
            }

            EnsureDirectoryExists(ROOM_DATA_PATH);
            Undo.SetCurrentGroupName($"Duplicate Room ({sourceRoom.RoomID})");

            var duplicateGO = Object.Instantiate(sourceRoom.gameObject, position, sourceRoom.transform.rotation, sourceRoom.transform.parent);
            Undo.RegisterCreatedObjectUndo(duplicateGO, "Duplicate Room");
            duplicateGO.transform.position = position;

            var duplicateRoom = duplicateGO.GetComponent<Room>();
            if (duplicateRoom == null)
            {
                Debug.LogError("[RoomFactory] Duplicated object is missing Room component.");
                return null;
            }

            RoomNodeType duplicateNodeType = sourceRoom.Data != null ? sourceRoom.Data.NodeType : RoomNodeType.Transit;
            int duplicateFloorLevel = sourceRoom.Data != null ? sourceRoom.Data.FloorLevel : 0;
            StableRoomIdentity identity = GenerateStableIdentity(duplicateNodeType, duplicateFloorLevel);
            duplicateGO.name = identity.RoomId;

            var duplicatedRoomSO = DuplicateRoomDataAsset(sourceRoom.Data, identity.RoomId, identity.DisplayName);
            if (duplicatedRoomSO != null)
            {
                var roomSerialized = new SerializedObject(duplicateRoom);
                roomSerialized.FindProperty("_data").objectReferenceValue = duplicatedRoomSO;
                roomSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(duplicateRoom);
            }

            ClearDuplicatedDoorAuthoring(duplicateRoom);

            EditorUtility.SetDirty(duplicateGO);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = duplicateGO;
            SceneView.RepaintAll();

            Debug.Log($"[RoomFactory] Duplicated room '{sourceRoom.RoomID}' as '{identity.RoomId}'.");
            return duplicateRoom;
        }

        /// <summary>
        /// Save an existing Room's configuration as a new RoomPresetSO asset.
        /// </summary>
        public static RoomPresetSO SaveRoomAsPreset(Room room, string presetName)
        {
            if (room == null)
            {
                Debug.LogError("[RoomFactory] Cannot save preset: room is null.");
                return null;
            }

            EnsureDirectoryExists(ROOM_PRESET_PATH);

            var preset = ScriptableObject.CreateInstance<RoomPresetSO>();

            var serialized = new SerializedObject(preset);
            serialized.FindProperty("_presetName").stringValue = presetName;
            serialized.FindProperty("_nodeType").enumValueIndex = (int)room.NodeType;

            var box = room.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                serialized.FindProperty("_defaultSize").vector2Value = box.size;
            }

            var spawnPoints = room.SpawnPoints;
            serialized.FindProperty("_spawnPointCount").intValue = spawnPoints != null ? spawnPoints.Length : 0;

            var arena = room.GetComponent<ArenaController>();
            serialized.FindProperty("_includeArenaController").boolValue = arena != null;

            var spawner = room.GetComponentInChildren<EnemySpawner>(true);
            serialized.FindProperty("_includeEnemySpawner").boolValue = spawner != null;

            if (room.Data != null && room.Data.Encounter != null)
            {
                serialized.FindProperty("_defaultEncounter").objectReferenceValue = room.Data.Encounter;
            }

            serialized.FindProperty("_description").stringValue = $"Saved from room '{room.RoomID}'";

            serialized.ApplyModifiedPropertiesWithoutUndo();

            string sanitized = presetName.Replace(" ", "_");
            string path = $"{ROOM_PRESET_PATH}Preset_{sanitized}.asset";
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RoomFactory] Saved room preset to {path}");

            return preset;
        }

        /// <summary>
        /// Find all RoomPresetSO assets in the project.
        /// </summary>
        public static RoomPresetSO[] FindAllPresets()
        {
            var guids = AssetDatabase.FindAssets("t:RoomPresetSO");
            var presets = new RoomPresetSO[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                presets[i] = AssetDatabase.LoadAssetAtPath<RoomPresetSO>(path);
            }

            return presets;
        }

        /// <summary>
        /// Create the built-in presets if they don't exist.
        /// Returns the default preset used by Blockout mode.
        /// </summary>
        public static RoomPresetSO CreateBuiltInPresets()
        {
            string path = ROOM_PRESET_PATH;
            EnsureDirectoryExists(path);

            RoomPresetSO normalPreset = null;

            normalPreset = CreatePresetIfMissing(path, "Preset_Safe", "Safe Room",
                "A safe zone with no enemies. May contain checkpoint, shop, or NPC.",
                RoomNodeType.Safe, new Vector2(15, 12), 2, false, false);

            var transit = CreatePresetIfMissing(path, "Preset_Transit", "Transit Room",
                "Connecting room focused on traversal and flow, with only light incidental threats.",
                RoomNodeType.Transit, new Vector2(20, 15), 4, false, false);
            if (transit != null) normalPreset = transit;

            CreatePresetIfMissing(path, "Preset_Combat", "Combat Room",
                "Open combat room focused on mobile skirmishes while traversal continues.",
                RoomNodeType.Combat, new Vector2(22, 16), 5, false, true);

            CreatePresetIfMissing(path, "Preset_Reward", "Reward Room",
                "Low-pressure payoff room for pickups, post-encounter relief, or one-off rewards.",
                RoomNodeType.Reward, new Vector2(18, 12), 2, false, false);

            CreatePresetIfMissing(path, "Preset_Arena", "Arena Room",
                "Closed combat room — doors lock on entry, unlock after all waves cleared.",
                RoomNodeType.Arena, new Vector2(25, 20), 6, true, true);

            CreatePresetIfMissing(path, "Preset_Boss", "Boss Room",
                "Boss encounter room — larger than arena, special rewards on clear.",
                RoomNodeType.Boss, new Vector2(35, 25), 6, true, true);

            CreatePresetIfMissing(path, "Preset_Corridor", "Corridor",
                "Narrow connecting passage between rooms.",
                RoomNodeType.Transit, new Vector2(15, 3), 0, false, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return normalPreset;
        }

        public static StableRoomIdentity GenerateStableIdentity(RoomNodeType nodeType, int floorLevel, Room excludedRoom = null)
        {
            string floorToken = GetFloorToken(floorLevel);
            string nodeToken = GetNodeTypeToken(nodeType);
            int index = 1;

            while (true)
            {
                string roomId = $"{floorToken}_{nodeToken}_{index:00}";
                if (!IsRoomIdentityInUse(roomId, excludedRoom))
                {
                    return new StableRoomIdentity(roomId, $"{nodeToken} {floorToken}-{index:00}");
                }

                index++;
            }
        }

        public static bool ApplyStableIdentity(Room room)
        {
            if (room == null || room.Data == null)
            {
                return false;
            }

            StableRoomIdentity identity = GenerateStableIdentity(room.Data.NodeType, room.Data.FloorLevel, room);

            Undo.RecordObject(room.Data, "Stable Rename Room");
            var serialized = new SerializedObject(room.Data);
            serialized.FindProperty("_roomID").stringValue = identity.RoomId;
            serialized.FindProperty("_displayName").stringValue = identity.DisplayName;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(room.Data);

            Undo.RecordObject(room.gameObject, "Stable Rename Room");
            room.gameObject.name = identity.RoomId;
            EditorUtility.SetDirty(room.gameObject);

            string assetPath = AssetDatabase.GetAssetPath(room.Data);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string desiredAssetName = $"{identity.RoomId}_Data";
                string currentAssetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!string.Equals(currentAssetName, desiredAssetName, StringComparison.Ordinal))
                {
                    AssetDatabase.RenameAsset(assetPath, desiredAssetName);
                }
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        public static Room[] CreateFiveRoomValidationSlice(Vector3 anchorPosition)
        {
            CreateBuiltInPresets();

            RoomPresetSO safePreset = FindBuiltInPreset("Safe Room");
            RoomPresetSO transitPreset = FindBuiltInPreset("Transit Room");
            RoomPresetSO combatPreset = FindBuiltInPreset("Combat Room");
            RoomPresetSO rewardPreset = FindBuiltInPreset("Reward Room");
            if (safePreset == null || transitPreset == null || combatPreset == null || rewardPreset == null)
            {
                Debug.LogError("[RoomFactory] Cannot create validation slice: required built-in presets are missing.");
                return Array.Empty<Room>();
            }

            var rooms = new List<Room>(5)
            {
                CreateRoomFromPreset(safePreset, anchorPosition + new Vector3(0f, 0f, 0f)),
                CreateRoomFromPreset(transitPreset, anchorPosition + new Vector3(26f, 0f, 0f)),
                CreateRoomFromPreset(combatPreset, anchorPosition + new Vector3(52f, 0f, 0f)),
                CreateRoomFromPreset(rewardPreset, anchorPosition + new Vector3(52f, -22f, 0f)),
                CreateRoomFromPreset(transitPreset, anchorPosition + new Vector3(26f, -22f, 0f))
            };

            rooms.RemoveAll(room => room == null);
            if (rooms.Count != 5)
            {
                Debug.LogError("[RoomFactory] Validation slice creation aborted: one or more rooms failed to instantiate.");
                return rooms.ToArray();
            }

            SetConnectionType(DoorWiringService.AutoConnectRooms(rooms[0], rooms[1]), ConnectionType.Progression);
            SetConnectionType(DoorWiringService.AutoConnectRooms(rooms[1], rooms[2]), ConnectionType.Progression);
            SetConnectionType(DoorWiringService.AutoConnectRooms(rooms[2], rooms[3]), ConnectionType.Challenge);
            SetConnectionType(DoorWiringService.AutoConnectRooms(rooms[3], rooms[4]), ConnectionType.Return);
            SetConnectionType(DoorWiringService.AutoConnectRooms(rooms[4], rooms[0]), ConnectionType.Return);

            TrySetEntryRoom(rooms[0]);
            Selection.activeGameObject = rooms[0].gameObject;
            SceneView.RepaintAll();

            Debug.Log("[RoomFactory] Created 5-room validation slice (Safe → Transit → Combat → Reward → Transit return loop).");
            return rooms.ToArray();
        }

        private static RoomPresetSO CreatePresetIfMissing(string basePath, string fileName, string presetName,
            string description, RoomNodeType nodeType, Vector2 defaultSize, int spawnPoints,
            bool includeArena, bool includeSpawner)
        {
            string fullPath = $"{basePath}{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoomPresetSO>(fullPath);
            if (existing != null) return existing;

            var preset = ScriptableObject.CreateInstance<RoomPresetSO>();
            var serialized = new SerializedObject(preset);
            serialized.FindProperty("_presetName").stringValue = presetName;
            serialized.FindProperty("_description").stringValue = description;
            serialized.FindProperty("_nodeType").enumValueIndex = (int)nodeType;
            serialized.FindProperty("_defaultSize").vector2Value = defaultSize;
            serialized.FindProperty("_spawnPointCount").intValue = spawnPoints;
            serialized.FindProperty("_includeArenaController").boolValue = includeArena;
            serialized.FindProperty("_includeEnemySpawner").boolValue = includeSpawner;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(preset, fullPath);
            Debug.Log($"[RoomFactory] Created built-in preset: {fullPath}");
            return preset;
        }

        // ──────────────────── Private Helpers ────────────────────

        private static RoomPresetSO FindBuiltInPreset(string presetName)
        {
            var presets = FindAllPresets();
            foreach (var preset in presets)
            {
                if (preset == null)
                {
                    continue;
                }

                if (string.Equals(preset.PresetName, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            return null;
        }

        private static void TrySetEntryRoom(Room room)
        {
            if (room == null)
            {
                return;
            }

            var roomManager = Object.FindAnyObjectByType<RoomManager>();
            if (roomManager == null)
            {
                return;
            }

            Undo.RecordObject(roomManager, "Set Entry Room");
            var serialized = new SerializedObject(roomManager);
            serialized.FindProperty("_startingRoom").objectReferenceValue = room;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(roomManager);
        }

        private static void SetConnectionType((Door doorA, Door doorB) connectionPair, ConnectionType connectionType)
        {
            if (connectionPair.doorA == null)
            {
                return;
            }

            DoorWiringService.SetConnectionType(connectionPair.doorA, connectionType);
        }

        private static bool IsRoomIdentityInUse(string roomId, Room excludedRoom)
        {
            Room existingRoom = FindSceneRoomById(roomId, excludedRoom);
            if (existingRoom != null)
            {
                return true;
            }

            var existingAsset = AssetDatabase.LoadAssetAtPath<RoomSO>($"{ROOM_DATA_PATH}{roomId}_Data.asset");
            return existingAsset != null && (excludedRoom == null || existingAsset != excludedRoom.Data);
        }

        private static Room FindSceneRoomById(string roomId, Room excludedRoom = null)
        {
            var rooms = Object.FindObjectsByType<Room>();
            foreach (var room in rooms)
            {
                if (room == null || room == excludedRoom)
                {
                    continue;
                }

                if (string.Equals(room.RoomID, roomId, StringComparison.OrdinalIgnoreCase))
                {
                    return room;
                }
            }

            return null;
        }

        private static string GetFloorToken(int floorLevel)
        {
            if (floorLevel == 0)
            {
                return "G";
            }

            return floorLevel > 0 ? $"U{floorLevel}" : $"D{Mathf.Abs(floorLevel)}";
        }

        private static string GetNodeTypeToken(RoomNodeType nodeType)
        {
            return nodeType.ToString();
        }

        private static RoomSO DuplicateRoomDataAsset(RoomSO sourceData, string roomId, string displayName)
        {
            string desiredPath = AssetDatabase.GenerateUniqueAssetPath($"{ROOM_DATA_PATH}{roomId}_Data.asset");
            RoomSO duplicatedRoomSO = null;

            string sourceAssetPath = sourceData != null ? AssetDatabase.GetAssetPath(sourceData) : string.Empty;
            if (!string.IsNullOrEmpty(sourceAssetPath) && AssetDatabase.CopyAsset(sourceAssetPath, desiredPath))
            {
                duplicatedRoomSO = AssetDatabase.LoadAssetAtPath<RoomSO>(desiredPath);
            }

            if (duplicatedRoomSO == null)
            {
                duplicatedRoomSO = ScriptableObject.CreateInstance<RoomSO>();
                AssetDatabase.CreateAsset(duplicatedRoomSO, desiredPath);
            }

            var serialized = new SerializedObject(duplicatedRoomSO);
            serialized.FindProperty("_roomID").stringValue = roomId;
            serialized.FindProperty("_displayName").stringValue = string.IsNullOrWhiteSpace(displayName) ? roomId : displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(duplicatedRoomSO);
            return duplicatedRoomSO;
        }

        private static void ClearDuplicatedDoorAuthoring(Room room)
        {
            if (room == null) return;

            var navigationDoorsRoot = room.transform.Find("Navigation/Doors");
            if (navigationDoorsRoot != null)
            {
                for (int i = navigationDoorsRoot.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(navigationDoorsRoot.GetChild(i).gameObject);
                }
            }

            var navigationSpawnRoot = room.transform.Find("Navigation/SpawnPoints");
            if (navigationSpawnRoot != null)
            {
                for (int i = navigationSpawnRoot.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(navigationSpawnRoot.GetChild(i).gameObject);
                }
            }
        }

        private static GameObject CreateChildObject(Transform parent, string name)
        {
            var child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SetConfinerBounds(PolygonCollider2D poly, Vector2 size)
        {
            // Slightly smaller than room bounds to keep camera inside
            float margin = 0.1f;
            float hw = (size.x / 2f) - margin;
            float hh = (size.y / 2f) - margin;

            poly.points = new Vector2[]
            {
                new Vector2(-hw, -hh),
                new Vector2( hw, -hh),
                new Vector2( hw,  hh),
                new Vector2(-hw,  hh)
            };
        }

        private static Transform[] CreateSpawnPoints(Transform parent, RoomPresetSO preset)
        {
            int count = preset.SpawnPointCount;
            if (count <= 0) return new Transform[0];

            var points = new Transform[count];
            Vector2 halfSize = preset.DefaultSize * 0.35f; // 35% inset from edges

            for (int i = 0; i < count; i++)
            {
                var spGO = new GameObject($"SpawnPoint_{i}");
                spGO.transform.SetParent(parent, false);

                // Distribute spawn points evenly inside the room
                float angle = (2f * Mathf.PI * i) / count;
                float x = Mathf.Cos(angle) * halfSize.x;
                float y = Mathf.Sin(angle) * halfSize.y;
                spGO.transform.localPosition = new Vector3(x, y, 0);

                points[i] = spGO.transform;
            }

            return points;
        }

        private static RoomSO CreateRoomSOForPreset(RoomPresetSO preset, StableRoomIdentity identity, int floorLevel)
        {
            var roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{identity.RoomId}_Data";

            var serialized = new SerializedObject(roomSO);
            serialized.FindProperty("_roomID").stringValue = identity.RoomId;
            serialized.FindProperty("_displayName").stringValue = identity.DisplayName;
            serialized.FindProperty("_nodeType").enumValueIndex = (int)preset.NodeTypeValue;
            serialized.FindProperty("_floorLevel").intValue = floorLevel;

            if (preset.DefaultEncounter != null)
            {
                serialized.FindProperty("_encounter").objectReferenceValue = preset.DefaultEncounter;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ROOM_DATA_PATH}{roomSO.name}.asset";
            AssetDatabase.CreateAsset(roomSO, path);
            AssetDatabase.SaveAssets();

            return roomSO;
        }

        private static void ConfigureRoom(Room room, RoomSO roomSO, Collider2D confinerBounds, Transform[] spawnPoints)
        {
            var serialized = new SerializedObject(room);

            serialized.FindProperty("_data").objectReferenceValue = roomSO;
            serialized.FindProperty("_confinerBounds").objectReferenceValue = confinerBounds;

            // Set spawn points array
            var spProp = serialized.FindProperty("_spawnPoints");
            spProp.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            // Set player layer to Layer 6 ("Player") if it exists, otherwise use default
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;
            }

            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureSpawner(EnemySpawner spawner, Transform[] spawnPoints)
        {
            var serialized = new SerializedObject(spawner);

            var spProp = serialized.FindProperty("_spawnPoints");
            spProp.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            serialized.ApplyModifiedProperties();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                // Create folder hierarchy
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
