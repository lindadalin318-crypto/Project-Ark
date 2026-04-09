using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Builds a complete level scene skeleton from a LevelDesigner.html exported JSON file.
    /// Replaces the old ShebaSliceBuilder (hardcoded) with a data-driven, reusable importer.
    ///
    /// Workflow:
    ///   `LevelArchitectWindow` Design tab → Import LevelDesigner JSON
    ///   → Room GameObjects + RoomSO assets + Door connections in active scene
    /// </summary>
    public static class LevelSliceBuilder
    {
        // ──────────────────── JSON Data Models ────────────────────

        [Serializable]
        private class LevelJson
        {
            public string levelName;
            public RoomJson[] rooms;
            public ConnectionJson[] connections;
        }

        [Serializable]
        private class RoomJson
        {
            public string id;
            public string name;
            public string type;   // Unity canonical: "transit"|"pressure"|"resolution"|"reward"|"anchor"|"loop"|"hub"|"threshold"|"safe"|"boss"
                                  // HTML aliases:    "normal"→pressure, "arena"→resolution, "narrative"→anchor, "puzzle"→reward, "corridor"→transit
            public int floor;
            public float[] position; // [x, y] in grid units
            public float[] size;     // [w, h] in grid units
            public string zoneId;
            public string act;
            public int tension;
            public string beatName;
        }

        [Serializable]
        private class ConnectionJson
        {
            public string from;
            public string to;
            public string fromDir;  // "east" | "west" | "north" | "south"
            public string toDir;
            public string connectionType; // Unity canonical: "progression"|"return"|"ability"|"challenge"|"identity"|"scheduled"
                                          // HTML aliases:    "normal"→progression, "tidal"→scheduled, "locked"→ability, "one_way"→progression
        }

        // ──────────────────── Constants ────────────────────

        private const float GRID_TO_WORLD = 1f;   // 1 grid unit = 1 Unity unit
        private const string ROOM_DATA_BASE = "Assets/_Data/Level/Rooms/";
        private const string ENCOUNTER_DATA_BASE = "Assets/_Data/Level/Encounters/";
        private const int IGNORE_RAYCAST_LAYER = 2;

        // ──────────────────── Menu Entry ────────────────────

        public static void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel(
                "Select LevelDesigner JSON",
                Application.dataPath,
                "json");

            if (string.IsNullOrEmpty(path)) return;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Cannot read file:\n{ex.Message}", "OK");
                return;
            }

            LevelJson data;
            try
            {
                data = JsonUtility.FromJson<LevelJson>(json);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", $"JSON parse error:\n{ex.Message}", "OK");
                return;
            }

            if (data == null || data.rooms == null || data.rooms.Length == 0)
            {
                EditorUtility.DisplayDialog("Import Failed", "No rooms found in JSON.", "OK");
                return;
            }

            string levelName = string.IsNullOrEmpty(data.levelName) ? "NewLevel" : data.levelName;

            bool confirm = EditorUtility.DisplayDialog(
                "Import Level Slice",
                $"Level: {levelName}\n" +
                $"Rooms: {data.rooms.Length}\n" +
                $"Connections: {data.connections?.Length ?? 0}\n\n" +
                "This will create Room GameObjects and RoomSO assets in the active scene.\n" +
                "Continue?",
                "Import", "Cancel");

            if (!confirm) return;

            BuildFromJson(data, levelName);
        }

        // ──────────────────── Core Build Logic ────────────────────

        private static void BuildFromJson(LevelJson data, string levelName)
        {
            Undo.SetCurrentGroupName($"Import Level Slice: {levelName}");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Sanitize level name for directory use
                string safeName = SanitizeName(levelName);
                string roomDataDir = $"{ROOM_DATA_BASE}{safeName}/";
                string encounterDataDir = $"{ENCOUNTER_DATA_BASE}{safeName}/";

                EnsureDirectoryExists(roomDataDir);

                // ── Scene root ──
                var sliceRoot = new GameObject($"── {levelName} ──");
                Undo.RegisterCreatedObjectUndo(sliceRoot, "Create Slice Root");
                sliceRoot.transform.position = Vector3.zero;

                // ── Step 1: Create Room GameObjects ──
                var roomMap = new Dictionary<string, Room>();
                var doorSpawnMap = new Dictionary<string, Transform>(); // key: "roomId_dir"

                foreach (var rj in data.rooms)
                {
                    if (rj == null || string.IsNullOrEmpty(rj.id)) continue;

                    Vector3 worldPos = JsonPosToWorld(rj.position);
                    Vector2 worldSize = JsonSizeToWorld(rj.size);
                    RoomNodeType nodeType = ParseNodeType(rj.type);
                    bool needsEncounter = nodeType == RoomNodeType.Resolution || nodeType == RoomNodeType.Boss;

                    // Create RoomSO
                    EncounterSO encounter = null;
                    if (needsEncounter)
                    {
                        EnsureDirectoryExists(encounterDataDir);
                        encounter = CreateEncounterSO(encounterDataDir, $"{rj.id}_Encounter");
                    }

                    RoomSO roomSO = CreateRoomSO(roomDataDir, rj, nodeType, encounter);

                    // Create Room GameObject
                    Room room = CreateRoomGameObject(rj, roomSO, worldPos, worldSize, nodeType, sliceRoot.transform);
                    roomMap[rj.id] = room;
                }

                // ── Step 2: Create Doors from connections ──
                if (data.connections != null)
                {
                    foreach (var conn in data.connections)
                    {
                        if (conn == null) continue;
                        if (!roomMap.TryGetValue(conn.from, out var fromRoom)) continue;
                        if (!roomMap.TryGetValue(conn.to, out var toRoom)) continue;

                        ConnectionType connType = ParseConnectionType(conn.connectionType);

                        // Create door pair
                        var fromDoor = CreateDoor(fromRoom, conn.fromDir, conn.to, connType);
                        var toDoor = CreateDoor(toRoom, conn.toDir, conn.from, connType);

                        // Wire cross-references
                        if (fromDoor.door != null && toDoor.door != null)
                        {
                            WireDoor(fromDoor.door, toRoom, toDoor.spawnPoint);
                            WireDoor(toDoor.door, fromRoom, fromDoor.spawnPoint);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Undo.CollapseUndoOperations(undoGroup);

                Selection.activeGameObject = sliceRoot;
                SceneView.RepaintAll();

                Debug.Log($"[LevelSliceBuilder] ✅ Imported '{levelName}': {data.rooms.Length} rooms, {data.connections?.Length ?? 0} connections.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelSliceBuilder] ❌ Import failed: {ex.Message}\n{ex.StackTrace}");
                Undo.RevertAllDownToGroup(undoGroup);
            }
        }

        // ──────────────────── Asset Creation ────────────────────

        private static RoomSO CreateRoomSO(string dir, RoomJson rj, RoomNodeType nodeType, EncounterSO encounter)
        {
            var roomSO = ScriptableObject.CreateInstance<RoomSO>();
            roomSO.name = $"{rj.id}_Data";

            var so = new SerializedObject(roomSO);
            so.FindProperty("_roomID").stringValue = rj.id;
            so.FindProperty("_displayName").stringValue = string.IsNullOrEmpty(rj.name) ? rj.id : rj.name;
            so.FindProperty("_nodeType").enumValueIndex = (int)nodeType;
            so.FindProperty("_floorLevel").intValue = rj.floor;
            if (encounter != null)
                so.FindProperty("_encounter").objectReferenceValue = encounter;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{dir}{roomSO.name}.asset";
            CreateOrReplaceAsset(roomSO, path);
            return AssetDatabase.LoadAssetAtPath<RoomSO>(path);
        }

        private static EncounterSO CreateEncounterSO(string dir, string assetName)
        {
            var enc = ScriptableObject.CreateInstance<EncounterSO>();
            enc.name = assetName;

            var so = new SerializedObject(enc);
            so.FindProperty("_mode").enumValueIndex = (int)EncounterMode.Closed;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{dir}{assetName}.asset";
            CreateOrReplaceAsset(enc, path);
            return AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
        }

        // ──────────────────── Scene Object Creation ────────────────────

        private static Room CreateRoomGameObject(
            RoomJson rj, RoomSO roomSO, Vector3 position, Vector2 size,
            RoomNodeType nodeType, Transform parent)
        {
            var roomGO = new GameObject(rj.id);
            Undo.RegisterCreatedObjectUndo(roomGO, $"Create Room {rj.id}");
            roomGO.transform.SetParent(parent, false);
            roomGO.transform.position = position;

            var room = roomGO.AddComponent<Room>();

            var box = roomGO.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;

            // Standard hierarchy
            var navRoot = CreateChild(roomGO.transform, "Navigation");
            CreateChild(roomGO.transform, "Elements");
            var encRoot = CreateChild(roomGO.transform, "Encounters");
            CreateChild(roomGO.transform, "Hazards");
            CreateChild(roomGO.transform, "Decoration");
            CreateChild(roomGO.transform, "Triggers");

            // Camera confiner
            var confinerGO = CreateChild(roomGO.transform, "CameraConfiner");
            confinerGO.layer = IGNORE_RAYCAST_LAYER;
            var poly = confinerGO.AddComponent<PolygonCollider2D>();
            SetConfinerBounds(poly, size);

            // Navigation placeholders
            CreateChild(navRoot.transform, "Doors");
            CreateChild(navRoot.transform, "SpawnPoints");

            // Encounter spawn points
            var encSpawnRoot = CreateChild(encRoot.transform, "SpawnPoints");
            Transform[] spawnPoints = new Transform[0];

            bool isCombat = nodeType == RoomNodeType.Resolution || nodeType == RoomNodeType.Boss;
            if (isCombat)
            {
                spawnPoints = CreateSpawnPoints(encSpawnRoot.transform, size, 4);
                roomGO.AddComponent<ArenaController>();
            }

            // Configure Room
            var serialized = new SerializedObject(room);
            serialized.FindProperty("_data").objectReferenceValue = roomSO;
            serialized.FindProperty("_confinerBounds").objectReferenceValue = poly;

            var spProp = serialized.FindProperty("_spawnPoints");
            spProp.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(roomGO);

            return room;
        }

        private static (Door door, Transform spawnPoint) CreateDoor(
            Room ownerRoom, string direction, string targetRoomId, ConnectionType connType)
        {
            var doorsParent = ownerRoom.transform.Find("Navigation/Doors");
            if (doorsParent == null)
            {
                Debug.LogError($"[LevelSliceBuilder] Room {ownerRoom.name} missing Navigation/Doors!");
                return (null, null);
            }

            string doorName = $"Door_{direction}_{targetRoomId}";
            string gateID = $"gate_{direction}_{targetRoomId}";

            var doorGO = new GameObject(doorName);
            Undo.RegisterCreatedObjectUndo(doorGO, $"Create Door {doorName}");
            doorGO.transform.SetParent(doorsParent, false);

            var box = ownerRoom.GetComponent<BoxCollider2D>();
            Vector2 roomSize = box != null ? box.size : new Vector2(20, 15);
            doorGO.transform.localPosition = GetDoorEdgePosition(roomSize, direction);

            var col = doorGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f);

            var door = doorGO.AddComponent<Door>();

            // Spawn point
            var spawnParent = ownerRoom.transform.Find("Navigation/SpawnPoints");
            var spawnGO = new GameObject($"SpawnPoint_{direction}");
            Undo.RegisterCreatedObjectUndo(spawnGO, $"Create SpawnPoint {direction}");
            spawnGO.transform.SetParent(spawnParent != null ? spawnParent : doorsParent, false);
            spawnGO.transform.localPosition = doorGO.transform.localPosition + GetSpawnOffset(direction);

            // Configure door
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_gateID").stringValue = gateID;
            serialized.FindProperty("_connectionType").enumValueIndex = (int)connType;
            serialized.FindProperty("_initialState").enumValueIndex = (int)DoorState.Open;
            serialized.FindProperty("_ceremony").enumValueIndex = (int)TransitionCeremony.Standard;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(doorGO);

            return (door, spawnGO.transform);
        }

        private static void WireDoor(Door door, Room targetRoom, Transform targetSpawn)
        {
            if (door == null) return;
            var serialized = new SerializedObject(door);
            serialized.FindProperty("_targetRoom").objectReferenceValue = targetRoom;
            serialized.FindProperty("_targetSpawnPoint").objectReferenceValue = targetSpawn;
            serialized.ApplyModifiedProperties();
        }

        // ──────────────────── Geometry Helpers ────────────────────

        private static Vector3 GetDoorEdgePosition(Vector2 roomSize, string dir)
        {
            float hx = roomSize.x * 0.5f;
            float hy = roomSize.y * 0.5f;
            return dir switch
            {
                "east"  => new Vector3(hx, 0, 0),
                "west"  => new Vector3(-hx, 0, 0),
                "north" => new Vector3(0, hy, 0),
                "south" => new Vector3(0, -hy, 0),
                _       => Vector3.zero
            };
        }

        private static Vector3 GetSpawnOffset(string dir)
        {
            const float INWARD = 2.5f;
            return dir switch
            {
                "east"  => new Vector3(-INWARD, 0, 0),
                "west"  => new Vector3(INWARD, 0, 0),
                "north" => new Vector3(0, -INWARD, 0),
                "south" => new Vector3(0, INWARD, 0),
                _       => Vector3.zero
            };
        }

        private static void SetConfinerBounds(PolygonCollider2D poly, Vector2 size)
        {
            float m = 0.1f;
            float hw = size.x * 0.5f - m;
            float hh = size.y * 0.5f - m;
            poly.points = new Vector2[]
            {
                new Vector2(-hw, -hh), new Vector2(hw, -hh),
                new Vector2(hw, hh),   new Vector2(-hw, hh)
            };
        }

        private static Transform[] CreateSpawnPoints(Transform parent, Vector2 roomSize, int count)
        {
            var pts = new Transform[count];
            Vector2 half = roomSize * 0.3f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"SpawnPoint_{i}");
                go.transform.SetParent(parent, false);
                float angle = 2f * Mathf.PI * i / count;
                go.transform.localPosition = new Vector3(Mathf.Cos(angle) * half.x, Mathf.Sin(angle) * half.y, 0);
                pts[i] = go.transform;
            }
            return pts;
        }

        // ──────────────────── Parsing Helpers ────────────────────

        private static Vector3 JsonPosToWorld(float[] pos)
        {
            if (pos == null || pos.Length < 2) return Vector3.zero;
            return new Vector3(pos[0] * GRID_TO_WORLD, -pos[1] * GRID_TO_WORLD, 0); // Y is flipped (HTML canvas Y-down)
        }

        private static Vector2 JsonSizeToWorld(float[] size)
        {
            if (size == null || size.Length < 2) return new Vector2(20, 15);
            return new Vector2(size[0] * GRID_TO_WORLD, size[1] * GRID_TO_WORLD);
        }

        private static RoomNodeType ParseNodeType(string type)
        {
            // Canonical mapping: HTML type strings → RoomNodeType enum.
            // HTML is the consumer; Unity RoomNodeType is the single source of truth.
            return (type ?? "").ToLower() switch
            {
                "transit"    => RoomNodeType.Transit,
                "pressure"   => RoomNodeType.Pressure,
                "resolution" => RoomNodeType.Resolution,
                "reward"     => RoomNodeType.Reward,
                "anchor"     => RoomNodeType.Anchor,
                "loop"       => RoomNodeType.Loop,
                "hub"        => RoomNodeType.Hub,
                "threshold"  => RoomNodeType.Threshold,
                "safe"       => RoomNodeType.Safe,
                "boss"       => RoomNodeType.Boss,
                // Legacy / HTML-side aliases
                "arena"      => RoomNodeType.Resolution,   // arena = closed encounter = Resolution
                "normal"     => RoomNodeType.Pressure,     // normal = open combat room = Pressure
                "narrative"  => RoomNodeType.Anchor,       // narrative landmark = Anchor
                "puzzle"     => RoomNodeType.Reward,       // puzzle/secret reward = Reward
                "corridor"   => RoomNodeType.Transit,      // corridor = pass-through = Transit
                _            => RoomNodeType.Transit
            };
        }

        private static ConnectionType ParseConnectionType(string type)
        {
            // Canonical mapping: HTML connectionType strings → ConnectionType enum.
            // HTML is the consumer; Unity ConnectionType is the single source of truth.
            return (type ?? "").ToLower() switch
            {
                "progression" => ConnectionType.Progression,
                "return"      => ConnectionType.Return,
                "ability"     => ConnectionType.Ability,
                "challenge"   => ConnectionType.Challenge,
                "identity"    => ConnectionType.Identity,
                "scheduled"   => ConnectionType.Scheduled,
                // Legacy / HTML-side aliases
                "normal"      => ConnectionType.Progression,  // default HTML type = main route
                "tidal"       => ConnectionType.Scheduled,    // tidal door = time-controlled = Scheduled
                "locked"      => ConnectionType.Ability,      // key-locked = ability gate = Ability
                "one_way"     => ConnectionType.Progression,  // one-way passage = progression
                "secret"      => ConnectionType.Ability,      // secret passage = ability gate
                _             => ConnectionType.Progression
            };
        }

        // ──────────────────── Utility ────────────────────

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void CreateOrReplaceAsset(UnityEngine.Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
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
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizeName(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            return sb.Length > 0 ? sb.ToString() : "Level";
        }
    }
}
