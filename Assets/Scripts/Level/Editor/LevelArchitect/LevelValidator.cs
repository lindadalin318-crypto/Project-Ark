using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
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
                ValidateRoomGeometryAuthoring(room);
                ValidateArenaBossConfig(room);
                ValidateRoomEncounterMode(room);
            }

            ValidateLocks();
            ValidateCheckpoints();
            ValidateOpenEncounterTriggers();
            ValidateHiddenAreaMasks();
            ValidateBreakableWalls();
            ValidateBiomeTriggers();
            ValidateScheduledBehaviours();
            ValidateActivationGroups();
            ValidateEnvironmentHazards();
            ValidatePreferredAuthoringRoots();


            ValidateDoorBidirectional(rooms);
            ValidateDoorCeremonyConsistency(rooms);

            ValidateDoorPlayerLayer(rooms);
            ValidateDoorTargetSpawnPoint(rooms);
            ValidateDoorGeometryAlignment(rooms);
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

        // Rule 3: Optional CameraConfiner integrity / HardConfine requirements
        private static void ValidateCameraConfiner(Room room)
        {
            bool requiresHardConfine = room.UsesHardCameraConfine;
            var confinerTransform = room.transform.Find("CameraConfiner");
            var assignedBounds = room.ConfinerBounds;
            var poly = confinerTransform != null ? confinerTransform.GetComponent<PolygonCollider2D>() : null;

            if (!requiresHardConfine && confinerTransform == null)
            {
                return;
            }

            bool missingTransform = confinerTransform == null;
            bool missingPolygon = confinerTransform != null && poly == null;
            bool wrongLayer = confinerTransform != null && confinerTransform.gameObject.layer != 2;
            bool blockingPhysics = poly != null && !poly.isTrigger;
            bool missingBoundsReference = requiresHardConfine && assignedBounds == null;
            bool mismatchedBoundsReference = requiresHardConfine && poly != null && assignedBounds != poly;

            if (!(missingTransform || missingPolygon || wrongLayer || blockingPhysics || missingBoundsReference || mismatchedBoundsReference))
            {
                return;
            }

            var issues = new List<string>();
            if (missingTransform) issues.Add("missing CameraConfiner child");
            if (missingPolygon) issues.Add("missing PolygonCollider2D");
            if (wrongLayer) issues.Add("not on Ignore Raycast layer");
            if (blockingPhysics) issues.Add("PolygonCollider2D is not set as Trigger");
            if (missingBoundsReference) issues.Add("Room._confinerBounds is unassigned");
            if (mismatchedBoundsReference) issues.Add("Room._confinerBounds does not point to CameraConfiner");

            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Warning,
                Message = requiresHardConfine
                    ? $"Room '{room.RoomID}' uses HardConfine but CameraConfiner authoring is incomplete: {string.Join(", ", issues)}."
                    : $"Room '{room.RoomID}' has an optional CameraConfiner authoring issue: {string.Join(", ", issues)}.",
                TargetObject = confinerTransform != null ? confinerTransform.gameObject : room,
                CanAutoFix = true,
                FixAction = () =>
                {
                    Transform targetTransform = confinerTransform;
                    PolygonCollider2D targetPoly = poly;

                    if (targetTransform == null)
                    {
                        var confinerGO = new GameObject("CameraConfiner");
                        Undo.RegisterCreatedObjectUndo(confinerGO, "Create CameraConfiner");
                        confinerGO.transform.SetParent(room.transform, false);
                        confinerGO.layer = 2;
                        targetTransform = confinerGO.transform;
                    }
                    else
                    {
                        Undo.RecordObject(targetTransform.gameObject, "Fix CameraConfiner");
                        targetTransform.gameObject.layer = 2;
                    }

                    if (targetPoly == null)
                    {
                        targetPoly = targetTransform.gameObject.GetComponent<PolygonCollider2D>();
                        if (targetPoly == null)
                        {
                            targetPoly = targetTransform.gameObject.AddComponent<PolygonCollider2D>();
                        }
                    }

                    Undo.RecordObject(targetPoly, "Fix CameraConfiner Collider");
                    targetPoly.isTrigger = true;

                    if (targetPoly.points == null || targetPoly.points.Length < 3)
                    {
                        var box = room.GetComponent<BoxCollider2D>();
                        if (box != null)
                        {
                            float hw = box.size.x / 2f - 0.1f;
                            float hh = box.size.y / 2f - 0.1f;
                            targetPoly.points = new Vector2[]
                            {
                                new Vector2(-hw, -hh), new Vector2(hw, -hh),
                                new Vector2(hw, hh), new Vector2(-hw, hh)
                            };
                        }
                    }

                    if (requiresHardConfine)
                    {
                        var serialized = new SerializedObject(room);
                        serialized.FindProperty("_confinerBounds").objectReferenceValue = targetPoly;
                        serialized.ApplyModifiedProperties();
                    }
                }
            });
        }


        // Rule 3.5: Standard room hierarchy
        private static void ValidateStandardRoomHierarchy(Room room)
        {
            string[] requiredRoots =
            {
                RoomAuthoringHierarchy.NavigationRootName,
                RoomAuthoringHierarchy.ElementsRootName,
                RoomAuthoringHierarchy.EncountersRootName,
                RoomAuthoringHierarchy.HazardsRootName,
                RoomAuthoringHierarchy.DecorationRootName,
                RoomAuthoringHierarchy.TriggersRootName
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
                    RoomAuthoringHierarchy.EnsureForRoom(room.transform);
                }
            });
        }

        private static void ValidateRoomGeometryAuthoring(Room room)
        {
            var navigationRoot = room.transform.Find(RoomAuthoringHierarchy.NavigationRootName);
            if (navigationRoot == null)
            {
                return;
            }

            var geometryRoot = navigationRoot.Find(RoomAuthoringHierarchy.GeometryRootName);
            if (geometryRoot == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' is missing Navigation/Geometry root for static geometry authoring.",
                    TargetObject = navigationRoot.gameObject,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        RoomAuthoringHierarchy.EnsureForRoom(room.transform);
                    }
                });
                return;
            }

            var outerWallsRoot = geometryRoot.Find(RoomAuthoringHierarchy.OuterWallsRootName);
            if (outerWallsRoot == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' is missing Navigation/Geometry/OuterWalls root.",
                    TargetObject = geometryRoot.gameObject,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        RoomAuthoringHierarchy.EnsureForRoom(room.transform);
                    }
                });
            }

            var innerWallsRoot = geometryRoot.Find(RoomAuthoringHierarchy.InnerWallsRootName);
            if (innerWallsRoot == null)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Info,
                    Message = $"Room '{room.RoomID}' is missing Navigation/Geometry/InnerWalls root.",
                    TargetObject = geometryRoot.gameObject,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        RoomAuthoringHierarchy.EnsureForRoom(room.transform);
                    }
                });
            }

            ValidateGeometryRootMarker(room, geometryRoot);

            if (outerWallsRoot != null)
            {
                ValidateOuterWallsCollisionChain(room, outerWallsRoot);
            }

            ValidateMisplacedNamedWallTilemaps(room, RoomAuthoringHierarchy.ElementsRootName);
            ValidateMisplacedNamedWallTilemaps(room, RoomAuthoringHierarchy.TriggersRootName);
        }

        private static void ValidateGeometryRootMarker(Room room, Transform geometryRoot)
        {
            var markers = geometryRoot.GetComponents<RoomGeometryRoot>();
            if (markers.Length == 0)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' Navigation/Geometry is missing RoomGeometryRoot marker.",
                    TargetObject = geometryRoot.gameObject,
                    CanAutoFix = true,
                    FixAction = () =>
                    {
                        Undo.AddComponent<RoomGeometryRoot>(geometryRoot.gameObject);
                    }
                });
                return;
            }

            if (markers.Length > 1)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Error,
                    Message = $"Room '{room.RoomID}' Navigation/Geometry has multiple RoomGeometryRoot components.",
                    TargetObject = geometryRoot.gameObject,
                    CanAutoFix = false,
                    FixAction = null
                });
            }

            var components = geometryRoot.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null || component is Transform || component is RoomGeometryRoot)
                {
                    continue;
                }

                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' Navigation/Geometry should stay marker-only, but found '{component.GetType().Name}'.",
                    TargetObject = component,
                    CanAutoFix = false,
                    FixAction = null
                });
            }
        }

        private static void ValidateOuterWallsCollisionChain(Room room, Transform outerWallsRoot)
        {
            var tilemapColliders = outerWallsRoot.GetComponentsInChildren<TilemapCollider2D>(true);
            if (tilemapColliders.Length == 0)
            {
                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Room '{room.RoomID}' OuterWalls has no TilemapCollider2D chain yet.",
                    TargetObject = outerWallsRoot.gameObject,
                    CanAutoFix = false,
                    FixAction = null
                });
                return;
            }

            foreach (var tilemapCollider in tilemapColliders)
            {
                if (tilemapCollider == null)
                {
                    continue;
                }

                if (tilemapCollider.GetComponent<Tilemap>() == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Outer wall '{tilemapCollider.gameObject.name}' is missing Tilemap component.",
                        TargetObject = tilemapCollider.gameObject,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                var compositeCollider = tilemapCollider.GetComponent<CompositeCollider2D>();
                if (compositeCollider == null)
                {
                    continue;
                }

                var rigidbody = tilemapCollider.GetComponent<Rigidbody2D>();
                if (rigidbody == null || rigidbody.bodyType != RigidbodyType2D.Static)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Outer wall '{tilemapCollider.gameObject.name}' uses CompositeCollider2D but is missing a Static Rigidbody2D.",
                        TargetObject = tilemapCollider.gameObject,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (tilemapCollider.compositeOperation == Collider2D.CompositeOperation.None)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"Outer wall '{tilemapCollider.gameObject.name}' has CompositeCollider2D but TilemapCollider2D is not configured to use composite.",
                        TargetObject = tilemapCollider.gameObject,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static void ValidateMisplacedNamedWallTilemaps(Room room, string rootName)
        {
            var root = room.transform.Find(rootName);
            if (root == null)
            {
                return;
            }

            var tilemapColliders = root.GetComponentsInChildren<TilemapCollider2D>(true);
            foreach (var tilemapCollider in tilemapColliders)
            {
                if (tilemapCollider == null)
                {
                    continue;
                }

                string objectName = tilemapCollider.gameObject.name;
                bool looksLikeWallRoot = objectName.StartsWith(RoomAuthoringHierarchy.OuterWallsRootName, StringComparison.Ordinal)
                    || objectName.StartsWith(RoomAuthoringHierarchy.InnerWallsRootName, StringComparison.Ordinal);

                if (!looksLikeWallRoot)
                {
                    continue;
                }

                _lastResults.Add(new ValidationResult
                {
                    Severity = Severity.Warning,
                    Message = $"Named wall tilemap '{objectName}' is under '{rootName}' in Room '{room.RoomID}'. Static walls should live under Navigation/Geometry.",
                    TargetObject = tilemapCollider.gameObject,
                    CanAutoFix = false,
                    FixAction = null
                });
            }
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

        private static void ValidateOpenEncounterTriggers()
        {
            var openEncounters = UnityEngine.Object.FindObjectsByType<OpenEncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var openEncounter in openEncounters)
            {
                if (openEncounter == null) continue;

                ValidateTriggerCollider(openEncounter, "OpenEncounterTrigger");
                ValidateLayerMask(openEncounter, "_playerLayer", "OpenEncounterTrigger");

                var serialized = new SerializedObject(openEncounter);
                var encounterProperty = serialized.FindProperty("_encounter");
                var encounter = encounterProperty.objectReferenceValue as EncounterSO;
                if (encounter == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"OpenEncounterTrigger '{openEncounter.gameObject.name}' is missing EncounterSO reference.",
                        TargetObject = openEncounter,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
                else if (encounter.Mode != EncounterMode.Open)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"OpenEncounterTrigger '{openEncounter.gameObject.name}' uses EncounterSO '{encounter.name}' with Mode '{encounter.Mode}'. Open encounters should use Mode 'Open'.",
                        TargetObject = encounter,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                bool hasAssignedSpawner = serialized.FindProperty("_spawner").objectReferenceValue != null;
                bool hasChildSpawner = openEncounter.GetComponentInChildren<EnemySpawner>(true) != null;
                if (!hasAssignedSpawner && !hasChildSpawner)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"OpenEncounterTrigger '{openEncounter.gameObject.name}' has no EnemySpawner reference or child spawner.",
                        TargetObject = openEncounter,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                var parentRoom = openEncounter.GetComponentInParent<Room>();
                if (parentRoom == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"OpenEncounterTrigger '{openEncounter.gameObject.name}' is not nested under a Room.",
                        TargetObject = openEncounter,
                        CanAutoFix = false,
                        FixAction = null
                    });
                    continue;
                }

                if (parentRoom.Data != null && parentRoom.Data.HasEncounter)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Room '{parentRoom.RoomID}' mixes RoomSO encounter with OpenEncounterTrigger '{openEncounter.gameObject.name}'. Choose a single encounter owner.",
                        TargetObject = openEncounter,
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

        private static void ValidateBreakableWalls()
        {
            var breakableWalls = UnityEngine.Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var breakableWall in breakableWalls)
            {
                if (breakableWall == null) continue;

                if (breakableWall.GetComponentInParent<Room>() == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"BreakableWall '{breakableWall.gameObject.name}' is not nested under a Room.",
                        TargetObject = breakableWall,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (breakableWall.GetComponent<DestroyableObject>() == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"BreakableWall '{breakableWall.gameObject.name}' is missing DestroyableObject.",
                        TargetObject = breakableWall,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                if (breakableWall.GetComponent<Collider2D>() == null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Error,
                        Message = $"BreakableWall '{breakableWall.gameObject.name}' is missing Collider2D.",
                        TargetObject = breakableWall,
                        CanAutoFix = true,
                        FixAction = () =>
                        {
                            Undo.AddComponent<BoxCollider2D>(breakableWall.gameObject);
                        }
                    });
                }

                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0 && breakableWall.gameObject.layer != wallLayer)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"BreakableWall '{breakableWall.gameObject.name}' should be on Wall layer so environment-hit weapons can target it.",
                        TargetObject = breakableWall,
                        CanAutoFix = true,
                        FixAction = () =>
                        {
                            Undo.RecordObject(breakableWall.gameObject, "Fix BreakableWall Layer");
                            breakableWall.gameObject.layer = wallLayer;
                        }
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

        private static void ValidateEnvironmentHazards()
        {
            var hazards = UnityEngine.Object.FindObjectsByType<EnvironmentHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hazard in hazards)
            {
                if (hazard == null) continue;

                string componentLabel = hazard.GetType().Name;
                ValidateTriggerCollider(hazard, componentLabel);
                ValidateLayerMask(hazard, "_targetLayer", componentLabel);
            }
        }

        private static void ValidatePreferredAuthoringRoots()

        {
            ValidatePreferredRoot<Lock>("Lock", "Elements");
            ValidatePreferredRoot<Checkpoint>("Checkpoint", "Elements");
            ValidatePreferredRoot<Door>("Door", "Navigation");
            ValidatePreferredRoot<DestroyableObject>("DestroyableObject", "Elements");
            ValidatePreferredRoot<BreakableWall>("BreakableWall", "Elements");
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

        // Rule 4: Surface authored one-way doors (legal but worth confirming)
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
                            Severity = Severity.Info,
                            Message = $"Door '{capturedDoor.gameObject.name}' in '{capturedRoom.RoomID}' is authored as one-way to '{capturedDoor.TargetRoom.RoomID}'. This is legal; add a reverse door only if traversal should be bidirectional.",
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

        private static void ValidateDoorCeremonyConsistency(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;
                var doors = room.GetComponentsInChildren<Door>(true);

                foreach (var door in doors)
                {
                    if (door == null || door.TargetRoom == null) continue;

                    var expectedCeremony = DoorWiringService.GetExpectedCeremony(room, door.TargetRoom);
                    if (door.Ceremony == expectedCeremony) continue;

                    var capturedRoom = room;
                    var capturedDoor = door;
                    var capturedExpectedCeremony = expectedCeremony;

                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Door '{capturedDoor.gameObject.name}' in '{capturedRoom.RoomID}' has ceremony '{capturedDoor.Ceremony}' but expected '{capturedExpectedCeremony}'.",
                        TargetObject = capturedDoor,
                        CanAutoFix = true,
                        FixAction = () => DoorWiringService.SynchronizeRoomConnections(capturedRoom)
                    });
                }
            }
        }

        private static void ValidateRoomEncounterMode(Room room)
        {
            if (room?.Data == null || room.Data.Encounter == null)
            {
                return;
            }

            if (room.Data.Encounter.Mode == EncounterMode.Closed)
            {
                return;
            }

            _lastResults.Add(new ValidationResult
            {
                Severity = Severity.Warning,
                Message = $"Room '{room.RoomID}' uses RoomSO Encounter '{room.Data.Encounter.name}' with Mode '{room.Data.Encounter.Mode}'. Room-owned encounters should use Mode 'Closed'; open encounters belong on OpenEncounterTrigger.",
                TargetObject = room.Data.Encounter,
                CanAutoFix = false,
                FixAction = null
            });
        }

        // Rule 5: Arena/Boss rooms must use ArenaController as their ceremony orchestrator.
        private static void ValidateArenaBossConfig(Room room)
        {
            var arenaController = room.GetComponent<ArenaController>();
            bool isArenaBossRoom = room.NodeType == RoomNodeType.Arena || room.NodeType == RoomNodeType.Boss;

            if (!isArenaBossRoom)
            {
                if (arenaController != null)
                {
                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Room '{room.RoomID}' ({room.NodeType}) has ArenaController but is not Arena/Boss. Remove the controller or change the room NodeType.",
                        TargetObject = arenaController,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }

                return;
            }

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
                        continue;
                    }

                    if (door.TargetRoom == null) continue;

                    var targetBox = door.TargetRoom.GetComponent<BoxCollider2D>();
                    if (targetBox == null) continue;

                    Rect targetRect = LevelArchitectWindow.GetRoomWorldRect(door.TargetRoom, targetBox);
                    if (targetRect.Contains(door.TargetSpawnPoint.position)) continue;

                    var capturedRoom = room;
                    var capturedDoor = door;

                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Door '{capturedDoor.gameObject.name}' in '{capturedRoom.RoomID}' points to spawn '{capturedDoor.TargetSpawnPoint.name}' outside target room '{capturedDoor.TargetRoom.RoomID}'.",
                        TargetObject = capturedDoor.TargetSpawnPoint,
                        CanAutoFix = true,
                        FixAction = () => DoorWiringService.SynchronizeRoomConnections(capturedRoom)
                    });
                }
            }
        }

        private static void ValidateDoorGeometryAlignment(Room[] rooms)
        {
            foreach (var room in rooms)
            {
                if (room == null) continue;

                var geometryRoot = room.transform.Find($"{RoomAuthoringHierarchy.NavigationRootName}/{RoomAuthoringHierarchy.GeometryRootName}");
                if (geometryRoot == null)
                {
                    continue;
                }

                var wallTilemaps = geometryRoot.GetComponentsInChildren<Tilemap>(true);
                if (wallTilemaps.Length == 0)
                {
                    continue;
                }

                var doors = room.GetComponentsInChildren<Door>(true);
                foreach (var door in doors)
                {
                    if (door == null) continue;
                    if (!TryGetImmediateRoomRoot(door.transform, out Room ownerRoom, out string rootName)) continue;
                    if (ownerRoom != room) continue;
                    if (!string.Equals(rootName, RoomAuthoringHierarchy.NavigationRootName, StringComparison.Ordinal)) continue;

                    Vector3 probePoint = TryGetDoorProbePoint(door);
                    if (!DoorProbeHitsFilledWallTile(wallTilemaps, probePoint))
                    {
                        continue;
                    }

                    _lastResults.Add(new ValidationResult
                    {
                        Severity = Severity.Warning,
                        Message = $"Door '{door.gameObject.name}' in Room '{room.RoomID}' overlaps filled wall geometry. Door-type paths should align with a geometry opening instead of sitting inside a filled wall tilemap.",
                        TargetObject = door,
                        CanAutoFix = false,
                        FixAction = null
                    });
                }
            }
        }

        private static Vector3 TryGetDoorProbePoint(Door door)
        {
            var collider = door.GetComponent<Collider2D>();
            return collider != null ? collider.bounds.center : door.transform.position;
        }

        private static bool DoorProbeHitsFilledWallTile(Tilemap[] tilemaps, Vector3 worldPoint)
        {
            foreach (var tilemap in tilemaps)
            {
                if (tilemap == null)
                {
                    continue;
                }

                var cell = tilemap.WorldToCell(worldPoint);
                if (tilemap.GetTile(cell) != null)
                {
                    return true;
                }
            }

            return false;
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
