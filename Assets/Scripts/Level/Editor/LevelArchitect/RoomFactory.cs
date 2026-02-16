using UnityEditor;
using UnityEngine;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Factory for creating fully-configured Room GameObjects from RoomPresetSO templates.
    /// Handles creation of all required child objects (Confiner, SpawnPoints, Doors, Spawner).
    /// </summary>
    public static class RoomFactory
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_PATH = "Assets/_Data/Level/Rooms/";
        private const string ROOM_PRESET_PATH = "Assets/_Data/Level/RoomPresets/";
        private const int IGNORE_RAYCAST_LAYER = 2; // Unity built-in "Ignore Raycast" layer

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Create a complete Room GameObject from a preset at the given world position.
        /// Includes BoxCollider2D, CameraConfiner, SpawnPoints, and optionally
        /// ArenaController + EnemySpawner for combat rooms.
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

            Undo.SetCurrentGroupName($"Create Room ({preset.PresetName})");

            // ── Root GameObject ──
            string roomName = GenerateRoomName(preset);
            var roomGO = new GameObject(roomName);
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

            // ── Camera Confiner Child ──
            var confinerGO = new GameObject("CameraConfiner");
            Undo.RegisterCreatedObjectUndo(confinerGO, "Create Confiner");
            confinerGO.transform.SetParent(roomGO.transform, false);
            confinerGO.layer = IGNORE_RAYCAST_LAYER;

            var polyCollider = confinerGO.AddComponent<PolygonCollider2D>();
            SetConfinerBounds(polyCollider, preset.DefaultSize);

            // ── SpawnPoints Container ──
            var spawnPointsGO = new GameObject("SpawnPoints");
            Undo.RegisterCreatedObjectUndo(spawnPointsGO, "Create SpawnPoints");
            spawnPointsGO.transform.SetParent(roomGO.transform, false);

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
                var spawnerGO = new GameObject("EnemySpawner");
                Undo.RegisterCreatedObjectUndo(spawnerGO, "Create EnemySpawner");
                spawnerGO.transform.SetParent(roomGO.transform, false);

                spawner = spawnerGO.AddComponent<EnemySpawner>();
            }

            // ── RoomSO Asset ──
            RoomSO roomSO = CreateRoomSOForPreset(preset, roomName);

            // ── Configure Room via SerializedObject ──
            ConfigureRoom(room, roomSO, polyCollider, spawnPoints);

            // ── Configure EnemySpawner spawn points ──
            if (spawner != null && spawnPoints.Length > 0)
            {
                ConfigureSpawner(spawner, spawnPoints);
            }

            // Mark scene dirty
            EditorUtility.SetDirty(roomGO);

            Selection.activeGameObject = roomGO;
            SceneView.RepaintAll();

            Debug.Log($"[RoomFactory] Created room '{roomName}' from preset '{preset.PresetName}' at {position}");

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
            serialized.FindProperty("_roomType").enumValueIndex = (int)room.Type;

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

        // ──────────────────── Private Helpers ────────────────────

        private static string GenerateRoomName(RoomPresetSO preset)
        {
            string prefix = preset.RoomTypeValue.ToString();
            string timestamp = System.DateTime.Now.ToString("HHmmss");
            return $"Room_{prefix}_{timestamp}";
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

        private static RoomSO CreateRoomSOForPreset(RoomPresetSO preset, string roomName)
        {
            var roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{roomName}_Data";

            var serialized = new SerializedObject(roomSO);
            serialized.FindProperty("_roomID").stringValue = roomName;
            serialized.FindProperty("_displayName").stringValue = roomName;
            serialized.FindProperty("_type").enumValueIndex = (int)preset.RoomTypeValue;
            serialized.FindProperty("_floorLevel").intValue = 0;

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
