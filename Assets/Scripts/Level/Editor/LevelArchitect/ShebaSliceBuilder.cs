using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [B4.2] One-click builder for the Sheba 7-room first slice.
    /// Creates RoomSO assets, EncounterSO for the Resolution room,
    /// Room GameObjects with standard hierarchy, and Door connections.
    ///
    /// Room layout (linear with one loop shortcut):
    ///   SH-R01 (Safe) → SH-R02 (Pressure) → SH-R03 (Anchor) → SH-R04 (Resolution)
    ///   → SH-R05 (Reward) → SH-R06 (Loop) → SH-R07 (Safe)
    ///                          ↑                      │
    ///                          └──── Return ──────────┘
    /// </summary>
    public static class ShebaSliceBuilder
    {
        // ──────────────────── Constants ────────────────────

        private const string ROOM_DATA_DIR = "Assets/_Data/Level/Rooms/Sheba/";
        private const string ENCOUNTER_DATA_DIR = "Assets/_Data/Level/Encounters/Sheba/";
        private const int IGNORE_RAYCAST_LAYER = 2;
        private const float ROOM_SPACING = 30f; // horizontal spacing between rooms
        private static readonly Vector2 DEFAULT_ROOM_SIZE = new Vector2(20f, 15f);
        private static readonly Vector2 RESOLUTION_ROOM_SIZE = new Vector2(24f, 18f); // slightly larger for combat

        // ──────────────────── Room Definitions ────────────────────

        private struct RoomDef
        {
            public string ID;
            public string DisplayName;
            public RoomNodeType NodeType;
            public RoomType LegacyType;
            public Vector2 Size;
            public bool HasEncounter;
        }

        private static readonly RoomDef[] ROOMS = new RoomDef[]
        {
            new RoomDef
            {
                ID = "SH-R01", DisplayName = "示巴星 · 入口",
                NodeType = RoomNodeType.Safe, LegacyType = RoomType.Safe,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
            new RoomDef
            {
                ID = "SH-R02", DisplayName = "示巴星 · 通道A",
                NodeType = RoomNodeType.Pressure, LegacyType = RoomType.Normal,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
            new RoomDef
            {
                ID = "SH-R03", DisplayName = "示巴星 · 信标台",
                NodeType = RoomNodeType.Anchor, LegacyType = RoomType.Normal,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
            new RoomDef
            {
                ID = "SH-R04", DisplayName = "示巴星 · 竞技场",
                NodeType = RoomNodeType.Resolution, LegacyType = RoomType.Arena,
                Size = RESOLUTION_ROOM_SIZE, HasEncounter = true
            },
            new RoomDef
            {
                ID = "SH-R05", DisplayName = "示巴星 · 宝藏室",
                NodeType = RoomNodeType.Reward, LegacyType = RoomType.Normal,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
            new RoomDef
            {
                ID = "SH-R06", DisplayName = "示巴星 · 回廊",
                NodeType = RoomNodeType.Loop, LegacyType = RoomType.Normal,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
            new RoomDef
            {
                ID = "SH-R07", DisplayName = "示巴星 · 闸口",
                NodeType = RoomNodeType.Safe, LegacyType = RoomType.Safe,
                Size = DEFAULT_ROOM_SIZE, HasEncounter = false
            },
        };

        // Door connection definitions: (fromIndex, toIndex, connectionType, gateIDSuffix)
        private struct DoorConnectionDef
        {
            public int FromRoom;
            public int ToRoom;
            public ConnectionType Type;
            public string FromGateSuffix; // e.g. "east" → gateID = "gate_east_SH-R02"
            public string ToGateSuffix;   // e.g. "west" → gateID = "gate_west_SH-R01"
        }

        private static readonly DoorConnectionDef[] CONNECTIONS = new DoorConnectionDef[]
        {
            // SH-R01 → SH-R02 (Progression)
            new DoorConnectionDef
            {
                FromRoom = 0, ToRoom = 1,
                Type = ConnectionType.Progression,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
            // SH-R02 → SH-R03 (Progression)
            new DoorConnectionDef
            {
                FromRoom = 1, ToRoom = 2,
                Type = ConnectionType.Progression,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
            // SH-R03 → SH-R04 (Challenge)
            new DoorConnectionDef
            {
                FromRoom = 2, ToRoom = 3,
                Type = ConnectionType.Challenge,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
            // SH-R04 → SH-R05 (Progression)
            new DoorConnectionDef
            {
                FromRoom = 3, ToRoom = 4,
                Type = ConnectionType.Progression,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
            // SH-R05 → SH-R06 (Progression)
            new DoorConnectionDef
            {
                FromRoom = 4, ToRoom = 5,
                Type = ConnectionType.Progression,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
            // SH-R06 → SH-R01 (Return / loop shortcut)
            new DoorConnectionDef
            {
                FromRoom = 5, ToRoom = 0,
                Type = ConnectionType.Return,
                FromGateSuffix = "north", ToGateSuffix = "south"
            },
            // SH-R06 → SH-R07 (Progression)
            new DoorConnectionDef
            {
                FromRoom = 5, ToRoom = 6,
                Type = ConnectionType.Progression,
                FromGateSuffix = "east", ToGateSuffix = "west"
            },
        };

        // ──────────────────── Menu Entry ────────────────────

        [MenuItem("ProjectArk/Level/Build Sheba 7-Room Slice", false, 200)]
        public static void BuildShebaSlice()
        {
            if (!EditorUtility.DisplayDialog(
                "Build Sheba 7-Room Slice",
                "This will create:\n" +
                "• 7 RoomSO assets in _Data/Level/Rooms/Sheba/\n" +
                "• 1 EncounterSO (placeholder) for SH-R04\n" +
                "• 7 Room GameObjects in the active scene\n" +
                "• Door connections between rooms\n\n" +
                "Existing assets with the same name will be OVERWRITTEN.\n" +
                "Continue?",
                "Build", "Cancel"))
            {
                return;
            }

            Undo.SetCurrentGroupName("Build Sheba 7-Room Slice");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Step 1: Ensure directories
                EnsureDirectoryExists(ROOM_DATA_DIR);
                EnsureDirectoryExists(ENCOUNTER_DATA_DIR);

                // Step 2: Create EncounterSO for SH-R04
                EncounterSO encounterR04 = CreateEncounterSO("SH-R04_Encounter", EncounterMode.Closed);

                // Step 3: Create RoomSO assets
                RoomSO[] roomSOs = new RoomSO[ROOMS.Length];
                for (int i = 0; i < ROOMS.Length; i++)
                {
                    var def = ROOMS[i];
                    EncounterSO encounter = def.HasEncounter ? encounterR04 : null;
                    roomSOs[i] = CreateRoomSO(def, encounter);
                }

                // Step 4: Create Room GameObjects in scene
                // Parent container
                var sliceRoot = new GameObject("── Sheba Slice ──");
                Undo.RegisterCreatedObjectUndo(sliceRoot, "Create Slice Root");
                sliceRoot.transform.position = Vector3.zero;

                Room[] rooms = new Room[ROOMS.Length];
                Transform[][] doorSpawnPoints = new Transform[ROOMS.Length][];
                for (int i = 0; i < ROOMS.Length; i++)
                {
                    var def = ROOMS[i];
                    Vector3 pos = new Vector3(i * ROOM_SPACING, 0f, 0f);
                    rooms[i] = CreateRoomGameObject(def, roomSOs[i], pos, sliceRoot.transform, out doorSpawnPoints[i]);
                }

                // Step 5: Create and wire Doors
                // We need a lookup: (roomIndex, gateSuffix) → (Door, spawnPoint)
                // For each connection, create a door pair (bidirectional)
                var doorLookup = new Dictionary<(int roomIdx, string gateSuffix), (Door door, Transform spawnPoint)>();

                foreach (var conn in CONNECTIONS)
                {
                    // Create door in FromRoom → pointing to ToRoom
                    var fromDoor = CreateDoor(
                        rooms[conn.FromRoom], conn.FromGateSuffix,
                        ROOMS[conn.FromRoom], ROOMS[conn.ToRoom],
                        conn.Type
                    );

                    // Create door in ToRoom → pointing to FromRoom (reverse)
                    var toDoor = CreateDoor(
                        rooms[conn.ToRoom], conn.ToGateSuffix,
                        ROOMS[conn.ToRoom], ROOMS[conn.FromRoom],
                        conn.Type == ConnectionType.Return ? ConnectionType.Return : conn.Type
                    );

                    doorLookup[(conn.FromRoom, conn.FromGateSuffix)] = fromDoor;
                    doorLookup[(conn.ToRoom, conn.ToGateSuffix)] = toDoor;
                }

                // Step 6: Wire door targets (cross-reference)
                foreach (var conn in CONNECTIONS)
                {
                    var fromEntry = doorLookup[(conn.FromRoom, conn.FromGateSuffix)];
                    var toEntry = doorLookup[(conn.ToRoom, conn.ToGateSuffix)];

                    WireDoorTarget(fromEntry.door, rooms[conn.ToRoom], toEntry.spawnPoint);
                    WireDoorTarget(toEntry.door, rooms[conn.FromRoom], fromEntry.spawnPoint);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Undo.CollapseUndoOperations(undoGroup);

                Selection.activeGameObject = sliceRoot;
                SceneView.RepaintAll();

                Debug.Log(
                    $"[ShebaSliceBuilder] ✅ Successfully built Sheba 7-room slice!\n" +
                    $"  • {ROOMS.Length} rooms created\n" +
                    $"  • {CONNECTIONS.Length * 2} doors wired\n" +
                    $"  • 1 EncounterSO created (SH-R04, Closed, placeholder waves)"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShebaSliceBuilder] ❌ Build failed: {ex.Message}\n{ex.StackTrace}");
                Undo.RevertAllDownToGroup(undoGroup);
            }
        }

        // ──────────────────── Asset Creation ────────────────────

        private static RoomSO CreateRoomSO(RoomDef def, EncounterSO encounter)
        {
            var roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{def.ID}_Data";

            var so = new SerializedObject(roomSO);
            so.FindProperty("_roomID").stringValue = def.ID;
            so.FindProperty("_displayName").stringValue = def.DisplayName;
            so.FindProperty("_type").enumValueIndex = (int)def.LegacyType;
            so.FindProperty("_nodeType").enumValueIndex = (int)def.NodeType;
            so.FindProperty("_useLegacyTypeMapping").boolValue = false; // 新房间显式指定 NodeType
            so.FindProperty("_floorLevel").intValue = 0;

            if (encounter != null)
            {
                so.FindProperty("_encounter").objectReferenceValue = encounter;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ROOM_DATA_DIR}{roomSO.name}.asset";
            CreateOrReplaceAsset(roomSO, path);

            return AssetDatabase.LoadAssetAtPath<RoomSO>(path);
        }

        private static EncounterSO CreateEncounterSO(string name, EncounterMode mode)
        {
            var encounterSO = ScriptableObject.CreateInstance<EncounterSO>();
            encounterSO.name = name;

            var so = new SerializedObject(encounterSO);
            so.FindProperty("_mode").enumValueIndex = (int)mode;
            // _waves left as empty array → placeholder for future population
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ENCOUNTER_DATA_DIR}{name}.asset";
            CreateOrReplaceAsset(encounterSO, path);

            return AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
        }

        // ──────────────────── Scene Object Creation ────────────────────

        private static Room CreateRoomGameObject(
            RoomDef def, RoomSO roomSO, Vector3 position, Transform parent,
            out Transform[] doorSpawnPoints)
        {
            // ── Root GameObject ──
            var roomGO = new GameObject(def.ID);
            Undo.RegisterCreatedObjectUndo(roomGO, $"Create Room {def.ID}");
            roomGO.transform.SetParent(parent, false);
            roomGO.transform.position = position;

            // ── Room Component ──
            var room = roomGO.AddComponent<Room>();

            // ── BoxCollider2D (Trigger for player detection) ──
            var boxCol = roomGO.GetComponent<BoxCollider2D>();
            if (boxCol == null) boxCol = roomGO.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = def.Size;

            // ── Standard hierarchy (matches RoomFactory pattern) ──
            var navigationRoot = CreateChild(roomGO.transform, "Navigation");
            CreateChild(roomGO.transform, "Elements");
            var encountersRoot = CreateChild(roomGO.transform, "Encounters");
            CreateChild(roomGO.transform, "Hazards");
            CreateChild(roomGO.transform, "Decoration");
            CreateChild(roomGO.transform, "Triggers");

            // ── Camera Confiner ──
            var confinerGO = CreateChild(roomGO.transform, "CameraConfiner");
            confinerGO.layer = IGNORE_RAYCAST_LAYER;
            var polyCol = confinerGO.AddComponent<PolygonCollider2D>();
            SetConfinerBounds(polyCol, def.Size);

            // ── Navigation placeholders ──
            CreateChild(navigationRoot.transform, "Doors");
            CreateChild(navigationRoot.transform, "SpawnPoints");

            // ── Encounter SpawnPoints ──
            var encSpawnRoot = CreateChild(encountersRoot.transform, "SpawnPoints");
            Transform[] spawnPoints;
            if (def.HasEncounter)
            {
                spawnPoints = CreateSpawnPoints(encSpawnRoot.transform, def.Size, 4);
                // Also add ArenaController for Resolution room
                roomGO.AddComponent<ArenaController>();
            }
            else
            {
                spawnPoints = new Transform[0];
            }

            // ── Configure Room via SerializedObject ──
            var serialized = new SerializedObject(room);
            serialized.FindProperty("_data").objectReferenceValue = roomSO;
            serialized.FindProperty("_confinerBounds").objectReferenceValue = polyCol;

            var spProp = serialized.FindProperty("_spawnPoints");
            spProp.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            int playerLayerIdx = LayerMask.NameToLayer("Player");
            if (playerLayerIdx >= 0)
            {
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayerIdx;
            }

            serialized.ApplyModifiedProperties();

            // Mark dirty
            EditorUtility.SetDirty(roomGO);

            doorSpawnPoints = spawnPoints; // not used for door wiring, kept for future
            return room;
        }

        /// <summary>
        /// Create a Door in the specified room, positioned based on gate direction suffix.
        /// Returns the Door component and its associated spawn point (where incoming players appear).
        /// </summary>
        private static (Door door, Transform spawnPoint) CreateDoor(
            Room ownerRoom, string gateSuffix,
            RoomDef ownerDef, RoomDef targetDef,
            ConnectionType connectionType)
        {
            var doorsParent = ownerRoom.transform.Find("Navigation/Doors");
            if (doorsParent == null)
            {
                Debug.LogError($"[ShebaSliceBuilder] Room {ownerDef.ID} missing Navigation/Doors!");
                return (null, null);
            }

            string doorName = $"Door_{gateSuffix}_{targetDef.ID}";
            string gateID = $"gate_{gateSuffix}_{targetDef.ID}";

            // ── Door GameObject ──
            var doorGO = new GameObject(doorName);
            Undo.RegisterCreatedObjectUndo(doorGO, $"Create Door {doorName}");
            doorGO.transform.SetParent(doorsParent, false);

            // Position door at room edge based on direction
            Vector3 doorLocalPos = GetDoorEdgePosition(ownerDef.Size, gateSuffix);
            doorGO.transform.localPosition = doorLocalPos;

            // ── Collider2D (trigger, required by Door) ──
            var col = doorGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f); // reasonable door trigger size

            // ── Door Component ──
            var door = doorGO.AddComponent<Door>();

            // ── Spawn Point (where player appears when arriving through this door) ──
            var spawnPointsParent = ownerRoom.transform.Find("Navigation/SpawnPoints");
            var spawnGO = new GameObject($"SpawnPoint_{gateSuffix}");
            Undo.RegisterCreatedObjectUndo(spawnGO, $"Create SpawnPoint {gateSuffix}");
            spawnGO.transform.SetParent(spawnPointsParent != null ? spawnPointsParent : doorsParent, false);
            // Slightly inward from door
            Vector3 spawnOffset = GetSpawnPointOffset(gateSuffix);
            spawnGO.transform.localPosition = doorLocalPos + spawnOffset;

            // ── Configure Door via SerializedObject ──
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_gateID").stringValue = gateID;
            serialized.FindProperty("_connectionType").enumValueIndex = (int)connectionType;
            serialized.FindProperty("_initialState").enumValueIndex = (int)DoorState.Open;
            serialized.FindProperty("_ceremony").enumValueIndex = (int)TransitionCeremony.Standard;

            int playerLayerIdx = LayerMask.NameToLayer("Player");
            if (playerLayerIdx >= 0)
            {
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayerIdx;
            }

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(doorGO);

            return (door, spawnGO.transform);
        }

        /// <summary>
        /// Wire a door's _targetRoom and _targetSpawnPoint references.
        /// Called in a second pass after all doors and spawn points exist.
        /// </summary>
        private static void WireDoorTarget(Door door, Room targetRoom, Transform targetSpawnPoint)
        {
            if (door == null) return;

            var serialized = new SerializedObject(door);
            serialized.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serialized.FindProperty("_targetSpawnPoint").objectReferenceValue = targetSpawnPoint;
            serialized.ApplyModifiedProperties();
        }

        // ──────────────────── Geometry Helpers ────────────────────

        private static Vector3 GetDoorEdgePosition(Vector2 roomSize, string direction)
        {
            float hx = roomSize.x * 0.5f;
            float hy = roomSize.y * 0.5f;

            return direction switch
            {
                "east" => new Vector3(hx, 0f, 0f),
                "west" => new Vector3(-hx, 0f, 0f),
                "north" => new Vector3(0f, hy, 0f),
                "south" => new Vector3(0f, -hy, 0f),
                _ => Vector3.zero
            };
        }

        private static Vector3 GetSpawnPointOffset(string direction)
        {
            // Offset inward from door edge (so player doesn't spawn inside wall)
            const float INWARD = 2.5f;
            return direction switch
            {
                "east" => new Vector3(-INWARD, 0f, 0f),
                "west" => new Vector3(INWARD, 0f, 0f),
                "north" => new Vector3(0f, -INWARD, 0f),
                "south" => new Vector3(0f, INWARD, 0f),
                _ => Vector3.zero
            };
        }

        private static void SetConfinerBounds(PolygonCollider2D poly, Vector2 size)
        {
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

        private static Transform[] CreateSpawnPoints(Transform parent, Vector2 roomSize, int count)
        {
            var points = new Transform[count];
            Vector2 halfSize = roomSize * 0.3f;

            for (int i = 0; i < count; i++)
            {
                var spGO = new GameObject($"SpawnPoint_{i}");
                spGO.transform.SetParent(parent, false);

                float angle = (2f * Mathf.PI * i) / count;
                float x = Mathf.Cos(angle) * halfSize.x;
                float y = Mathf.Sin(angle) * halfSize.y;
                spGO.transform.localPosition = new Vector3(x, y, 0f);

                points[i] = spGO.transform;
            }

            return points;
        }

        // ──────────────────── Utility ────────────────────

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void CreateOrReplaceAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureDirectoryExists(string path)
        {
            string trimmed = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(trimmed)) return;

            var parts = trimmed.Split('/');
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
