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

            var rooms = UnityEngine.Object.FindObjectsByType<Room>();

            foreach (var room in rooms)
            {
                if (room == null) continue;

                ValidateRoomSO(room);
                ValidateBoxColliderTrigger(room);
                ValidateCameraConfiner(room);
                ValidateStandardRoomHierarchy(room);
                ValidateArenaBossConfig(room);
            }

            ValidateLocks();
            ValidateCheckpoints();
            ValidateHiddenAreaMasks();
            ValidateBiomeTriggers();
            ValidateScheduledBehaviours();
            ValidateActivationGroups();
            ValidatePreferredAuthoringRoots();

            ValidateDoorBidirectional(rooms);

            ValidateDoorPlayerLayer(rooms);
            ValidateDoorTargetSpawnPoint(rooms);
            ValidateOrphanRooms(rooms);


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

            var rooms = UnityEngine.Object.FindObjectsByType<Room>();
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
            else
            {
                var poly = confinerTransform.GetComponent<PolygonCollider2D>();
                bool wrongLayer = confinerTransform.gameObject.layer != 2; // Ignore Raycast = 2
                bool missingPolygon = poly == null;
                bool blockingPhysics = poly != null && !poly.isTrigger;

                if (wrongLayer || missingPolygon || blockingPhysics)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = missingPolygon
                            ? $"Room '{room.RoomID}' CameraConfiner is missing PolygonCollider2D."
                            : blockingPhysics
                                ? $"Room '{room.RoomID}' CameraConfiner PolygonCollider2D is not set as Trigger."
                                : $"Room '{room.RoomID}' CameraConfiner is not on Ignore Raycast layer.",
                        TargetObject = confinerTransform.gameObject,
                        CanAutoFix = true,
                        FixAction = () =>
                        {
                            Undo.RecordObject(confinerTransform.gameObject, "Fix CameraConfiner");
                            confinerTransform.gameObject.layer = 2;

                            var targetPoly = poly;
                            if (targetPoly == null)
                            {
                                targetPoly = confinerTransform.gameObject.AddComponent<PolygonCollider2D>();
                            }

                            targetPoly.isTrigger = true;
                        }
                    });
                }
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

        // Rule 3.7: Lock / Checkpoint / Trigger family
        private static void ValidateLocks()
        {
            var locks = UnityEngine.Object.FindObjectsByType<Lock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lockComponent in locks)
            {
                if (lockComponent == null) continue;

                ValidateTriggerCollider(lockComponent, "Lock");
                ValidateLayerMask(lockComponent, "_playerLayer", "Lock");

                var serialized = new SerializedObject(lockComponent);
                if (serialized.FindProperty("_requiredKey").objectReferenceValue == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Lock '{lockComponent.gameObject.name}' is missing required key reference.",
                        TargetObject = lockComponent,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (serialized.FindProperty("_targetDoor").objectReferenceValue == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Lock '{lockComponent.gameObject.name}' is missing target door reference.",
                        TargetObject = lockComponent,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateCheckpoints()
        {
            var checkpoints = UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint == null) continue;

                ValidateTriggerCollider(checkpoint, "Checkpoint");
                ValidateLayerMask(checkpoint, "_playerLayer", "Checkpoint");

                var serialized = new SerializedObject(checkpoint);
                if (serialized.FindProperty("_data").objectReferenceValue == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Checkpoint '{checkpoint.gameObject.name}' is missing CheckpointSO reference.",
                        TargetObject = checkpoint,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateHiddenAreaMasks()
        {
            var masks = UnityEngine.Object.FindObjectsByType<HiddenAreaMask>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mask in masks)
            {
                if (mask == null) continue;

                ValidateTriggerCollider(mask, "HiddenAreaMask");
                ValidateLayerMask(mask, "_playerLayer", "HiddenAreaMask");

                if (mask.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"HiddenAreaMask '{mask.gameObject.name}' has no SpriteRenderer on itself or children.",
                        TargetObject = mask,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateBiomeTriggers()
        {
            var biomeTriggers = UnityEngine.Object.FindObjectsByType<BiomeTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var biomeTrigger in biomeTriggers)
            {
                if (biomeTrigger == null) continue;

                ValidateTriggerCollider(biomeTrigger, "BiomeTrigger");
                ValidateLayerMask(biomeTrigger, "_playerLayer", "BiomeTrigger");

                var serialized = new SerializedObject(biomeTrigger);
                if (serialized.FindProperty("_ambiencePreset").objectReferenceValue == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"BiomeTrigger '{biomeTrigger.gameObject.name}' is missing RoomAmbienceSO reference.",
                        TargetObject = biomeTrigger,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateScheduledBehaviours()
        {
            var scheduledBehaviours = UnityEngine.Object.FindObjectsByType<ScheduledBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var scheduledBehaviour in scheduledBehaviours)
            {
                if (scheduledBehaviour == null) continue;

                var serialized = new SerializedObject(scheduledBehaviour);
                if (serialized.FindProperty("_activePhaseIndices").arraySize == 0)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"ScheduledBehaviour '{scheduledBehaviour.gameObject.name}' has no active phase indices configured.",
                        TargetObject = scheduledBehaviour,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (serialized.FindProperty("_targetGameObject").objectReferenceValue == null && scheduledBehaviour.transform.childCount == 0)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"ScheduledBehaviour '{scheduledBehaviour.gameObject.name}' has no target GameObject and no child objects to toggle.",
                        TargetObject = scheduledBehaviour,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateTriggerCollider(Component component, string componentLabel)
        {
            var collider = component.GetComponent<Collider2D>();
            if (collider == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Error,
                    Message = $"{componentLabel} '{component.gameObject.name}' is missing Collider2D.",
                    TargetObject = component,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        var newCollider = component.gameObject.AddComponent<BoxCollider2D>();
                        newCollider.isTrigger = true;
                    }
                });
                return;
            }

            if (collider.isTrigger)
            {
                return;
            }

            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Warning,
                Message = $"{componentLabel} '{component.gameObject.name}' collider is not set as Trigger.",
                TargetObject = component,
                CanAutoFix = true,
                FixAction = () =>
                {
                    Undo.RecordObject(collider, $"Fix {componentLabel} Trigger");
                    collider.isTrigger = true;
                }
            });
        }

        private static void ValidateLayerMask(Component component, string fieldName, string componentLabel)
        {
            var serialized = new SerializedObject(component);
            int bits = serialized.FindProperty(fieldName).FindPropertyRelative("m_Bits").intValue;
            if (bits != 0)
            {
                return;
            }

            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Warning,
                Message = $"{componentLabel} '{component.gameObject.name}' has no {fieldName} configured.",
                TargetObject = component,
                CanAutoFix = true,
                FixAction = () =>
                {
                    int playerLayer = LayerMask.NameToLayer("Player");
                    if (playerLayer >= 0)
                    {
                        var s = new SerializedObject(component);
                        s.FindProperty(fieldName).FindPropertyRelative("m_Bits").intValue = 1 << playerLayer;
                        s.ApplyModifiedProperties();
                    }
                }
            });
        }

        private static void ValidateActivationGroups()
        {
            var activationGroups = UnityEngine.Object.FindObjectsByType<ActivationGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var activationGroup in activationGroups)
            {
                if (activationGroup == null) continue;

                var parentRoom = activationGroup.GetComponentInParent<Room>();
                if (parentRoom == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"ActivationGroup '{activationGroup.gameObject.name}' is not a child of a Room and will not respond to room enter/exit events.",
                        TargetObject = activationGroup,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                var serialized = new SerializedObject(activationGroup);
                var members = serialized.FindProperty("_members");
                bool hasAssignedMember = false;
                bool hasNullMember = false;

                for (int i = 0; i < members.arraySize; i++)
                {
                    if (members.GetArrayElementAtIndex(i).objectReferenceValue is GameObject member)
                    {
                        hasAssignedMember = true;

                        if (parentRoom != null)
                        {
                            var memberRoom = member.GetComponentInParent<Room>();
                            if (memberRoom != null && memberRoom != parentRoom)
                            {
                                _lastResults.Add(new ValidationResult
                                {
                                    Severity = Severity.Warning,
                                    Message = $"ActivationGroup '{activationGroup.gameObject.name}' references member '{member.name}' from a different Room.",
                                    TargetObject = activationGroup,
                                    CanAutoFix = false,
                                    FixAction = null
                                });
                            }
                        }
                    }
                    else
                    {
                        hasNullMember = true;
                    }
                }

                if (hasNullMember)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"ActivationGroup '{activationGroup.gameObject.name}' contains null entries in _members.",
                        TargetObject = activationGroup,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (!hasAssignedMember && activationGroup.transform.childCount == 0)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"ActivationGroup '{activationGroup.gameObject.name}' has no members or child objects to manage.",
                        TargetObject = activationGroup,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidatePreferredAuthoringRoots()
        {
            ValidatePreferredRoot<Lock>("Lock", "Elements");
            ValidatePreferredRoot<Checkpoint>("Checkpoint", "Elements");
            ValidatePreferredRoot<DestroyableObject>("DestroyableObject", "Elements");
            ValidatePreferredRoot<OpenEncounterTrigger>("OpenEncounterTrigger", "Encounters");
            ValidatePreferredRoot<BiomeTrigger>("BiomeTrigger", "Triggers");
            ValidatePreferredRoot<HiddenAreaMask>("HiddenAreaMask", "Triggers");
            ValidatePreferredRoot<ScheduledBehaviour>("ScheduledBehaviour", "Triggers");
            ValidatePreferredRoot<WorldEventTrigger>("WorldEventTrigger", "Triggers");
            ValidatePreferredRoot<EnvironmentHazard>("EnvironmentHazard", "Hazards");
            ValidatePreferredRoot<ActivationGroup>("ActivationGroup", "Triggers", "ActivationGroups");
        }

        private static void ValidatePreferredRoot<T>(string componentLabel, params string[] allowedRootNames) where T : Component
        {
            var components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (!TryGetImmediateRoomRoot(component.transform, out Room room, out string rootName))
                {
                    continue;
                }

                bool isAllowed = false;
                foreach (var allowedRoot in allowedRootNames)
                {
                    if (string.Equals(rootName, allowedRoot, StringComparison.Ordinal))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (isAllowed)
                {
                    continue;
                }

                string expectedRoots = string.Join(", ", allowedRootNames);
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"{componentLabel} '{component.gameObject.name}' is under root '{rootName}' in Room '{room.RoomID}', but should be placed under {expectedRoots}.",
                    TargetObject = component,
                    CanAutoFix = false,
                    FixAction = null
                });
            }
        }

        private static bool TryGetImmediateRoomRoot(Transform target, out Room room, out string rootName)
        {
            room = target.GetComponentInParent<Room>();
            rootName = null;
            if (room == null)
            {
                return false;
            }

            var current = target;
            while (current != null && current != room.transform)
            {
                if (current.parent == room.transform)
                {
                    rootName = current.name;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

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

    }
}
