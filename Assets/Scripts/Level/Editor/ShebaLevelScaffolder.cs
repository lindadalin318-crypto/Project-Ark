#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Editor tool that scaffolds the Sheba level with ~12 rooms, RoomSO assets,
    /// EncounterSO assets, and Door connections. Provides a starting framework
    /// the designer can then customize (paint tilemaps, adjust sizes, add visuals).
    /// 
    /// Menu: ProjectArk > Scaffold Sheba Level
    /// </summary>
    public static class ShebaLevelScaffolder
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_PATH = "Assets/_Data/Level/Rooms/Sheba/";
        private const string ENCOUNTER_DATA_PATH = "Assets/_Data/Level/Encounters/Sheba/";
        private const string MENU_NAME = "ProjectArk/Scaffold Sheba Level";

        private const float ROOM_SPACING = 25f;
        private const float ROOM_DEFAULT_WIDTH = 20f;
        private const float ROOM_DEFAULT_HEIGHT = 15f;
        private const float BOSS_ROOM_WIDTH = 30f;
        private const float BOSS_ROOM_HEIGHT = 25f;

        // ──────────────────── Room Definitions ────────────────────

        private struct RoomDef
        {
            public string ID;
            public string DisplayName;
            public RoomType Type;
            public int Floor;
            public Vector2 Position;
            public Vector2 Size;
            public bool HasEncounter;
            public string EncounterName;
        }

        private struct DoorDef
        {
            public string FromRoomID;
            public string ToRoomID;
            public bool IsLayerTransition;
        }

        // ──────────────────── Menu Entry ────────────────────

        [MenuItem(MENU_NAME)]
        public static void ScaffoldLevel()
        {
            if (!EditorUtility.DisplayDialog(
                    "Scaffold Sheba Level",
                    "This will create ~12 Room GameObjects in the scene and ~12 RoomSO + ~5 EncounterSO assets.\n\n" +
                    "Existing assets with matching paths will be overwritten.\n\nProceed?",
                    "Scaffold", "Cancel"))
            {
                return;
            }

            // Ensure directories exist
            EnsureDirectory(ROOM_DATA_PATH);
            EnsureDirectory(ENCOUNTER_DATA_PATH);

            // Define rooms
            var rooms = DefineRooms();
            var doors = DefineDoors();

            // Create assets
            var roomSOLookup = CreateRoomSOAssets(rooms);
            CreateEncounterSOAssets(rooms, roomSOLookup);

            // Create scene hierarchy
            var roomGOLookup = CreateRoomGameObjects(rooms, roomSOLookup);
            WireDoorConnections(doors, roomGOLookup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Print checklist
            PrintChecklist();

            Debug.Log("[ShebaLevelScaffolder] Scaffolding complete! See Console for post-setup checklist.");
        }

        // ──────────────────── Room Definitions ────────────────────

        private static List<RoomDef> DefineRooms()
        {
            return new List<RoomDef>
            {
                // Floor 0 (Surface)
                new()
                {
                    ID = "sheba_entrance", DisplayName = "Entrance",
                    Type = RoomType.Safe, Floor = 0,
                    Position = new Vector2(0, 0),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = false
                },
                new()
                {
                    ID = "sheba_hub", DisplayName = "Central Hub",
                    Type = RoomType.Safe, Floor = 0,
                    Position = new Vector2(ROOM_SPACING, 0),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH * 1.5f, ROOM_DEFAULT_HEIGHT * 1.5f),
                    HasEncounter = false
                },
                new()
                {
                    ID = "sheba_corridor_a", DisplayName = "Corridor A",
                    Type = RoomType.Normal, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 2, ROOM_SPACING * 0.5f),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Patrol_Light"
                },
                new()
                {
                    ID = "sheba_corridor_b", DisplayName = "Corridor B",
                    Type = RoomType.Normal, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 2, -ROOM_SPACING * 0.5f),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Patrol_Light"
                },
                new()
                {
                    ID = "sheba_corridor_c", DisplayName = "Corridor C",
                    Type = RoomType.Normal, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 3, 0),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Mixed_Medium"
                },
                new()
                {
                    ID = "sheba_arena_01", DisplayName = "Arena 01",
                    Type = RoomType.Arena, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 3, ROOM_SPACING),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH * 1.2f, ROOM_DEFAULT_HEIGHT * 1.2f),
                    HasEncounter = true, EncounterName = "Encounter_Arena_Heavy"
                },
                new()
                {
                    ID = "sheba_arena_02", DisplayName = "Arena 02",
                    Type = RoomType.Arena, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 3, -ROOM_SPACING),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH * 1.2f, ROOM_DEFAULT_HEIGHT * 1.2f),
                    HasEncounter = true, EncounterName = "Encounter_Arena_Heavy"
                },
                new()
                {
                    ID = "sheba_safe_01", DisplayName = "Rest Station",
                    Type = RoomType.Safe, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 4, 0),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH * 0.8f, ROOM_DEFAULT_HEIGHT * 0.8f),
                    HasEncounter = false
                },
                new()
                {
                    ID = "sheba_key_chamber", DisplayName = "Key Chamber",
                    Type = RoomType.Arena, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 4, ROOM_SPACING),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Mixed_Medium"
                },
                new()
                {
                    ID = "sheba_boss_antechamber", DisplayName = "Boss Antechamber",
                    Type = RoomType.Normal, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 5, 0),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Patrol_Light"
                },
                new()
                {
                    ID = "sheba_boss", DisplayName = "Boss Chamber",
                    Type = RoomType.Boss, Floor = 0,
                    Position = new Vector2(ROOM_SPACING * 6, 0),
                    Size = new Vector2(BOSS_ROOM_WIDTH, BOSS_ROOM_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Boss"
                },
                // Floor -1 (Underground)
                new()
                {
                    ID = "sheba_underground_01", DisplayName = "Underground Passage",
                    Type = RoomType.Normal, Floor = -1,
                    Position = new Vector2(ROOM_SPACING, -ROOM_SPACING * 2),
                    Size = new Vector2(ROOM_DEFAULT_WIDTH * 1.3f, ROOM_DEFAULT_HEIGHT),
                    HasEncounter = true, EncounterName = "Encounter_Mixed_Medium"
                }
            };
        }

        private static List<DoorDef> DefineDoors()
        {
            return new List<DoorDef>
            {
                new() { FromRoomID = "sheba_entrance", ToRoomID = "sheba_hub" },
                new() { FromRoomID = "sheba_hub", ToRoomID = "sheba_corridor_a" },
                new() { FromRoomID = "sheba_hub", ToRoomID = "sheba_corridor_b" },
                new() { FromRoomID = "sheba_corridor_a", ToRoomID = "sheba_corridor_c" },
                new() { FromRoomID = "sheba_corridor_b", ToRoomID = "sheba_corridor_c" },
                new() { FromRoomID = "sheba_corridor_a", ToRoomID = "sheba_arena_01" },
                new() { FromRoomID = "sheba_corridor_b", ToRoomID = "sheba_arena_02" },
                new() { FromRoomID = "sheba_corridor_c", ToRoomID = "sheba_safe_01" },
                new() { FromRoomID = "sheba_arena_01", ToRoomID = "sheba_key_chamber" },
                new() { FromRoomID = "sheba_safe_01", ToRoomID = "sheba_boss_antechamber" },
                new() { FromRoomID = "sheba_boss_antechamber", ToRoomID = "sheba_boss" },
                // Layer transition to underground
                new() { FromRoomID = "sheba_hub", ToRoomID = "sheba_underground_01", IsLayerTransition = true },
            };
        }

        // ──────────────────── Asset Creation ────────────────────

        private static Dictionary<string, RoomSO> CreateRoomSOAssets(List<RoomDef> rooms)
        {
            var lookup = new Dictionary<string, RoomSO>();

            foreach (var def in rooms)
            {
                string path = $"{ROOM_DATA_PATH}{def.ID}.asset";
                var so = AssetDatabase.LoadAssetAtPath<RoomSO>(path);

                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<RoomSO>();
                    AssetDatabase.CreateAsset(so, path);
                }

                // Set fields via SerializedObject
                var serialized = new SerializedObject(so);
                serialized.FindProperty("_roomID").stringValue = def.ID;
                serialized.FindProperty("_displayName").stringValue = def.DisplayName;
                serialized.FindProperty("_floorLevel").intValue = def.Floor;
                serialized.FindProperty("_type").enumValueIndex = (int)def.Type;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                lookup[def.ID] = so;
            }

            return lookup;
        }

        private static void CreateEncounterSOAssets(List<RoomDef> rooms, Dictionary<string, RoomSO> roomSOLookup)
        {
            // Create distinct encounter configs
            var encounterNames = new HashSet<string>();
            foreach (var def in rooms)
            {
                if (def.HasEncounter && !string.IsNullOrEmpty(def.EncounterName))
                    encounterNames.Add(def.EncounterName);
            }

            var encounterLookup = new Dictionary<string, EncounterSO>();
            foreach (var name in encounterNames)
            {
                string path = $"{ENCOUNTER_DATA_PATH}{name}.asset";
                var so = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);

                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<EncounterSO>();
                    AssetDatabase.CreateAsset(so, path);

                    // Set up placeholder waves via SerializedObject
                    var serialized = new SerializedObject(so);
                    var wavesProperty = serialized.FindProperty("_waves");

                    int waveCount = GetWaveCountForEncounter(name);
                    wavesProperty.arraySize = waveCount;

                    for (int i = 0; i < waveCount; i++)
                    {
                        var wave = wavesProperty.GetArrayElementAtIndex(i);
                        wave.FindPropertyRelative("DelayBeforeWave").floatValue = i == 0 ? 0f : 1.5f;
                        var entries = wave.FindPropertyRelative("Entries");
                        entries.arraySize = 1;
                        var entry = entries.GetArrayElementAtIndex(0);
                        entry.FindPropertyRelative("Count").intValue = GetEnemyCountForEncounter(name, i);
                        // EnemyPrefab left null — user must assign
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                encounterLookup[name] = so;
            }

            // Wire encounters to RoomSOs
            foreach (var def in rooms)
            {
                if (!def.HasEncounter || string.IsNullOrEmpty(def.EncounterName)) continue;
                if (!roomSOLookup.TryGetValue(def.ID, out var roomSO)) continue;
                if (!encounterLookup.TryGetValue(def.EncounterName, out var encounterSO)) continue;

                var serialized = new SerializedObject(roomSO);
                serialized.FindProperty("_encounter").objectReferenceValue = encounterSO;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static int GetWaveCountForEncounter(string name)
        {
            return name switch
            {
                "Encounter_Patrol_Light" => 1,
                "Encounter_Mixed_Medium" => 2,
                "Encounter_Arena_Heavy" => 3,
                "Encounter_Boss" => 1,
                _ => 1
            };
        }

        private static int GetEnemyCountForEncounter(string name, int waveIndex)
        {
            return name switch
            {
                "Encounter_Patrol_Light" => 2,
                "Encounter_Mixed_Medium" => waveIndex == 0 ? 3 : 4,
                "Encounter_Arena_Heavy" => waveIndex switch { 0 => 3, 1 => 4, _ => 5 },
                "Encounter_Boss" => 1,
                _ => 2
            };
        }

        // ──────────────────── Scene Hierarchy ────────────────────

        private static Dictionary<string, GameObject> CreateRoomGameObjects(
            List<RoomDef> rooms, Dictionary<string, RoomSO> roomSOLookup)
        {
            // Create a parent for organization
            var levelRoot = new GameObject("--- Sheba Level ---");
            Undo.RegisterCreatedObjectUndo(levelRoot, "Scaffold Sheba Level");

            var lookup = new Dictionary<string, GameObject>();

            foreach (var def in rooms)
            {
                var roomGO = new GameObject($"Room_{def.ID}");
                roomGO.transform.SetParent(levelRoot.transform);
                roomGO.transform.position = new Vector3(def.Position.x, def.Position.y, 0f);

                // BoxCollider2D (trigger for player detection)
                var boxCol = roomGO.AddComponent<BoxCollider2D>();
                boxCol.isTrigger = true;
                boxCol.size = def.Size;

                // PolygonCollider2D for camera confiner (non-trigger, child object)
                var confinerChild = new GameObject("CameraConfiner");
                confinerChild.transform.SetParent(roomGO.transform);
                confinerChild.transform.localPosition = Vector3.zero;
                var polyCol = confinerChild.AddComponent<PolygonCollider2D>();
                polyCol.isTrigger = false;
                // Set confiner bounds to match room size
                float hw = def.Size.x * 0.5f;
                float hh = def.Size.y * 0.5f;
                polyCol.points = new Vector2[]
                {
                    new(-hw, -hh), new(hw, -hh),
                    new(hw, hh), new(-hw, hh)
                };

                // Room component
                var room = roomGO.AddComponent<Room>();

                // Wire RoomSO via SerializedObject
                if (roomSOLookup.TryGetValue(def.ID, out var roomSO))
                {
                    var serialized = new SerializedObject(room);
                    serialized.FindProperty("_data").objectReferenceValue = roomSO;
                    serialized.FindProperty("_confinerBounds").objectReferenceValue = polyCol;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                // Spawn points container
                var spawnPointsContainer = new GameObject("SpawnPoints");
                spawnPointsContainer.transform.SetParent(roomGO.transform);
                spawnPointsContainer.transform.localPosition = Vector3.zero;

                // Create 4 spawn points per room
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * (360f / 4f) * Mathf.Deg2Rad;
                    float radius = Mathf.Min(def.Size.x, def.Size.y) * 0.25f;
                    var sp = new GameObject($"SpawnPoint_{i}");
                    sp.transform.SetParent(spawnPointsContainer.transform);
                    sp.transform.localPosition = new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        0f);
                }

                // Wire spawn points
                var spawnTransforms = new Transform[4];
                for (int i = 0; i < 4; i++)
                    spawnTransforms[i] = spawnPointsContainer.transform.GetChild(i);

                var roomSerialized = new SerializedObject(room);
                var spawnPointsProp = roomSerialized.FindProperty("_spawnPoints");
                spawnPointsProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                roomSerialized.ApplyModifiedPropertiesWithoutUndo();

                lookup[def.ID] = roomGO;
            }

            return lookup;
        }

        private static void WireDoorConnections(List<DoorDef> doors, Dictionary<string, GameObject> roomGOLookup)
        {
            foreach (var doorDef in doors)
            {
                if (!roomGOLookup.TryGetValue(doorDef.FromRoomID, out var fromGO)) continue;
                if (!roomGOLookup.TryGetValue(doorDef.ToRoomID, out var toGO)) continue;

                var fromRoom = fromGO.GetComponent<Room>();
                var toRoom = toGO.GetComponent<Room>();
                if (fromRoom == null || toRoom == null) continue;

                // Create door on the "from" side
                var doorGO = new GameObject($"Door_{doorDef.FromRoomID}_to_{doorDef.ToRoomID}");
                doorGO.transform.SetParent(fromGO.transform);

                // Position door at the edge facing the target
                Vector3 direction = (toGO.transform.position - fromGO.transform.position).normalized;
                doorGO.transform.localPosition = direction * 5f; // Near room edge

                // Collider for player detection
                var doorCol = doorGO.AddComponent<BoxCollider2D>();
                doorCol.isTrigger = true;
                doorCol.size = new Vector2(3f, 3f);

                // Door component
                var door = doorGO.AddComponent<Door>();

                // Spawn point in target room
                var spawnGO = new GameObject($"SpawnPoint_from_{doorDef.FromRoomID}");
                spawnGO.transform.SetParent(toGO.transform);
                Vector3 reverseDir = (fromGO.transform.position - toGO.transform.position).normalized;
                spawnGO.transform.localPosition = reverseDir * 5f;

                // Wire via SerializedObject
                var doorSerialized = new SerializedObject(door);
                doorSerialized.FindProperty("_targetRoom").objectReferenceValue = toRoom;
                doorSerialized.FindProperty("_targetSpawnPoint").objectReferenceValue = spawnGO.transform;
                doorSerialized.FindProperty("_initialState").enumValueIndex = (int)DoorState.Open;
                doorSerialized.FindProperty("_isLayerTransition").boolValue = doorDef.IsLayerTransition;
                doorSerialized.ApplyModifiedPropertiesWithoutUndo();

                // Create corresponding door on the "to" side (reverse direction)
                var reverseDoorGO = new GameObject($"Door_{doorDef.ToRoomID}_to_{doorDef.FromRoomID}");
                reverseDoorGO.transform.SetParent(toGO.transform);
                reverseDoorGO.transform.localPosition = reverseDir * 5f;

                var reverseDoorCol = reverseDoorGO.AddComponent<BoxCollider2D>();
                reverseDoorCol.isTrigger = true;
                reverseDoorCol.size = new Vector2(3f, 3f);

                var reverseDoor = reverseDoorGO.AddComponent<Door>();

                var reverseSpawnGO = new GameObject($"SpawnPoint_from_{doorDef.ToRoomID}");
                reverseSpawnGO.transform.SetParent(fromGO.transform);
                reverseSpawnGO.transform.localPosition = direction * 5f;

                var reverseDoorSerialized = new SerializedObject(reverseDoor);
                reverseDoorSerialized.FindProperty("_targetRoom").objectReferenceValue = fromRoom;
                reverseDoorSerialized.FindProperty("_targetSpawnPoint").objectReferenceValue = reverseSpawnGO.transform;
                reverseDoorSerialized.FindProperty("_initialState").enumValueIndex = (int)DoorState.Open;
                reverseDoorSerialized.FindProperty("_isLayerTransition").boolValue = doorDef.IsLayerTransition;
                reverseDoorSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ──────────────────── Utility ────────────────────

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                // Create nested directories
                string[] parts = path.TrimEnd('/').Split('/');
                string current = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private static void PrintChecklist()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  SHEBA LEVEL SCAFFOLDING COMPLETE — POST-SETUP CHECKLIST");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  1. ✏️  Assign Player Layer to all Room/Door '_playerLayer' fields");
            Debug.Log("  2. 🖌️  Paint Tilemaps for each room (add Tilemap children)");
            Debug.Log("  3. 📐  Adjust room BoxCollider2D sizes to match your Tilemaps");
            Debug.Log("  4. 📐  Adjust CameraConfiner PolygonCollider2D to match room bounds");
            Debug.Log("  5. 👾  Assign enemy prefabs in EncounterSO assets:");
            Debug.Log("       - Assets/_Data/Level/Encounters/Sheba/Encounter_Patrol_Light.asset");
            Debug.Log("       - Assets/_Data/Level/Encounters/Sheba/Encounter_Mixed_Medium.asset");
            Debug.Log("       - Assets/_Data/Level/Encounters/Sheba/Encounter_Arena_Heavy.asset");
            Debug.Log("       - Assets/_Data/Level/Encounters/Sheba/Encounter_Boss.asset");
            Debug.Log("  6. 🗝️  Place Key pickup in 'sheba_key_chamber' room");
            Debug.Log("  7. 🏁  Place Checkpoint objects in safe rooms");
            Debug.Log("  8. 🎨  Add visual decorations, lighting, and particles");
            Debug.Log("  9. 🚢  Place player ship spawn point in 'sheba_entrance'");
            Debug.Log(" 10. 🔊  Assign audio clips to RoomSOs if per-room music is desired");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }
    }
}
#endif
