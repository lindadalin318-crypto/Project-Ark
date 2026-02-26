#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// One-click level generator: reads a LevelScaffoldData asset and produces
    /// a complete, playable scene hierarchy with Room GameObjects, RoomSO assets,
    /// Door bi-directional connections, Checkpoints, EnemySpawners, ArenaControllers,
    /// and EncounterSO assets.
    ///
    /// Menu: Window > ProjectArk > Generate Level From Scaffold
    /// </summary>
    public class ScaffoldToSceneGenerator : EditorWindow
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_DIR   = "Assets/_Data/Level/Rooms";
        private const string ENCOUNTER_DIR   = "Assets/_Data/Level/Encounters";
        private const string CHECKPOINT_DIR  = "Assets/_Data/Level/Checkpoints";
        private const string ENEMY_PREFAB_PATH = "Assets/_Prefabs/Enemies/Enemy_Rusher.prefab";
        private const string LOG_TAG = "[ScaffoldToScene]";

        // ──────────────────── EditorWindow State ────────────────────

        private LevelScaffoldData _scaffold;

        // ──────────────────── Generation Stats ────────────────────

        private struct GenerationStats
        {
            public int RoomCount;
            public int DoorCount;
            public int RoomSOCount;
            public int EncounterSOCount;
            public int CheckpointSOCount;
            public List<string> Warnings;

            public static GenerationStats Create()
            {
                return new GenerationStats { Warnings = new List<string>() };
            }
        }

        // ──────────────────── Menu Entry ────────────────────

        [MenuItem("Window/ProjectArk/Generate Level From Scaffold")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScaffoldToSceneGenerator>("Scaffold → Scene");
            window.minSize = new Vector2(400, 180);
        }

        // ──────────────────── GUI ────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Scaffold → Scene Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scaffold = (LevelScaffoldData)EditorGUILayout.ObjectField(
                "Scaffold Data", _scaffold, typeof(LevelScaffoldData), false);

            EditorGUILayout.Space(8);

            GUI.enabled = _scaffold != null;
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                int roomCount = _scaffold.Rooms.Count;
                int connCount = _scaffold.Rooms.Sum(r => r.Connections.Count);
                int elemCount = _scaffold.Rooms.Sum(r => r.Elements.Count);

                if (EditorUtility.DisplayDialog(
                    "Generate Level From Scaffold",
                    $"Level: {_scaffold.LevelName}\n" +
                    $"Rooms: {roomCount}\n" +
                    $"Connections: {connCount}\n" +
                    $"Elements: {elemCount}\n\n" +
                    "This will create GameObjects in the scene and SO assets on disk.\n" +
                    "Existing assets with matching paths will be overwritten.\n\n" +
                    "Proceed?",
                    "Generate", "Cancel"))
                {
                    Generate();
                }
            }
            GUI.enabled = true;
        }

        // ══════════════════════════════════════════════════════════════
        //  MAIN GENERATION PIPELINE
        // ══════════════════════════════════════════════════════════════

        private void Generate()
        {
            Undo.SetCurrentGroupName($"Generate Level: {_scaffold.LevelName}");
            int undoGroup = Undo.GetCurrentGroup();

            var stats = GenerationStats.Create();

            try
            {
                // Ensure output directories
                EnsureDirectory(ROOM_DATA_DIR);
                EnsureDirectory(ENCOUNTER_DIR);
                EnsureDirectory(CHECKPOINT_DIR);

                // Player layer
                int playerLayerMask = LayerMask.GetMask("Player");
                if (playerLayerMask == 0)
                {
                    Debug.LogWarning($"{LOG_TAG} 'Player' layer not found. _playerLayer fields will be 0.");
                    stats.Warnings.Add("Player layer not found — _playerLayer fields will be 0.");
                }

                // Load enemy prefab
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
                if (enemyPrefab == null)
                {
                    Debug.LogWarning($"{LOG_TAG} Enemy_Rusher.prefab not found at {ENEMY_PREFAB_PATH}. EncounterSO EnemyPrefab will be null.");
                    stats.Warnings.Add($"Enemy_Rusher.prefab not found at {ENEMY_PREFAB_PATH}.");
                }

                // Build room ID → ScaffoldRoom lookup
                var scaffoldLookup = new Dictionary<string, ScaffoldRoom>();
                foreach (var sr in _scaffold.Rooms)
                    scaffoldLookup[sr.RoomID] = sr;

                // ── Phase 1: Create Room GameObjects ──
                var levelRoot = new GameObject($"--- {_scaffold.LevelName} ---");
                Undo.RegisterCreatedObjectUndo(levelRoot, "Create Level Root");

                // roomID → (GameObject, Room component)
                var roomGOMap = new Dictionary<string, (GameObject go, Room room)>();
                // roomID → list of EnemySpawn transforms
                var roomEnemySpawns = new Dictionary<string, List<Transform>>();

                foreach (var sr in _scaffold.Rooms)
                {
                    var (go, room) = CreateRoomGameObject(sr, levelRoot.transform, playerLayerMask);
                    roomGOMap[sr.RoomID] = (go, room);
                    roomEnemySpawns[sr.RoomID] = new List<Transform>();
                    stats.RoomCount++;
                }

                // ── Phase 2: Create RoomSO assets and link ──
                var roomSOMap = new Dictionary<string, RoomSO>(); // roomID → RoomSO
                foreach (var sr in _scaffold.Rooms)
                {
                    var roomSO = CreateOrUpdateRoomSO(sr);
                    roomSOMap[sr.RoomID] = roomSO;

                    // Link to Room component
                    var room = roomGOMap[sr.RoomID].room;
                    var serialized = new SerializedObject(room);
                    serialized.FindProperty("_data").objectReferenceValue = roomSO;
                    serialized.ApplyModifiedPropertiesWithoutUndo();

                    stats.RoomSOCount++;
                }

                // ── Phase 3: Instantiate elements (PlayerSpawn, EnemySpawn, Checkpoint, placeholders) ──
                foreach (var sr in _scaffold.Rooms)
                {
                    var parentGO = roomGOMap[sr.RoomID].go;
                    int enemyIdx = 0;

                    foreach (var elem in sr.Elements)
                    {
                        switch (elem.ElementType)
                        {
                            case ScaffoldElementType.PlayerSpawn:
                                CreateElementGO("PlayerSpawn", parentGO.transform, elem.LocalPosition);
                                break;

                            case ScaffoldElementType.EnemySpawn:
                                var spGO = CreateElementGO($"EnemySpawn_{enemyIdx}", parentGO.transform, elem.LocalPosition);
                                roomEnemySpawns[sr.RoomID].Add(spGO.transform);
                                enemyIdx++;
                                break;

                            case ScaffoldElementType.Checkpoint:
                                CreateCheckpointElement(sr, elem, parentGO.transform, playerLayerMask, ref stats);
                                break;

                            case ScaffoldElementType.Door:
                                // Doors handled in Phase 4 via connections + boundConnectionID
                                break;

                            default: // Wall, WallCorner, CrateWooden, CrateMetal, Hazard
                                CreateElementGO($"[{elem.ElementType}]_Placeholder", parentGO.transform, elem.LocalPosition);
                                break;
                        }
                    }

                    // Bind EnemySpawn transforms to Room._spawnPoints
                    BindSpawnPoints(roomGOMap[sr.RoomID].room, roomEnemySpawns[sr.RoomID]);
                }

                // ── Phase 4: Door bi-directional connections ──
                var doorComponents = GenerateDoors(scaffoldLookup, roomGOMap, playerLayerMask, ref stats);

                // Apply DoorConfig from elements with _boundConnectionID
                ApplyDoorConfigFromElements(scaffoldLookup, doorComponents);

                // ── Phase 5: Arena/Boss combat setup ──
                SetupArenaBossCombat(scaffoldLookup, roomGOMap, roomSOMap, roomEnemySpawns, enemyPrefab, ref stats);

                // ── Phase 6: Normal room combat setup ──
                SetupNormalRoomCombat(scaffoldLookup, roomGOMap, roomSOMap, roomEnemySpawns, enemyPrefab, ref stats);

                // ── Phase 7: Validation report ──
                PrintReport(stats, scaffoldLookup, roomEnemySpawns);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"{LOG_TAG} Generation complete! See report above.");
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_TAG} Generation failed: {e.Message}\n{e.StackTrace}");
                Undo.RevertAllDownToGroup(undoGroup);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 1: ROOM GAMEOBJECT CREATION
        // ══════════════════════════════════════════════════════════════

        private (GameObject go, Room room) CreateRoomGameObject(
            ScaffoldRoom sr, Transform parent, int playerLayerMask)
        {
            var roomGO = new GameObject(sr.DisplayName);
            roomGO.transform.SetParent(parent);
            roomGO.transform.position = sr.Position;

            // Set layer to RoomBounds so the ship can enter rooms correctly
            int roomBoundsLayer = LayerMask.NameToLayer("RoomBounds");
            if (roomBoundsLayer >= 0)
                roomGO.layer = roomBoundsLayer;
            else
                Debug.LogWarning($"{LOG_TAG} 'RoomBounds' layer not found. Room '{sr.DisplayName}' will use Default layer. Add 'RoomBounds' in Project Settings > Tags and Layers.");

            // BoxCollider2D for room trigger
            var boxCol = roomGO.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = sr.Size;

            // CameraConfiner child
            var confinerChild = new GameObject("CameraConfiner");
            confinerChild.transform.SetParent(roomGO.transform);
            confinerChild.transform.localPosition = Vector3.zero;
            confinerChild.layer = 2; // Ignore Raycast

            var polyCol = confinerChild.AddComponent<PolygonCollider2D>();
            polyCol.isTrigger = false;
            float hw = sr.Size.x * 0.5f;
            float hh = sr.Size.y * 0.5f;
            polyCol.points = new Vector2[]
            {
                new(-hw, -hh), new(hw, -hh),
                new(hw, hh), new(-hw, hh)
            };

            // Room component
            var room = roomGO.AddComponent<Room>();

            // Wire via SerializedObject
            var serialized = new SerializedObject(room);
            serialized.FindProperty("_confinerBounds").objectReferenceValue = polyCol;
            serialized.FindProperty("_playerLayer").intValue = playerLayerMask;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return (roomGO, room);
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 2: ROOMSO CREATION
        // ══════════════════════════════════════════════════════════════

        private RoomSO CreateOrUpdateRoomSO(ScaffoldRoom sr)
        {
            string safeName = SanitizeName(sr.DisplayName);
            string path = $"{ROOM_DATA_DIR}/{safeName}_Data.asset";

            var so = AssetDatabase.LoadAssetAtPath<RoomSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<RoomSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_roomID").stringValue = sr.RoomID;
            serialized.FindProperty("_displayName").stringValue = sr.DisplayName;
            serialized.FindProperty("_floorLevel").intValue =
                sr.Position.z != 0 ? Mathf.RoundToInt(sr.Position.z) : 0;
            serialized.FindProperty("_type").enumValueIndex = (int)sr.RoomType;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return so;
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 3: ELEMENT INSTANTIATION
        // ══════════════════════════════════════════════════════════════

        private GameObject CreateElementGO(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            return go;
        }

        private void CreateCheckpointElement(
            ScaffoldRoom sr, ScaffoldElement elem, Transform parent,
            int playerLayerMask, ref GenerationStats stats)
        {
            var cpGO = new GameObject($"Checkpoint_{SanitizeName(sr.DisplayName)}");
            cpGO.transform.SetParent(parent);
            cpGO.transform.localPosition = elem.LocalPosition;

            // BoxCollider2D
            var col = cpGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 2f);

            // Checkpoint component
            var cp = cpGO.AddComponent<Checkpoint>();

            // Create CheckpointSO asset
            string safeName = SanitizeName(sr.DisplayName);
            string cpPath = $"{CHECKPOINT_DIR}/CP_{safeName}.asset";
            var cpSO = AssetDatabase.LoadAssetAtPath<CheckpointSO>(cpPath);
            if (cpSO == null)
            {
                cpSO = ScriptableObject.CreateInstance<CheckpointSO>();
                AssetDatabase.CreateAsset(cpSO, cpPath);
            }

            var cpSOSerialized = new SerializedObject(cpSO);
            cpSOSerialized.FindProperty("_checkpointID").stringValue = $"cp_{safeName}";
            cpSOSerialized.FindProperty("_displayName").stringValue = sr.DisplayName;
            cpSOSerialized.FindProperty("_restoreHP").boolValue = true;
            cpSOSerialized.FindProperty("_restoreHeat").boolValue = true;
            cpSOSerialized.ApplyModifiedPropertiesWithoutUndo();

            // Wire Checkpoint component
            var cpSerialized = new SerializedObject(cp);
            cpSerialized.FindProperty("_data").objectReferenceValue = cpSO;
            cpSerialized.FindProperty("_playerLayer").intValue = playerLayerMask;
            cpSerialized.ApplyModifiedPropertiesWithoutUndo();

            stats.CheckpointSOCount++;
        }

        private void BindSpawnPoints(Room room, List<Transform> spawnTransforms)
        {
            if (spawnTransforms.Count == 0) return;

            var serialized = new SerializedObject(room);
            var prop = serialized.FindProperty("_spawnPoints");
            prop.arraySize = spawnTransforms.Count;
            for (int i = 0; i < spawnTransforms.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 4: DOOR BI-DIRECTIONAL CONNECTIONS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a dictionary: connectionID → Door component (the forward door).
        /// Used later by ApplyDoorConfigFromElements to overlay DoorConfig.
        /// </summary>
        private Dictionary<string, Door> GenerateDoors(
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, (GameObject go, Room room)> roomGOMap,
            int playerLayerMask,
            ref GenerationStats stats)
        {
            // Track processed connection pairs to avoid duplicates
            var processedPairs = new HashSet<string>();
            // connectionID → Door component (forward direction)
            var doorLookup = new Dictionary<string, Door>();

            foreach (var sr in scaffoldLookup.Values)
            {
                foreach (var conn in sr.Connections)
                {
                    // Create canonical pair key
                    string pairKey = GetPairKey(sr.RoomID, conn.TargetRoomID);
                    if (processedPairs.Contains(pairKey)) continue;
                    processedPairs.Add(pairKey);

                    if (!roomGOMap.TryGetValue(sr.RoomID, out var source)) continue;
                    if (!roomGOMap.TryGetValue(conn.TargetRoomID, out var target)) continue;

                    string sourceDisplayName = scaffoldLookup[sr.RoomID].DisplayName;
                    string targetDisplayName = scaffoldLookup.TryGetValue(conn.TargetRoomID, out var tsr)
                        ? tsr.DisplayName : conn.TargetRoomID;

                    // ── Forward door: source → target ──
                    var fwdDoorGO = new GameObject($"Door_to_{SanitizeName(targetDisplayName)}");
                    fwdDoorGO.transform.SetParent(source.go.transform);
                    fwdDoorGO.transform.localPosition = conn.DoorPosition;

                    var fwdCol = fwdDoorGO.AddComponent<BoxCollider2D>();
                    fwdCol.isTrigger = true;
                    fwdCol.size = new Vector2(3f, 3f);

                    var fwdDoor = fwdDoorGO.AddComponent<Door>();

                    // ── Find reverse connection's door position ──
                    Vector3 reverseSpawnPos = FindReverseDoorPosition(
                        conn.TargetRoomID, sr.RoomID, scaffoldLookup, conn.DoorDirection);

                    // ── SpawnPoint in target room (where player appears) ──
                    var fwdSpawn = new GameObject($"SpawnPoint_from_{SanitizeName(sourceDisplayName)}");
                    fwdSpawn.transform.SetParent(target.go.transform);
                    fwdSpawn.transform.localPosition = reverseSpawnPos;

                    // ── Reverse door: target → source ──
                    var revDoorGO = new GameObject($"Door_to_{SanitizeName(sourceDisplayName)}");
                    revDoorGO.transform.SetParent(target.go.transform);
                    revDoorGO.transform.localPosition = reverseSpawnPos;

                    var revCol = revDoorGO.AddComponent<BoxCollider2D>();
                    revCol.isTrigger = true;
                    revCol.size = new Vector2(3f, 3f);

                    var revDoor = revDoorGO.AddComponent<Door>();

                    // ── SpawnPoint in source room (where player appears on reverse) ──
                    var revSpawn = new GameObject($"SpawnPoint_from_{SanitizeName(targetDisplayName)}");
                    revSpawn.transform.SetParent(source.go.transform);
                    revSpawn.transform.localPosition = conn.DoorPosition;

                    // ── Wire forward door ──
                    WireDoor(fwdDoor, target.room, fwdSpawn.transform,
                        conn.IsLayerTransition, DoorState.Open, playerLayerMask);

                    // ── Wire reverse door ──
                    // Find the reverse connection to check IsLayerTransition
                    var reverseConn = FindReverseConnection(conn.TargetRoomID, sr.RoomID, scaffoldLookup);
                    bool revIsLayerTransition = reverseConn?.IsLayerTransition ?? conn.IsLayerTransition;

                    WireDoor(revDoor, source.room, revSpawn.transform,
                        revIsLayerTransition, DoorState.Open, playerLayerMask);

                    // Store forward door by connectionID for DoorConfig overlay
                    doorLookup[conn.ConnectionID] = fwdDoor;
                    if (reverseConn != null)
                        doorLookup[reverseConn.ConnectionID] = revDoor;

                    stats.DoorCount += 2; // Both directions
                }
            }

            return doorLookup;
        }

        private void WireDoor(Door door, Room targetRoom, Transform spawnPoint,
            bool isLayerTransition, DoorState initialState, int playerLayerMask)
        {
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serialized.FindProperty("_targetSpawnPoint").objectReferenceValue = spawnPoint;
            serialized.FindProperty("_isLayerTransition").boolValue = isLayerTransition;
            serialized.FindProperty("_initialState").enumValueIndex = (int)initialState;
            serialized.FindProperty("_playerLayer").intValue = playerLayerMask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private Vector3 FindReverseDoorPosition(
            string targetRoomID, string sourceRoomID,
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Vector2 forwardDirection)
        {
            // Try to find the reverse connection
            var reverseConn = FindReverseConnection(targetRoomID, sourceRoomID, scaffoldLookup);
            if (reverseConn != null)
                return reverseConn.DoorPosition;

            // Fallback: offset from center in reverse direction
            return new Vector3(-forwardDirection.x * 2f, -forwardDirection.y * 2f, 0f);
        }

        private ScaffoldDoorConnection FindReverseConnection(
            string fromRoomID, string toRoomID,
            Dictionary<string, ScaffoldRoom> scaffoldLookup)
        {
            if (!scaffoldLookup.TryGetValue(fromRoomID, out var room)) return null;
            return room.Connections.FirstOrDefault(c => c.TargetRoomID == toRoomID);
        }

        private string GetPairKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";
        }

        /// <summary>
        /// For Door elements with _boundConnectionID, overlay DoorConfig fields
        /// onto the already-created Door component.
        /// </summary>
        private void ApplyDoorConfigFromElements(
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, Door> doorLookup)
        {
            foreach (var sr in scaffoldLookup.Values)
            {
                foreach (var elem in sr.Elements)
                {
                    if (elem.ElementType != ScaffoldElementType.Door) continue;
                    if (string.IsNullOrEmpty(elem.BoundConnectionID)) continue;
                    if (elem.DoorConfig == null) continue;

                    if (doorLookup.TryGetValue(elem.BoundConnectionID, out var door))
                    {
                        var serialized = new SerializedObject(door);
                        serialized.FindProperty("_initialState").enumValueIndex = (int)elem.DoorConfig.InitialState;

                        if (!string.IsNullOrEmpty(elem.DoorConfig.RequiredKeyID))
                            serialized.FindProperty("_requiredKeyID").stringValue = elem.DoorConfig.RequiredKeyID;

                        if (elem.DoorConfig.OpenDuringPhases != null && elem.DoorConfig.OpenDuringPhases.Length > 0)
                        {
                            var phasesProp = serialized.FindProperty("_openDuringPhases");
                            phasesProp.arraySize = elem.DoorConfig.OpenDuringPhases.Length;
                            for (int i = 0; i < elem.DoorConfig.OpenDuringPhases.Length; i++)
                                phasesProp.GetArrayElementAtIndex(i).intValue = elem.DoorConfig.OpenDuringPhases[i];
                        }

                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else
                    {
                        Debug.LogWarning($"{LOG_TAG} Door element in '{sr.DisplayName}' has boundConnectionID " +
                            $"'{elem.BoundConnectionID}' but no matching connection was found.");
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 5: ARENA / BOSS COMBAT SETUP
        // ══════════════════════════════════════════════════════════════

        private void SetupArenaBossCombat(
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, (GameObject go, Room room)> roomGOMap,
            Dictionary<string, RoomSO> roomSOMap,
            Dictionary<string, List<Transform>> roomEnemySpawns,
            GameObject enemyPrefab,
            ref GenerationStats stats)
        {
            foreach (var sr in scaffoldLookup.Values)
            {
                if (sr.RoomType != RoomType.Arena && sr.RoomType != RoomType.Boss) continue;

                var (go, room) = roomGOMap[sr.RoomID];

                // Add ArenaController
                go.AddComponent<ArenaController>();

                // Create EnemySpawner child
                var spawnerGO = new GameObject("EnemySpawner");
                spawnerGO.transform.SetParent(go.transform);
                spawnerGO.transform.localPosition = Vector3.zero;
                var spawner = spawnerGO.AddComponent<EnemySpawner>();

                // Bind spawn points to EnemySpawner
                var spawnTransforms = roomEnemySpawns[sr.RoomID];
                if (spawnTransforms.Count > 0)
                {
                    var spawnerSerialized = new SerializedObject(spawner);
                    var spProp = spawnerSerialized.FindProperty("_spawnPoints");
                    spProp.arraySize = spawnTransforms.Count;
                    for (int i = 0; i < spawnTransforms.Count; i++)
                        spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                    spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    stats.Warnings.Add($"Arena/Boss room '{sr.DisplayName}' has no EnemySpawn elements.");
                }

                // Create EncounterSO
                string safeName = SanitizeName(sr.DisplayName);
                string encPath = $"{ENCOUNTER_DIR}/{safeName}_[DEFAULT]_Encounter.asset";
                var encSO = CreateEncounterSO(encPath, sr.RoomType, enemyPrefab);
                stats.EncounterSOCount++;

                // Assign to RoomSO
                if (roomSOMap.TryGetValue(sr.RoomID, out var roomSO))
                {
                    var soSerialized = new SerializedObject(roomSO);
                    soSerialized.FindProperty("_encounter").objectReferenceValue = encSO;
                    soSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 6: NORMAL ROOM COMBAT SETUP
        // ══════════════════════════════════════════════════════════════

        private void SetupNormalRoomCombat(
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, (GameObject go, Room room)> roomGOMap,
            Dictionary<string, RoomSO> roomSOMap,
            Dictionary<string, List<Transform>> roomEnemySpawns,
            GameObject enemyPrefab,
            ref GenerationStats stats)
        {
            foreach (var sr in scaffoldLookup.Values)
            {
                if (sr.RoomType != RoomType.Normal) continue;

                var spawnTransforms = roomEnemySpawns[sr.RoomID];
                if (spawnTransforms.Count == 0) continue; // No EnemySpawns → skip

                var (go, room) = roomGOMap[sr.RoomID];

                // Create EnemySpawner child (NO ArenaController for Normal rooms)
                var spawnerGO = new GameObject("EnemySpawner");
                spawnerGO.transform.SetParent(go.transform);
                spawnerGO.transform.localPosition = Vector3.zero;
                var spawner = spawnerGO.AddComponent<EnemySpawner>();

                // Bind spawn points
                var spawnerSerialized = new SerializedObject(spawner);
                var spProp = spawnerSerialized.FindProperty("_spawnPoints");
                spProp.arraySize = spawnTransforms.Count;
                for (int i = 0; i < spawnTransforms.Count; i++)
                    spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

                // Create EncounterSO (lighter: 1 wave, 2 enemies)
                string safeName = SanitizeName(sr.DisplayName);
                string encPath = $"{ENCOUNTER_DIR}/{safeName}_[DEFAULT]_Encounter.asset";
                var encSO = CreateEncounterSO_Normal(encPath, enemyPrefab);
                stats.EncounterSOCount++;

                // Assign to RoomSO
                if (roomSOMap.TryGetValue(sr.RoomID, out var roomSO))
                {
                    var soSerialized = new SerializedObject(roomSO);
                    soSerialized.FindProperty("_encounter").objectReferenceValue = encSO;
                    soSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // ──────────────────── EncounterSO Helpers ────────────────────

        private EncounterSO CreateEncounterSO(string path, RoomType roomType, GameObject enemyPrefab)
        {
            var so = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EncounterSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            var wavesProp = serialized.FindProperty("_waves");

            if (roomType == RoomType.Arena)
            {
                // 1 wave, 3 enemies
                wavesProp.arraySize = 1;
                var wave0 = wavesProp.GetArrayElementAtIndex(0);
                wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
                var entries0 = wave0.FindPropertyRelative("Entries");
                entries0.arraySize = 1;
                var entry0 = entries0.GetArrayElementAtIndex(0);
                entry0.FindPropertyRelative("EnemyPrefab").objectReferenceValue = enemyPrefab;
                entry0.FindPropertyRelative("Count").intValue = 3;
            }
            else // Boss
            {
                // 2 waves: wave 1 = 2 enemies (delay 0), wave 2 = 3 enemies (delay 1.5)
                wavesProp.arraySize = 2;

                var wave0 = wavesProp.GetArrayElementAtIndex(0);
                wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
                var entries0 = wave0.FindPropertyRelative("Entries");
                entries0.arraySize = 1;
                var entry0 = entries0.GetArrayElementAtIndex(0);
                entry0.FindPropertyRelative("EnemyPrefab").objectReferenceValue = enemyPrefab;
                entry0.FindPropertyRelative("Count").intValue = 2;

                var wave1 = wavesProp.GetArrayElementAtIndex(1);
                wave1.FindPropertyRelative("DelayBeforeWave").floatValue = 1.5f;
                var entries1 = wave1.FindPropertyRelative("Entries");
                entries1.arraySize = 1;
                var entry1 = entries1.GetArrayElementAtIndex(0);
                entry1.FindPropertyRelative("EnemyPrefab").objectReferenceValue = enemyPrefab;
                entry1.FindPropertyRelative("Count").intValue = 3;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private EncounterSO CreateEncounterSO_Normal(string path, GameObject enemyPrefab)
        {
            var so = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EncounterSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            var wavesProp = serialized.FindProperty("_waves");

            // 1 wave, 2 enemies (lighter than Arena)
            wavesProp.arraySize = 1;
            var wave0 = wavesProp.GetArrayElementAtIndex(0);
            wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
            var entries0 = wave0.FindPropertyRelative("Entries");
            entries0.arraySize = 1;
            var entry0 = entries0.GetArrayElementAtIndex(0);
            entry0.FindPropertyRelative("EnemyPrefab").objectReferenceValue = enemyPrefab;
            entry0.FindPropertyRelative("Count").intValue = 2;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 7: VALIDATION REPORT
        // ══════════════════════════════════════════════════════════════

        private void PrintReport(GenerationStats stats,
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, List<Transform>> roomEnemySpawns)
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log($"  {LOG_TAG} GENERATION REPORT");
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log($"  Rooms generated:      {stats.RoomCount}");
            Debug.Log($"  Doors generated:       {stats.DoorCount} (bi-directional)");
            Debug.Log($"  RoomSO assets:         {stats.RoomSOCount}");
            Debug.Log($"  EncounterSO assets:    {stats.EncounterSOCount}");
            Debug.Log($"  CheckpointSO assets:   {stats.CheckpointSOCount}");
            Debug.Log("───────────────────────────────────────────────────────────────");

            // Check Arena/Boss rooms for missing EnemySpawn
            foreach (var sr in scaffoldLookup.Values)
            {
                if (sr.RoomType != RoomType.Arena && sr.RoomType != RoomType.Boss) continue;
                if (roomEnemySpawns.TryGetValue(sr.RoomID, out var spawns) && spawns.Count == 0)
                {
                    Debug.LogWarning($"{LOG_TAG} ⚠️ {sr.RoomType} room '{sr.DisplayName}' has NO EnemySpawn elements!");
                }
            }

            // Output warnings
            if (stats.Warnings.Count > 0)
            {
                Debug.Log("  ⚠️ WARNINGS:");
                foreach (var w in stats.Warnings)
                    Debug.LogWarning($"  • {w}");
            }

            // TODO checklist
            Debug.Log("───────────────────────────────────────────────────────────────");
            Debug.Log("  📋 TODO CHECKLIST (manual follow-up):");
            Debug.Log("  1. 🖌️  Paint Tilemaps for each room");
            Debug.Log("  2. 👾  Replace [DEFAULT] EncounterSO enemies with real enemy Prefabs");
            Debug.Log("  3. 🐉  Configure Boss room with dedicated Boss Prefab");
            Debug.Log("  4. 🎨  Add visual decorations, lighting, and particle effects");
            Debug.Log("  5. 🖼️  Add SpriteRenderer to Checkpoint GameObjects");
            Debug.Log("  6. ⚙️  Configure Physics2D collision matrix (Player vs Room/Door/Checkpoint)");
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════════════════════
        //  UTILITY
        // ══════════════════════════════════════════════════════════════

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizeName(string name)
        {
            // Replace invalid filename chars and common punctuation with underscores
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0 || c == '·' || c == '→')
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
#endif
