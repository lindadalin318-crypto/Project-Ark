using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectArk.Combat.Enemy;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.1]
    /// Validates level configuration and provides auto-fix capabilities.
    /// Runs 8 validation rules and reports results with severity and fix actions.
    /// </summary>
    public static class LevelValidator
    {
        // ──────────────────── Validation Result ────────────────────

        public enum Severity
        {
            Info,
            Warning,
            Error
        }

        public class ValidationResult
        {
            public Severity Severity;
            public string Message;
            public UnityEngine.Object TargetObject;
            public bool CanAutoFix;
            public Action FixAction;
        }

        // ──────────────────── Cached Results ────────────────────

        private static List<ValidationResult> _lastResults = new List<ValidationResult>();
        private static HashSet<int> _fatalRoomInstanceIDs = new HashSet<int>();

        /// <summary> Last validation results. </summary>
        public static List<ValidationResult> LastResults => _lastResults;

        /// <summary> Instance IDs of rooms with fatal issues (for SceneView overlay). </summary>
        public static HashSet<int> FatalRoomIDs => _fatalRoomInstanceIDs;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Run all validation rules on the current scene.
        /// Returns a list of all issues found.
        /// </summary>
        public static List<ValidationResult> ValidateAll()
        {
            _lastResults.Clear();
            _fatalRoomInstanceIDs.Clear();

            var rooms = UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

            foreach (var room in rooms)
            {
                if (room == null) continue;

                ValidateRoomSO(room);
                ValidateBoxColliderTrigger(room);
                ValidateCameraConfiner(room);
                ValidateStandardRoomHierarchy(room);
                ValidateArenaBossConfig(room);
            }

            ValidateDoorBidirectional(rooms);
            ValidateDoorPlayerLayer(rooms);
            ValidateDoorTargetSpawnPoint(rooms);
            ValidateOrphanRooms(rooms);

            // World Graph validation (new in Batch 1)
            ValidateWorldGraph(rooms);

            int errors = 0, warnings = 0, infos = 0;
            foreach (var r in _lastResults)
            {
                switch (r.Severity)
                {
                    case Severity.Error: errors++; break;
                    case Severity.Warning: warnings++; break;
                    case Severity.Info: infos++; break;
                }
            }

            Debug.Log($"[LevelValidator] Validation complete: {errors} errors, {warnings} warnings, {infos} info.");

            return _lastResults;
        }

        /// <summary>
        /// Lightweight check that runs every frame — only fatal issues (missing RoomSO).
        /// Updates FatalRoomIDs for SceneView overlay.
        /// </summary>
        public static void LightweightCheck()
        {
            _fatalRoomInstanceIDs.Clear();

            var rooms = UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            foreach (var room in rooms)
            {
                if (room == null) continue;

                if (room.Data == null)
                {
                    _fatalRoomInstanceIDs.Add(room.GetInstanceID());
                }
            }
        }

        /// <summary>
        /// Auto-fix all fixable issues.
        /// </summary>
        public static int AutoFixAll()
        {
            int fixed_count = 0;

            foreach (var result in _lastResults)
            {
                if (result.CanAutoFix && result.FixAction != null)
                {
                    try
                    {
                        result.FixAction();
                        fixed_count++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[LevelValidator] Auto-fix failed: {e.Message}");
                    }
                }
            }

            if (fixed_count > 0)
            {
                Debug.Log($"[LevelValidator] Auto-fixed {fixed_count} issue(s). Re-validating...");
                ValidateAll();
            }

            return fixed_count;
        }

        // ──────────────────── Validation Rules ────────────────────

        // Rule 1: Room missing RoomSO reference
        private static void ValidateRoomSO(Room room)
        {
            if (room.Data == null)
            {
                _fatalRoomInstanceIDs.Add(room.GetInstanceID());

                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Error,
                    Message = $"Room '{room.gameObject.name}' is missing RoomSO reference.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        // Auto-create a RoomSO
                        var roomSO = ScriptableObject.CreateInstance<RoomSO>();
                        roomSO.name = $"{room.gameObject.name}_Data";

                        var serializedSO = new SerializedObject(roomSO);
                        serializedSO.FindProperty("_roomID").stringValue = room.gameObject.name;
                        serializedSO.FindProperty("_displayName").stringValue = room.gameObject.name;
                        serializedSO.FindProperty("_nodeType").enumValueIndex = (int)RoomNodeType.Transit;
                        serializedSO.ApplyModifiedPropertiesWithoutUndo();

                        string path = $"Assets/_Data/Level/Rooms/{roomSO.name}.asset";
                        AssetDatabase.CreateAsset(roomSO, path);
                        AssetDatabase.SaveAssets();

                        var serializedRoom = new SerializedObject(room);
                        serializedRoom.FindProperty("_data").objectReferenceValue = roomSO;
                        serializedRoom.ApplyModifiedProperties();

                        Debug.Log($"[LevelValidator] Auto-created RoomSO at {path}");
                    }
                });
            }
        }

        // Rule 2: BoxCollider2D not trigger
        private static void ValidateBoxColliderTrigger(Room room)
        {
            var box = room.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Error,
                    Message = $"Room '{room.RoomID}' has no BoxCollider2D.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        var newBox = room.gameObject.AddComponent<BoxCollider2D>();
                        newBox.isTrigger = true;
                        newBox.size = new Vector2(20, 15);
                    }
                });
            }
            else if (!box.isTrigger)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' BoxCollider2D is not set as Trigger.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        Undo.RecordObject(box, "Fix Trigger");
                        box.isTrigger = true;
                    }
                });
            }
        }

        // Rule 3: Missing CameraConfiner or wrong layer
        private static void ValidateCameraConfiner(Room room)
        {
            var confinerTransform = room.transform.Find("CameraConfiner");

            if (confinerTransform == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' has no CameraConfiner child.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        var confinerGO = new GameObject("CameraConfiner");
                        Undo.RegisterCreatedObjectUndo(confinerGO, "Create Confiner");
                        confinerGO.transform.SetParent(room.transform, false);
                        confinerGO.layer = 2; // Ignore Raycast

                        var poly = confinerGO.AddComponent<PolygonCollider2D>();
                        var box = room.GetComponent<BoxCollider2D>();
                        if (box != null)
                        {
                            float hw = box.size.x / 2f - 0.1f;
                            float hh = box.size.y / 2f - 0.1f;
                            poly.points = new Vector2[]
                            {
                                new Vector2(-hw, -hh), new Vector2(hw, -hh),
                                new Vector2(hw, hh), new Vector2(-hw, hh)
                            };
                        }

                        // Assign to room
                        var serialized = new SerializedObject(room);
                        serialized.FindProperty("_confinerBounds").objectReferenceValue = poly;
                        serialized.ApplyModifiedProperties();
                    }
                });
            }
            else if (confinerTransform.gameObject.layer != 2) // Ignore Raycast = 2
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' CameraConfiner is not on Ignore Raycast layer.",
                    TargetObject = confinerTransform.gameObject,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        Undo.RecordObject(confinerTransform.gameObject, "Fix Confiner Layer");
                        confinerTransform.gameObject.layer = 2;
                    }
                });
            }
        }

        // Rule 3.5: Standard room hierarchy
        private static void ValidateStandardRoomHierarchy(Room room)
        {
            string[] requiredRoots =
            {
                "Navigation",
                "Elements",
                "Encounters",
                "Hazards",
                "Decoration",
                "Triggers"
            };

            var missingRoots = new List<string>();
            foreach (var rootName in requiredRoots)
            {
                if (room.transform.Find(rootName) == null)
                {
                    missingRoots.Add(rootName);
                }
            }

            if (missingRoots.Count == 0)
            {
                return;
            }

            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Warning,
                Message = $"Room '{room.RoomID}' is missing standard child roots: {string.Join(", ", missingRoots)}.",
                TargetObject = room,
                CanAutoFix = true,
                FixAction = () =>
                {
                    foreach (var rootName in missingRoots)
                    {
                        if (room.transform.Find(rootName) != null) continue;

                        var rootGO = new GameObject(rootName);
                        Undo.RegisterCreatedObjectUndo(rootGO, $"Create {rootName}");
                        rootGO.transform.SetParent(room.transform, false);
                    }
                }
            });
        }

        // Rule 3.6: NodeType migration state — removed (all rooms now use explicit NodeType)

        // Rule 4: Door connections not bidirectional
        private static void ValidateDoorBidirectional(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    // Check if reverse door exists
                    var targetDoors = door.TargetRoom.GetComponentsInChildren<Door>(true);
                    bool hasReverse = false;

                    foreach (var rd in targetDoors)
                    {
                        if (rd.TargetRoom == room)
                        {
                            hasReverse = true;
                            break;
                        }
                    }

                    if (!hasReverse)
                    {
                        var capturedDoor = door;
                        var capturedRoom = room;

                        _lastResults.Add(new ValidationResult
                        {
                            Severity = Severity.Error,
                            Message = $"Door '{capturedDoor.gameObject.name}' in '{capturedRoom.RoomID}' → '{capturedDoor.TargetRoom.RoomID}' has no reverse door.",
                            TargetObject = capturedDoor,
                            CanAutoFix = true,
                            FixAction = () =>
                            {
                                DoorWiringService.AutoConnectRooms(capturedRoom, capturedDoor.TargetRoom);
                            }
                        });
                    }
                }
            }
        }

        // Rule 5: Resolution/Boss rooms missing ArenaController or EncounterSO
        private static void ValidateArenaBossConfig(Room room)
        {
            if (room.NodeType != RoomNodeType.Resolution && room.NodeType != RoomNodeType.Boss) return;

            var arenaController = room.GetComponent<ArenaController>();
            if (arenaController == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' ({room.NodeType}) is missing ArenaController.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        Undo.RecordObject(room.gameObject, "Add ArenaController");
                        room.gameObject.AddComponent<ArenaController>();
                    }
                });
            }

            if (room.Data != null && room.Data.Encounter == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' ({room.NodeType}) has no EncounterSO configured.",
                    TargetObject = room.Data,
                    CanAutoFix = false,
                    FixAction = null
                });
            }

            var spawner = room.GetComponentInChildren<EnemySpawner>(true);
            if (spawner == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' ({room.NodeType}) is missing EnemySpawner child.",
                    TargetObject = room,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        var spawnerGO = new GameObject("EnemySpawner");
                        Undo.RegisterCreatedObjectUndo(spawnerGO, "Add EnemySpawner");

                        var encountersRoot = room.transform.Find("Encounters");
                        spawnerGO.transform.SetParent(encountersRoot != null ? encountersRoot : room.transform, false);
                        spawnerGO.AddComponent<EnemySpawner>();
                    }
                });
            }
        }

        // Rule 6: Door _playerLayer not configured
        private static void ValidateDoorPlayerLayer(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null) continue;

                    var serialized = new SerializedObject(door);
                    int bits = serialized.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue;

                    if (bits == 0)
                    {
                        var capturedDoor = door;

                        _lastResults.Add(new ValidationResult
                        {
                            Severity = Severity.Warning,
                            Message = $"Door '{capturedDoor.gameObject.name}' has no _playerLayer configured.",
                            TargetObject = capturedDoor,
                            CanAutoFix = true,
                            FixAction = () =>
                            {
                                int playerLayer = LayerMask.NameToLayer("Player");
                                if (playerLayer >= 0)
                                {
                                    var s = new SerializedObject(capturedDoor);
                                    s.FindProperty("_playerLayer").FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;
                                    s.ApplyModifiedProperties();
                                }
                            }
                        });
                    }
                }
            }
        }

        // Rule 7: Door _targetSpawnPoint is null
        private static void ValidateDoorTargetSpawnPoint(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null) continue;

                    if (door.TargetSpawnPoint == null)
                    {
                        _lastResults.Add(new ValidationResult
                        {
                            Severity = Severity.Error,
                            Message = $"Door '{door.gameObject.name}' in '{room.RoomID}' has null TargetSpawnPoint.",
                            TargetObject = door,
                            CanAutoFix = false,
                            FixAction = null
                        });
                    }
                }
            }
        }

        // Rule 8: Orphan rooms (no door connections)
        private static void ValidateOrphanRooms(Room[] rooms)
        {
            if (rooms.Length <= 1) return; // Single room is OK

            foreach (var room in rooms)
            {
                if (room == null) continue;

                var doors = room.GetComponentsInChildren<Door>(true);
                if (doors.Length == 0)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Info,
                        Message = $"Room '{room.RoomID}' is isolated (no door connections).",
                        TargetObject = room,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        // ──────────────────── World Graph Validation (Rules 9-11) ────────────────────

        /// <summary>
        /// Validate WorldGraphSO consistency with the scene.
        /// Searches for all WorldGraphSO assets and validates each.
        /// </summary>
        private static void ValidateWorldGraph(Room[] rooms)
        {
            // Find all WorldGraphSO assets in the project
            var graphGuids = AssetDatabase.FindAssets("t:WorldGraphSO");
            if (graphGuids.Length == 0) return; // No world graph configured — skip

            // Build scene room lookup
            var sceneRoomIDs = new HashSet<string>();
            var sceneRoomDoorGateIDs = new Dictionary<string, HashSet<string>>(); // roomID → set of gateIDs

            foreach (var room in rooms)
            {
                if (room == null || room.Data == null) continue;

                string id = room.RoomID;
                sceneRoomIDs.Add(id);

                var gateSet = new HashSet<string>();
                var doors = room.GetComponentsInChildren<Door>(true);
                foreach (var door in doors)
                {
                    if (door != null && !string.IsNullOrEmpty(door.GateID))
                    {
                        gateSet.Add(door.GateID);
                    }
                }
                sceneRoomDoorGateIDs[id] = gateSet;
            }

            // Validate each WorldGraphSO
            foreach (var guid in graphGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<WorldGraphSO>(assetPath);
                if (graph == null) continue;

                ValidateWorldGraph_RoomIDsExist(graph, sceneRoomIDs);
                ValidateWorldGraph_GateIDsMatch(graph, sceneRoomDoorGateIDs);
                ValidateWorldGraph_IsolatedRooms(graph);
            }
        }

        // Rule 9: WorldGraph RoomID not found in scene
        private static void ValidateWorldGraph_RoomIDsExist(WorldGraphSO graph, HashSet<string> sceneRoomIDs)
        {
            foreach (var roomID in graph.GetAllRoomIDs())
            {
                if (!sceneRoomIDs.Contains(roomID))
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"[WorldGraph '{graph.GraphName}'] RoomID '{roomID}' is defined in the graph but not found in the scene.",
                        TargetObject = graph,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        // Rule 10: WorldGraph GateID mismatch (graph defines a gate but no Door in scene has it)
        private static void ValidateWorldGraph_GateIDsMatch(
            WorldGraphSO graph,
            Dictionary<string, HashSet<string>> sceneRoomDoorGateIDs)
        {
            foreach (var conn in graph.Connections)
            {
                // Check FromGateID
                if (!string.IsNullOrEmpty(conn.FromGateID))
                {
                    if (sceneRoomDoorGateIDs.TryGetValue(conn.FromRoomID, out var fromGates))
                    {
                        if (!fromGates.Contains(conn.FromGateID))
                        {
                            _lastResults.Add(new ValidationResult
                            {
                                Severity = Severity.Warning,
                                Message = $"[WorldGraph '{graph.GraphName}'] GateID '{conn.FromGateID}' in room '{conn.FromRoomID}' " +
                                          $"is defined in the graph but no Door in the scene has this GateID.",
                                TargetObject = graph,
                                CanAutoFix = false,
                                FixAction = null
                            });
                        }
                    }
                }

                // Check ToGateID
                if (!string.IsNullOrEmpty(conn.ToGateID))
                {
                    if (sceneRoomDoorGateIDs.TryGetValue(conn.ToRoomID, out var toGates))
                    {
                        if (!toGates.Contains(conn.ToGateID))
                        {
                            _lastResults.Add(new ValidationResult
                            {
                                Severity = Severity.Warning,
                                Message = $"[WorldGraph '{graph.GraphName}'] GateID '{conn.ToGateID}' in room '{conn.ToRoomID}' " +
                                          $"is defined in the graph but no Door in the scene has this GateID.",
                                TargetObject = graph,
                                CanAutoFix = false,
                                FixAction = null
                            });
                        }
                    }
                }
            }
        }

        // Rule 11: WorldGraph isolated rooms (no connections)
        private static void ValidateWorldGraph_IsolatedRooms(WorldGraphSO graph)
        {
            var isolatedIDs = graph.GetIsolatedRoomIDs();
            foreach (var roomID in isolatedIDs)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"[WorldGraph '{graph.GraphName}'] Room '{roomID}' has no connections (isolated node).",
                    TargetObject = graph,
                    CanAutoFix = false,
                    FixAction = null
                });
            }
        }
    }
}
