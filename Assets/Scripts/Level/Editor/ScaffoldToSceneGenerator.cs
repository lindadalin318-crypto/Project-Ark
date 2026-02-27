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

        private const string LEVEL_DATA_ROOT   = "Assets/_Data/Level";
        private const string ENEMY_PREFAB_PATH = "Assets/_Prefabs/Enemies/Enemy_Rusher.prefab";
        private const string LOG_TAG = "[ScaffoldToScene]";

        // ──────────────────── Per-generation Paths (set in Generate()) ────────────────────

        private string _roomDataDir;
        private string _encounterDir;
        private string _checkpointDir;

        // ──────────────────── EditorWindow State ────────────────────

        private LevelScaffoldData _scaffold;
        private bool _updateExisting = false;
        private bool _gizmosVisible = true;

        // ──────────────────── Generation Stats ────────────────────

        private struct GenerationStats
        {
            public int RoomCount;
            public int DoorCount;
            public int RoomSOCount;
            public int EncounterSOCount;
            public int CheckpointSOCount;
            public List<string> Warnings;
            public List<string> FallbackRooms;   // rooms that used fallback size
            public List<string> PreservedRooms;  // rooms skipped in update-existing mode

            public static GenerationStats Create()
            {
                return new GenerationStats
                {
                    Warnings = new List<string>(),
                    FallbackRooms = new List<string>(),
                    PreservedRooms = new List<string>()
                };
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

            // ── 需求8：路径预览 ──
            if (_scaffold != null)
            {
                string previewName = SanitizeName(_scaffold.LevelName);
                EditorGUILayout.HelpBox($"Output: Assets/_Data/Level/{previewName}/", MessageType.None);
            }

            EditorGUILayout.Space(8);

            // ── 需求6：Update Existing 复选框 ──
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _scaffold != null;
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                int roomCount = _scaffold.Rooms.Count;
                int connCount = _scaffold.Rooms.Sum(r => r.Connections.Count);
                int elemCount = _scaffold.Rooms.Sum(r => r.Elements.Count);

                string modeLabel = _updateExisting ? "[UPDATE EXISTING MODE]\nExisting rooms with matching names will be updated in-place (Tilemaps preserved).\n\n" : "";

                if (EditorUtility.DisplayDialog(
                    "Generate Level From Scaffold",
                    $"{modeLabel}Level: {_scaffold.LevelName}\n" +
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
            _updateExisting = EditorGUILayout.ToggleLeft("Update Existing", _updateExisting, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.Space(4);

            // ── 需求7：Toggle Gizmos 按钮 ──
            string gizmosBtnLabel = _gizmosVisible ? "Hide Gizmos" : "Show Gizmos";
            if (GUILayout.Button(gizmosBtnLabel, GUILayout.Height(24)))
            {
                _gizmosVisible = !_gizmosVisible;
                ToggleGizmos();
            }
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
                // ── Compute per-scaffold output directories ──
                string safeLevelName = SanitizeName(_scaffold.LevelName);
                string levelRoot_Dir = $"{LEVEL_DATA_ROOT}/{safeLevelName}";
                _roomDataDir    = $"{levelRoot_Dir}/Rooms";
                _encounterDir   = $"{levelRoot_Dir}/Encounters";
                _checkpointDir  = $"{levelRoot_Dir}/Checkpoints";

                // Ensure output directories
                EnsureDirectory(_roomDataDir);
                EnsureDirectory(_encounterDir);
                EnsureDirectory(_checkpointDir);

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
                // roomID → list of EnemyTypeID strings (parallel to roomEnemySpawns)
                var roomEnemyTypeIDs = new Dictionary<string, List<string>>();

                foreach (var sr in _scaffold.Rooms)
                {
                    // ── 需求2：房间尺寸 Fallback ──
                    if (sr.Size.x <= 0 || sr.Size.y <= 0)
                    {
                        Vector2 fallback = GetFallbackSize(sr.RoomType);
                        Debug.LogWarning($"{LOG_TAG} ⚠️ Room '{sr.DisplayName}' has zero/invalid Size, using fallback {fallback.x}×{fallback.y}");
                        sr.Size = fallback;
                        stats.FallbackRooms.Add(sr.DisplayName);
                    }

                    // ── 需求6：增量更新模式 ──
                    if (_updateExisting)
                    {
                        var existingGO = FindRoomGOByName(sr.DisplayName);
                        if (existingGO != null)
                        {
                            var existingRoom = existingGO.GetComponent<Room>();
                            roomGOMap[sr.RoomID] = (existingGO, existingRoom);
                            roomEnemySpawns[sr.RoomID] = new List<Transform>();
                            roomEnemyTypeIDs[sr.RoomID] = new List<string>();
                            stats.PreservedRooms.Add(sr.DisplayName);
                            stats.RoomCount++;
                            continue;
                        }
                    }

                    var (go, room) = CreateRoomGameObject(sr, levelRoot.transform, playerLayerMask);
                    roomGOMap[sr.RoomID] = (go, room);
                    roomEnemySpawns[sr.RoomID] = new List<Transform>();
                    roomEnemyTypeIDs[sr.RoomID] = new List<string>();
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
                                CreateElementGO("PlayerSpawn", parentGO.transform, elem.LocalPosition,
                                    ScaffoldElementType.PlayerSpawn);
                                break;

                            case ScaffoldElementType.EnemySpawn:
                                var spGO = CreateElementGO($"EnemySpawn_{enemyIdx}", parentGO.transform, elem.LocalPosition,
                                    ScaffoldElementType.EnemySpawn);
                                roomEnemySpawns[sr.RoomID].Add(spGO.transform);
                                roomEnemyTypeIDs[sr.RoomID].Add(elem.EnemyTypeID ?? string.Empty);
                                enemyIdx++;
                                break;

                            case ScaffoldElementType.Checkpoint:
                                CreateCheckpointElement(sr, elem, parentGO.transform, playerLayerMask, ref stats);
                                break;

                            case ScaffoldElementType.Door:
                                // Doors handled in Phase 4 via connections + boundConnectionID
                                break;

                            default: // Wall, WallCorner, CrateWooden, CrateMetal, Hazard
                                CreateElementGO($"[{elem.ElementType}]_Placeholder", parentGO.transform, elem.LocalPosition,
                                    elem.ElementType);
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
                SetupArenaBossCombat(scaffoldLookup, roomGOMap, roomSOMap, roomEnemySpawns, roomEnemyTypeIDs, enemyPrefab, ref stats);

                // ── Phase 6: Normal room combat setup ──
                SetupNormalRoomCombat(scaffoldLookup, roomGOMap, roomSOMap, roomEnemySpawns, roomEnemyTypeIDs, enemyPrefab, ref stats);

                // ── Phase 7: Validation report ──
                PrintReport(stats, scaffoldLookup, roomEnemySpawns, roomEnemyTypeIDs);

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
            polyCol.isTrigger = true;
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

            // ── 需求4：自动创建标准 Tilemap 层级 ──
            CreateTilemapHierarchy(roomGO);

            return (roomGO, room);
        }

        /// <summary>
        /// 需求4：在房间 GO 下自动创建标准 Tilemap 层级。
        /// Tilemaps/Tilemap_Ground (order=0), Tilemap_Wall (order=1), Tilemap_Decoration (order=2)
        /// </summary>
        private static void CreateTilemapHierarchy(GameObject roomGO)
        {
            var tilemapsGO = new GameObject("Tilemaps");
            tilemapsGO.transform.SetParent(roomGO.transform);
            tilemapsGO.transform.localPosition = Vector3.zero;

            var layers = new (string name, int order)[]
            {
                ("Tilemap_Ground",      0),
                ("Tilemap_Wall",        1),
                ("Tilemap_Decoration",  2),
            };

            foreach (var (layerName, sortOrder) in layers)
            {
                var layerGO = new GameObject(layerName);
                layerGO.transform.SetParent(tilemapsGO.transform);
                layerGO.transform.localPosition = Vector3.zero;
                layerGO.AddComponent<UnityEngine.Tilemaps.Tilemap>();
                var renderer = layerGO.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                renderer.sortingOrder = sortOrder;
            }
        }

        /// <summary>
        /// 需求6：在场景中按名称查找已存在的房间 GO。
        /// </summary>
        private static GameObject FindRoomGOByName(string displayName)
        {
            var allGOs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allGOs)
            {
                // 尝试在根对象下查找
                var found = root.transform.Find(displayName);
                if (found != null) return found.gameObject;
                // 也检查根对象本身
                if (root.name == displayName) return root;
            }
            return null;
        }

        /// <summary>
        /// 需求2：根据 RoomType 返回预设默认尺寸。
        /// </summary>
        private static Vector2 GetFallbackSize(RoomType type)
        {
            return type switch
            {
                RoomType.Normal    => new Vector2(20f, 15f),
                RoomType.Arena     => new Vector2(30f, 20f),
                RoomType.Boss      => new Vector2(40f, 30f),
                RoomType.Corridor  => new Vector2(20f, 8f),
                RoomType.Shop      => new Vector2(15f, 12f),
                _                  => new Vector2(20f, 15f),
            };
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 2: ROOMSO CREATION
        // ══════════════════════════════════════════════════════════════

        private RoomSO CreateOrUpdateRoomSO(ScaffoldRoom sr)
        {
            string safeName = SanitizeName(sr.DisplayName);
            string path = $"{_roomDataDir}/{safeName}_Data.asset";

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

        /// <summary>
        /// Returns the world-space size for each scaffold element type's gizmo sprite,
        /// matching the BoxCollider2D trigger area used at runtime.
        /// </summary>
        private static Vector2 GetElementGizmoSize(ScaffoldElementType type)
        {
            return type switch
            {
ScaffoldElementType.Door        => new Vector2(1f, 1f),   // matches BoxCollider2D.size
                ScaffoldElementType.Checkpoint  => new Vector2(2f, 2f),   // matches BoxCollider2D.size
                ScaffoldElementType.PlayerSpawn => new Vector2(1.5f, 1.5f),
                ScaffoldElementType.EnemySpawn  => new Vector2(1.5f, 1.5f),
                _                               => Vector2.one,
            };
        }

        /// <summary>
        /// Returns a distinct color for each scaffold element type, used by gizmo visuals.
        /// </summary>
        private static Color GetElementGizmoColor(ScaffoldElementType type)
        {
            return type switch
            {
                ScaffoldElementType.PlayerSpawn  => new Color(0.9f, 0.8f, 0.2f, 0.8f),  // yellow
                ScaffoldElementType.EnemySpawn   => new Color(0.9f, 0.3f, 0.3f, 0.8f),  // red
                ScaffoldElementType.Checkpoint   => new Color(0.3f, 0.9f, 0.4f, 0.8f),  // green
                ScaffoldElementType.Wall         => new Color(0.6f, 0.6f, 0.6f, 0.8f),  // grey
                ScaffoldElementType.WallCorner   => new Color(0.5f, 0.5f, 0.5f, 0.8f),  // dark grey
                ScaffoldElementType.CrateWooden  => new Color(0.8f, 0.5f, 0.2f, 0.8f),  // orange
                ScaffoldElementType.CrateMetal   => new Color(0.4f, 0.4f, 0.5f, 0.8f),  // blue-grey
                ScaffoldElementType.Hazard       => new Color(0.9f, 0.2f, 0.8f, 0.8f),  // purple
                ScaffoldElementType.Door         => new Color(0.3f, 0.7f, 0.8f, 0.8f),  // cyan
                _                                => new Color(1.0f, 1.0f, 1.0f, 0.8f),  // white fallback
            };
        }

        /// <summary>
        /// Adds a SpriteRenderer (colored square) and a TextMesh label child to a GO.
        /// <paramref name="size"/> controls the world-space size of the color block so it can
        /// match the GO's BoxCollider2D trigger area (e.g. pass (3,3) for a Door).
        /// These are editor-only gizmo visuals; remove/hide before shipping.
        /// </summary>
        private static void AddGizmoVisuals(GameObject go, string labelText, Color color,
            Vector2 size = default)
        {
            if (size == default) size = Vector2.one;

            // ── SpriteRenderer color block ──
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color  = color;
            sr.sortingOrder = 1;
            // Scale the GO so the sprite visually matches the collider area
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            // ── TextMesh label child ──
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform);
            // Offset label above the block; compensate for parent scale so it stays readable
            float labelY = 0.5f + (0.6f / size.y);
            labelGO.transform.localPosition = new Vector3(0f, labelY, 0f);
            // Counter-scale so text size is independent of parent scale
            labelGO.transform.localScale    = new Vector3(0.1f / size.x, 0.1f / size.y, 0.1f);

            var tm = labelGO.AddComponent<TextMesh>();
            tm.text      = labelText;
            tm.fontSize  = 12;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color     = Color.white;

            var mr = labelGO.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 2;
        }

        private GameObject CreateElementGO(string name, Transform parent, Vector3 localPos,
            ScaffoldElementType type = ScaffoldElementType.PlayerSpawn, string labelOverride = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            AddGizmoVisuals(go, labelOverride ?? type.ToString(),
                GetElementGizmoColor(type), GetElementGizmoSize(type));
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

            // Gizmo visual — size matches BoxCollider2D (2×2)
            AddGizmoVisuals(cpGO, "Checkpoint",
                GetElementGizmoColor(ScaffoldElementType.Checkpoint),
                GetElementGizmoSize(ScaffoldElementType.Checkpoint));

            // Checkpoint component
            var cp = cpGO.AddComponent<Checkpoint>();

            // Create CheckpointSO asset
            string safeName = SanitizeName(sr.DisplayName);
            string cpPath = $"{_checkpointDir}/CP_{safeName}.asset";
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

                    // ── 需求3：Door 位置自动推算 ──
                    Vector3 fwdDoorLocalPos = conn.DoorPosition;
                    if (fwdDoorLocalPos == Vector3.zero)
                    {
                        var srcRoom = scaffoldLookup[sr.RoomID];
                        fwdDoorLocalPos = ResolveDoorPosition(conn, srcRoom.Size);
                    }
                    fwdDoorGO.transform.localPosition = fwdDoorLocalPos;

                    var fwdCol = fwdDoorGO.AddComponent<BoxCollider2D>();
                    fwdCol.isTrigger = true;
            fwdCol.size = new Vector2(1f, 1f);

            var fwdDoor = fwdDoorGO.AddComponent<Door>();
            AddGizmoVisuals(fwdDoorGO, "Door", GetElementGizmoColor(ScaffoldElementType.Door), new Vector2(1f, 1f));

                    // ── Find reverse connection's door position ──
                    Vector3 reverseSpawnPos = FindReverseDoorPosition(
                        conn.TargetRoomID, sr.RoomID, scaffoldLookup, conn.DoorDirection);

                    // ── SpawnPoint in target room (where player appears) ──
                    var fwdSpawn = new GameObject($"SpawnPoint_from_{SanitizeName(sourceDisplayName)}");
                    fwdSpawn.transform.SetParent(target.go.transform);
                    fwdSpawn.transform.localPosition = reverseSpawnPos;
                    AddGizmoVisuals(fwdSpawn, "SpawnPt", new Color(0.5f, 0.8f, 1.0f, 0.6f), new Vector2(1.5f, 1.5f));

                    // ── Reverse door: target → source ──
                    var revDoorGO = new GameObject($"Door_to_{SanitizeName(sourceDisplayName)}");
                    revDoorGO.transform.SetParent(target.go.transform);

                    // ── 需求3：反向门位置自动推算 ──
                    Vector3 revDoorLocalPos = reverseSpawnPos;
                    if (revDoorLocalPos == Vector3.zero)
                    {
                        var reverseConnForPos = FindReverseConnection(conn.TargetRoomID, sr.RoomID, scaffoldLookup);
                        if (reverseConnForPos != null && reverseConnForPos.DoorPosition != Vector3.zero)
                            revDoorLocalPos = reverseConnForPos.DoorPosition;
                        else if (reverseConnForPos != null)
                        {
                            var tgtRoom = scaffoldLookup[conn.TargetRoomID];
                            revDoorLocalPos = ResolveDoorPosition(reverseConnForPos, tgtRoom.Size);
                        }
                    }
                    revDoorGO.transform.localPosition = revDoorLocalPos;

                    var revCol = revDoorGO.AddComponent<BoxCollider2D>();
                    revCol.isTrigger = true;
            revCol.size = new Vector2(1f, 1f);

            var revDoor = revDoorGO.AddComponent<Door>();
            AddGizmoVisuals(revDoorGO, "Door", GetElementGizmoColor(ScaffoldElementType.Door), new Vector2(1f, 1f));

                    // ── SpawnPoint in source room (where player appears on reverse) ──
                    var revSpawn = new GameObject($"SpawnPoint_from_{SanitizeName(targetDisplayName)}");
                    revSpawn.transform.SetParent(source.go.transform);
                    revSpawn.transform.localPosition = conn.DoorPosition;
                    AddGizmoVisuals(revSpawn, "SpawnPt", new Color(0.5f, 0.8f, 1.0f, 0.6f), new Vector2(1.5f, 1.5f));

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
            if (reverseConn != null && reverseConn.DoorPosition != Vector3.zero)
                return reverseConn.DoorPosition;

            // 需求3：如果反向连接存在但位置为零，尝试自动推算
            if (reverseConn != null && scaffoldLookup.TryGetValue(targetRoomID, out var tgtRoom))
                return ResolveDoorPosition(reverseConn, tgtRoom.Size);

            // Fallback: offset from center in reverse direction
            return new Vector3(-forwardDirection.x * 2f, -forwardDirection.y * 2f, 0f);
        }

        /// <summary>
        /// 需求3：根据 DoorDirection 和房间尺寸自动计算门在房间边缘的局部坐标。
        /// </summary>
        private static Vector3 ResolveDoorPosition(ScaffoldDoorConnection conn, Vector2 roomSize)
        {
            float hw = roomSize.x * 0.5f;
            float hh = roomSize.y * 0.5f;

            // 判断主要方向（允许任意 Vector2 方向）
            var dir = conn.DoorDirection;

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                // 水平方向为主
                if (dir.x > 0f)  return new Vector3( hw, 0f, 0f);  // Right
                if (dir.x < 0f)  return new Vector3(-hw, 0f, 0f);  // Left
            }
            else
            {
                // 垂直方向为主
                if (dir.y > 0f)  return new Vector3(0f,  hh, 0f);  // Up
                if (dir.y < 0f)  return new Vector3(0f, -hh, 0f);  // Down
            }

            // DoorDirection 为 (0,0) 或未定义：fallback
            Debug.LogWarning($"{LOG_TAG} ⚠️ Connection '{conn.ConnectionID}' has DoorDirection (0,0). " +
                "Cannot auto-resolve door position. Using fallback (-dir*2).");
            return new Vector3(-conn.DoorDirection.x * 2f, -conn.DoorDirection.y * 2f, 0f);
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
            Dictionary<string, List<string>> roomEnemyTypeIDs,
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

                // Create EncounterSO — 需求5：按 EnemyTypeID 自动填充
                string safeName = SanitizeName(sr.DisplayName);
                string encPath = $"{_encounterDir}/{safeName}_[DEFAULT]_Encounter.asset";
                var typeIDs = roomEnemyTypeIDs.TryGetValue(sr.RoomID, out var ids) ? ids : new List<string>();
                var encSO = CreateEncounterSO(encPath, sr.RoomType, enemyPrefab, typeIDs, ref stats);
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
            Dictionary<string, List<string>> roomEnemyTypeIDs,
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

                // Create EncounterSO (lighter: 1 wave, 2 enemies) — 需求5：按 EnemyTypeID 自动填充
                string safeName = SanitizeName(sr.DisplayName);
                string encPath = $"{_encounterDir}/{safeName}_[DEFAULT]_Encounter.asset";
                var typeIDs = roomEnemyTypeIDs.TryGetValue(sr.RoomID, out var ids) ? ids : new List<string>();
                var encSO = CreateEncounterSO_Normal(encPath, enemyPrefab, typeIDs, ref stats);
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

        /// <summary>
        /// 需求5：根据 EnemyTypeID 列表加载对应 Prefab，找不到则 fallback 到 Enemy_Rusher。
        /// 返回去重后的 (prefab, count) 列表，每种类型一个 Entry。
        /// </summary>
        private List<(GameObject prefab, int count)> ResolveEnemyEntries(
            List<string> typeIDs, GameObject fallbackPrefab, ref GenerationStats stats)
        {
            // 按 typeID 分组统计数量
            var grouped = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in typeIDs)
            {
                string key = string.IsNullOrEmpty(id) ? string.Empty : id;
                grouped.TryGetValue(key, out int cnt);
                grouped[key] = cnt + 1;
            }

            var result = new List<(GameObject, int)>();
            foreach (var kvp in grouped)
            {
                GameObject prefab = null;
                if (!string.IsNullOrEmpty(kvp.Key))
                {
                    string prefabPath = $"Assets/_Prefabs/Enemies/{kvp.Key}.prefab";
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"{LOG_TAG} ⚠️ EnemyPrefab '{kvp.Key}' not found at '{prefabPath}', falling back to Enemy_Rusher");
                        stats.Warnings.Add($"EnemyPrefab '{kvp.Key}' not found, using Enemy_Rusher fallback.");
                        prefab = fallbackPrefab;
                    }
                }
                else
                {
                    prefab = fallbackPrefab; // empty EnemyTypeID → use default
                }
                result.Add((prefab, kvp.Value));
            }

            // 如果没有任何 typeID，至少保留一个默认 entry
            if (result.Count == 0)
                result.Add((fallbackPrefab, 1));

            return result;
        }

        private EncounterSO CreateEncounterSO(string path, RoomType roomType, GameObject enemyPrefab,
            List<string> typeIDs, ref GenerationStats stats)
        {
            var so = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EncounterSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var entries = ResolveEnemyEntries(typeIDs, enemyPrefab, ref stats);

            var serialized = new SerializedObject(so);
            var wavesProp = serialized.FindProperty("_waves");

            if (roomType == RoomType.Arena)
            {
                // 1 wave, entries per type (count=3 per type)
                wavesProp.arraySize = 1;
                var wave0 = wavesProp.GetArrayElementAtIndex(0);
                wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
                var entries0 = wave0.FindPropertyRelative("Entries");
                entries0.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries0.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("EnemyPrefab").objectReferenceValue = entries[i].prefab;
                    e.FindPropertyRelative("Count").intValue = Mathf.Max(entries[i].count, 3);
                }
            }
            else // Boss
            {
                // 2 waves: wave 1 = entries (delay 0), wave 2 = entries (delay 1.5, count+1)
                wavesProp.arraySize = 2;

                var wave0 = wavesProp.GetArrayElementAtIndex(0);
                wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
                var entries0 = wave0.FindPropertyRelative("Entries");
                entries0.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries0.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("EnemyPrefab").objectReferenceValue = entries[i].prefab;
                    e.FindPropertyRelative("Count").intValue = Mathf.Max(entries[i].count, 2);
                }

                var wave1 = wavesProp.GetArrayElementAtIndex(1);
                wave1.FindPropertyRelative("DelayBeforeWave").floatValue = 1.5f;
                var entries1 = wave1.FindPropertyRelative("Entries");
                entries1.arraySize = entries.Count;
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries1.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("EnemyPrefab").objectReferenceValue = entries[i].prefab;
                    e.FindPropertyRelative("Count").intValue = Mathf.Max(entries[i].count, 3);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
            return so;
        }

        private EncounterSO CreateEncounterSO_Normal(string path, GameObject enemyPrefab,
            List<string> typeIDs, ref GenerationStats stats)
        {
            var so = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EncounterSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var entries = ResolveEnemyEntries(typeIDs, enemyPrefab, ref stats);

            var serialized = new SerializedObject(so);
            var wavesProp = serialized.FindProperty("_waves");

            // 1 wave, entries per type (count=2 per type, lighter than Arena)
            wavesProp.arraySize = 1;
            var wave0 = wavesProp.GetArrayElementAtIndex(0);
            wave0.FindPropertyRelative("DelayBeforeWave").floatValue = 0f;
            var entries0 = wave0.FindPropertyRelative("Entries");
            entries0.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries0.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("EnemyPrefab").objectReferenceValue = entries[i].prefab;
                e.FindPropertyRelative("Count").intValue = Mathf.Max(entries[i].count, 2);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
            return so;
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 7: VALIDATION REPORT
        // ══════════════════════════════════════════════════════════════

        private void PrintReport(GenerationStats stats,
            Dictionary<string, ScaffoldRoom> scaffoldLookup,
            Dictionary<string, List<Transform>> roomEnemySpawns,
            Dictionary<string, List<string>> roomEnemyTypeIDs)
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

            // 需求5：输出每个房间的敌人类型汇总
            bool hasEnemyInfo = false;
            foreach (var sr in scaffoldLookup.Values)
            {
                if (!roomEnemyTypeIDs.TryGetValue(sr.RoomID, out var ids) || ids.Count == 0) continue;
                if (!hasEnemyInfo)
                {
                    Debug.Log("───────────────────────────────────────────────────────────────");
                    Debug.Log("  👾 ENEMY TYPE SUMMARY:");
                    hasEnemyInfo = true;
                }
                var grouped = ids
                    .GroupBy(id => string.IsNullOrEmpty(id) ? "(default)" : id)
                    .Select(g => $"{g.Key} ×{g.Count()}");
                Debug.Log($"  • {sr.DisplayName}: {string.Join(", ", grouped)}");
            }

            // 需求2：输出使用了 fallback 尺寸的房间
            if (stats.FallbackRooms != null && stats.FallbackRooms.Count > 0)
            {
                Debug.Log("───────────────────────────────────────────────────────────────");
                Debug.Log("  ⚠️ ROOMS USING FALLBACK SIZE (Size was zero/invalid):");
                foreach (var r in stats.FallbackRooms)
                    Debug.LogWarning($"  • {r}");
            }

            // 需求6：输出被保留的房间
            if (stats.PreservedRooms != null && stats.PreservedRooms.Count > 0)
            {
                Debug.Log("───────────────────────────────────────────────────────────────");
                Debug.Log("  ✅ PRESERVED ROOMS (Update Existing mode, Tilemaps kept):");
                foreach (var r in stats.PreservedRooms)
                    Debug.Log($"  • {r}");
            }

            // TODO checklist
            Debug.Log("───────────────────────────────────────────────────────────────");
            Debug.Log("  📋 TODO CHECKLIST (manual follow-up):");
            Debug.Log("  1. 🖌️  Tilemap 层级已自动创建，直接选中对应层开始绘制");
            Debug.Log("  2. 👾  Check [DEFAULT] EncounterSO — EnemyTypeID 已自动填充，如需调整请手动修改");
            Debug.Log("  3. 🐉  Configure Boss room with dedicated Boss Prefab");
            Debug.Log("  4. 🎨  Add visual decorations, lighting, and particle effects");
            Debug.Log("  5. 🖼️  Add SpriteRenderer to Checkpoint GameObjects");
            Debug.Log("  6. ⚙️  Configure Physics2D collision matrix (Player vs Room/Door/Checkpoint)");
            Debug.Log("  7. 🎨  [ ] Hide/Remove Gizmo visuals (SpriteRenderer + TextMesh Label children) before shipping");
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════════════════════
        //  GIZMO TOGGLE (需求7)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// 需求7：遍历场景中所有 Gizmo 可视化组件，统一设置 enabled 状态。
        /// 目标对象：sortingOrder==1 的 SpriteRenderer，以及名为 "Label" 的 MeshRenderer。
        /// </summary>
        private void ToggleGizmos()
        {
            bool visible = _gizmosVisible;
            int count = 0;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var allGOs = scene.GetRootGameObjects();

            // 递归遍历所有 GameObject
            var queue = new Queue<GameObject>();
            foreach (var root in allGOs)
                queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var go = queue.Dequeue();

                // SpriteRenderer with sortingOrder == 1 → Gizmo color block
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sortingOrder == 1)
                {
                    sr.enabled = visible;
                    count++;
                }

                // MeshRenderer on a GameObject named "Label" → Gizmo text label
                if (go.name == "Label")
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.enabled = visible;
                        count++;
                    }
                }

                // Enqueue children
                foreach (Transform child in go.transform)
                    queue.Enqueue(child.gameObject);
            }

            if (count == 0)
                Debug.Log($"{LOG_TAG} No Gizmo visuals found in scene");
            else
                Debug.Log($"{LOG_TAG} Gizmos {(visible ? "shown" : "hidden")} — {count} component(s) toggled.");
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
            // Replace invalid filename chars, common punctuation, and spaces with underscores
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0 || c == '·' || c == '→' || c == ' ')
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
#endif
