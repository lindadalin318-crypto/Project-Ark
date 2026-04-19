using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Regression tests for `LevelValidator` rules covering high-value room element
    /// validation and authoring-root placement checks.
    /// </summary>
    [TestFixture]
    public class LevelValidatorTests
    {
        private readonly List<Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            LevelValidator.LastResults.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ValidateAll_ReportsError_WhenLockMissingRequiredKey()
        {
            var lockObject = CreateGameObjectWithTriggerColliderAndComponent<Lock>("Lock_MissingKey");
            SetPrivateField(lockObject.GetComponent<Lock>(), "_playerLayer", (LayerMask)1);
            SetPrivateField(lockObject.GetComponent<Lock>(), "_targetDoor", CreateGameObjectWithComponent<Door>("TargetDoor").GetComponent<Door>());

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == lockObject.GetComponent<Lock>() &&
                result.Severity == LevelValidator.Severity.Error &&
                result.Message.Contains("required key")));
        }

        [Test]
        public void ValidateAll_ReportsError_WhenCheckpointMissingData()
        {
            var checkpointObject = CreateGameObjectWithTriggerColliderAndComponent<Checkpoint>("Checkpoint_MissingData");
            SetPrivateField(checkpointObject.GetComponent<Checkpoint>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == checkpointObject.GetComponent<Checkpoint>() &&
                result.Severity == LevelValidator.Severity.Error &&
                result.Message.Contains("CheckpointSO")));
        }

        [Test]
        public void ValidateAll_ReportsError_WhenBiomeTriggerMissingPreset()
        {
            var biomeObject = CreateGameObjectWithComponent<BiomeTrigger>("Biome_MissingPreset");
            biomeObject.AddComponent<BoxCollider2D>().isTrigger = true;
            SetPrivateField(biomeObject.GetComponent<BiomeTrigger>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == biomeObject.GetComponent<BiomeTrigger>() &&
                result.Severity == LevelValidator.Severity.Error &&
                result.Message.Contains("RoomAmbienceSO")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenScheduledBehaviourHasNoActivePhases()
        {
            var scheduledObject = CreateGameObjectWithComponent<ScheduledBehaviour>("Scheduled_NoPhases");
            new GameObject("ChildVisual").transform.SetParent(scheduledObject.transform, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == scheduledObject.GetComponent<ScheduledBehaviour>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("active phase")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenActivationGroupHasNoRoomParent()
        {
            var activationObject = CreateGameObjectWithComponent<ActivationGroup>("Activation_NoRoom");
            new GameObject("MemberVisual").transform.SetParent(activationObject.transform, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == activationObject.GetComponent<ActivationGroup>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("not a child of a Room")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenActivationGroupHasNoMembersOrChildren()
        {
            var roomRig = CreateValidRoomRig("Room_Activation_Empty");
            var activationObject = CreateGameObjectWithComponent<ActivationGroup>("Activation_Empty");
            activationObject.transform.SetParent(roomRig.TriggersRoot, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == activationObject.GetComponent<ActivationGroup>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("has no members or child objects")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenActivationGroupPlacedOutsideTriggersFamilyRoots()
        {
            var roomRig = CreateValidRoomRig("Room_Activation_WrongRoot");
            var activationObject = CreateGameObjectWithComponent<ActivationGroup>("Activation_WrongRoot");
            activationObject.transform.SetParent(roomRig.ElementsRoot, false);
            new GameObject("MemberVisual").transform.SetParent(activationObject.transform, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == activationObject.GetComponent<ActivationGroup>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("ActivationGroup") &&
                result.Message.Contains("Triggers") &&
                result.Message.Contains("ActivationGroups")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenLockPlacedOutsideElementsRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Lock_WrongRoot");
            var lockObject = CreateGameObjectWithTriggerColliderAndComponent<Lock>("Lock_WrongRoot");
            lockObject.transform.SetParent(roomRig.TriggersRoot, false);
            SetPrivateField(lockObject.GetComponent<Lock>(), "_playerLayer", (LayerMask)1);
            SetPrivateField(lockObject.GetComponent<Lock>(), "_requiredKey", CreateScriptableObject<KeyItemSO>("TestKey"));
            SetPrivateField(lockObject.GetComponent<Lock>(), "_targetDoor", CreateGameObjectWithComponent<Door>("DetachedTargetDoor").GetComponent<Door>());

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == lockObject.GetComponent<Lock>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Lock") &&
                result.Message.Contains("Elements")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenBiomeTriggerPlacedOutsideTriggersRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Biome_WrongRoot");
            var biomeObject = CreateGameObjectWithComponent<BiomeTrigger>("Biome_WrongRoot");
            biomeObject.transform.SetParent(roomRig.ElementsRoot, false);
            biomeObject.AddComponent<BoxCollider2D>().isTrigger = true;
            SetPrivateField(biomeObject.GetComponent<BiomeTrigger>(), "_playerLayer", (LayerMask)1);
            SetPrivateField(biomeObject.GetComponent<BiomeTrigger>(), "_ambiencePreset", CreateScriptableObject<RoomAmbienceSO>("TestAmbience"));

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == biomeObject.GetComponent<BiomeTrigger>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("BiomeTrigger") &&
                result.Message.Contains("Triggers")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenEnvironmentHazardPlacedOutsideHazardsRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Hazard_WrongRoot");
            var hazardObject = CreateGameObjectWithTriggerColliderAndComponent<ContactHazard>("Hazard_WrongRoot");
            hazardObject.transform.SetParent(roomRig.ElementsRoot, false);
            SetPrivateField(hazardObject.GetComponent<ContactHazard>(), "_targetLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == hazardObject.GetComponent<ContactHazard>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("EnvironmentHazard") &&
                result.Message.Contains("Hazards")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenEnvironmentHazardHasNoTargetLayer()
        {
            var roomRig = CreateValidRoomRig("Room_Hazard_MissingLayer");
            var hazardObject = CreateGameObjectWithTriggerColliderAndComponent<ContactHazard>("Hazard_MissingLayer");
            hazardObject.transform.SetParent(roomRig.HazardsRoot, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == hazardObject.GetComponent<ContactHazard>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("_targetLayer")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenBreakableWallPlacedOutsideElementsRoot()
        {
            var roomRig = CreateValidRoomRig("Room_BreakableWall_WrongRoot");
            var wallObject = CreateGameObjectWithTriggerColliderAndComponent<BreakableWall>("BreakableWall_WrongRoot");
            wallObject.AddComponent<DestroyableObject>();
            wallObject.transform.SetParent(roomRig.TriggersRoot, false);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == wallObject.GetComponent<BreakableWall>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("BreakableWall") &&
                result.Message.Contains("Elements")));
        }

        [Test]
        public void ValidateAll_ReportsError_WhenBreakableWallMissingDestroyableObject()
        {
            var roomRig = CreateValidRoomRig("Room_BreakableWall_MissingDestroyable");
            var wallObject = CreateGameObjectWithTriggerColliderAndComponent<BreakableWall>("BreakableWall_MissingDestroyable");
            wallObject.transform.SetParent(roomRig.ElementsRoot, false);
            Object.DestroyImmediate(wallObject.GetComponent<DestroyableObject>());

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == wallObject.GetComponent<BreakableWall>() &&
                result.Severity == LevelValidator.Severity.Error &&
                result.Message.Contains("DestroyableObject")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenBreakableWallIsNotOnWallLayer()
        {
            var roomRig = CreateValidRoomRig("Room_BreakableWall_WrongLayer");
            var wallObject = CreateGameObjectWithTriggerColliderAndComponent<BreakableWall>("BreakableWall_WrongLayer");
            wallObject.transform.SetParent(roomRig.ElementsRoot, false);
            wallObject.AddComponent<DestroyableObject>();

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == wallObject.GetComponent<BreakableWall>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Wall layer")));
        }

        [Test]
        public void ValidateAll_DoesNotReportBreakableWallIssues_WhenAuthoringIsValid()
        {
            var roomRig = CreateValidRoomRig("Room_BreakableWall_Valid");
            var wallObject = CreateGameObjectWithTriggerColliderAndComponent<BreakableWall>("BreakableWall_Valid");
            wallObject.transform.SetParent(roomRig.ElementsRoot, false);
            wallObject.layer = LayerMask.NameToLayer("Wall");

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == wallObject.GetComponent<BreakableWall>() &&
                (result.Message.Contains("BreakableWall") || result.Message.Contains("DestroyableObject"))), Is.False);
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenNavigationGeometryRootMissing()
        {
            var roomRig = CreateValidRoomRig("Room_Geometry_MissingRoot");
            Object.DestroyImmediate(roomRig.GeometryRoot.gameObject);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == roomRig.NavigationRoot.gameObject &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Navigation/Geometry")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenGeometryRootMissingMarkerComponent()
        {
            var roomRig = CreateValidRoomRig("Room_Geometry_MissingMarker");
            Object.DestroyImmediate(roomRig.GeometryRoot.GetComponent<RoomGeometryRoot>());

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == roomRig.GeometryRoot.gameObject &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("RoomGeometryRoot")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenGeometryRootHasUnexpectedExtraComponent()
        {
            var roomRig = CreateValidRoomRig("Room_Geometry_UnexpectedComponent");
            roomRig.GeometryRoot.gameObject.AddComponent<BoxCollider2D>();

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == roomRig.GeometryRoot.GetComponent<BoxCollider2D>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("marker-only") &&
                result.Message.Contains("BoxCollider2D")));
        }

        [Test]
        public void ValidateAll_ReportsError_WhenOuterWallsCompositeColliderHasNoStaticRigidbody()
        {
            var roomRig = CreateValidRoomRig("Room_Geometry_MissingStaticBody");
            var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
            outerWall.gameObject.AddComponent<Tilemap>();
            outerWall.gameObject.AddComponent<TilemapRenderer>();
            outerWall.gameObject.AddComponent<TilemapCollider2D>();
            outerWall.gameObject.AddComponent<CompositeCollider2D>();

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == outerWall.gameObject &&
                result.Severity == LevelValidator.Severity.Error &&
                result.Message.Contains("Rigidbody2D") &&
                result.Message.Contains("Static")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenWallNamedTilemapLivesUnderElementsRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Geometry_WrongRoot");
            var misplacedWall = CreateChild(roomRig.ElementsRoot, "OuterWalls_Main");
            misplacedWall.gameObject.AddComponent<Tilemap>();
            misplacedWall.gameObject.AddComponent<TilemapRenderer>();
            misplacedWall.gameObject.AddComponent<TilemapCollider2D>();

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == misplacedWall.gameObject &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Navigation/Geometry") &&
                result.Message.Contains("OuterWalls_Main")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenDoorPlacedOutsideNavigationRoot()
        {
            var roomRig = CreateValidRoomRig("Room_Door_WrongRoot");
            var targetRig = CreateValidRoomRig("Room_Door_Target");
            var doorObject = CreateGameObjectWithTriggerColliderAndComponent<Door>("Door_WrongRoot");
            doorObject.transform.SetParent(roomRig.ElementsRoot, false);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetRoom", targetRig.Room);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetSpawnPoint", CreateChild(targetRig.NavigationRoot, "Spawn_Target"));
            SetPrivateField(doorObject.GetComponent<Door>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == doorObject.GetComponent<Door>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("Door") &&
                result.Message.Contains("Navigation")));
        }

        [Test]
        public void ValidateAll_ReportsWarning_WhenDoorCenterSitsInsideFilledOuterWallTile()
        {
            var roomRig = CreateValidRoomRig("Room_Door_FilledWall");
            var targetRig = CreateValidRoomRig("Room_Door_TargetWall");
            roomRig.OuterWallsRoot.gameObject.AddComponent<Grid>();
            var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
            var tilemap = outerWall.gameObject.AddComponent<Tilemap>();
            outerWall.gameObject.AddComponent<TilemapRenderer>();
            var tilemapCollider = outerWall.gameObject.AddComponent<TilemapCollider2D>();
            var rigidbody = outerWall.gameObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            outerWall.gameObject.AddComponent<CompositeCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var tile = ScriptableObject.CreateInstance<Tile>();
            _createdObjects.Add(tile);
            tilemap.SetTile(Vector3Int.zero, tile);

            var doorRoot = roomRig.NavigationRoot.Find(RoomAuthoringHierarchy.DoorsRootName);
            Assert.That(doorRoot, Is.Not.Null);

            var doorObject = CreateGameObjectWithTriggerColliderAndComponent<Door>("Door_FilledWall");
            doorObject.transform.SetParent(doorRoot, false);
            doorObject.transform.position = Vector3.zero;
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetRoom", targetRig.Room);
            SetPrivateField(doorObject.GetComponent<Door>(), "_targetSpawnPoint", CreateChild(targetRig.NavigationRoot, "Spawn_TargetWall"));
            SetPrivateField(doorObject.GetComponent<Door>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == doorObject.GetComponent<Door>() &&
                result.Severity == LevelValidator.Severity.Warning &&
                result.Message.Contains("geometry opening")));
        }

        [Test]
        public void ValidateAll_DoesNotReportDoorOpeningWarning_ForNarrativeFallTrigger()
        {
            var roomRig = CreateValidRoomRig("Room_FallTrigger_Boundary");
            var targetRig = CreateValidRoomRig("Room_FallTrigger_Target");
            var outerWall = CreateChild(roomRig.OuterWallsRoot, "OuterWalls_Main");
            var tilemap = outerWall.gameObject.AddComponent<Tilemap>();
            outerWall.gameObject.AddComponent<TilemapRenderer>();
            outerWall.gameObject.AddComponent<TilemapCollider2D>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            _createdObjects.Add(tile);
            tilemap.SetTile(Vector3Int.zero, tile);

            var triggerObject = CreateGameObjectWithTriggerColliderAndComponent<NarrativeFallTrigger>("NarrativeFall_Boundary");
            triggerObject.transform.SetParent(roomRig.TriggersRoot, false);
            triggerObject.transform.position = Vector3.zero;
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_targetRoom", targetRig.Room);
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_landingPoint", CreateChild(targetRig.NavigationRoot, "Landing_Target"));
            SetPrivateField(triggerObject.GetComponent<NarrativeFallTrigger>(), "_playerLayer", (LayerMask)1);

            var results = LevelValidator.ValidateAll();

            Assert.That(results.Any(result =>
                result.TargetObject == triggerObject.GetComponent<NarrativeFallTrigger>() &&
                result.Message.Contains("geometry opening")), Is.False);
        }

        private RoomTestRig CreateValidRoomRig(string roomName)

        {
            var roomObject = CreateGameObjectWithComponent<Room>(roomName);
            roomObject.AddComponent<BoxCollider2D>().isTrigger = true;

            var roomData = CreateScriptableObject<RoomSO>($"{roomName}_Data");
            SetPrivateField(roomData, "_roomID", roomName);
            SetPrivateField(roomData, "_displayName", roomName);
            SetPrivateField(roomObject.GetComponent<Room>(), "_data", roomData);

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);
            var confiner = CreateChild(roomObject.transform, "CameraConfiner");
            confiner.gameObject.layer = 2;

            return new RoomTestRig
            {
                Room = roomObject.GetComponent<Room>(),
                NavigationRoot = hierarchy.NavigationRoot,
                GeometryRoot = hierarchy.GeometryRoot,
                OuterWallsRoot = hierarchy.OuterWallsRoot,
                InnerWallsRoot = hierarchy.InnerWallsRoot,
                ElementsRoot = hierarchy.ElementsRoot,
                EncountersRoot = hierarchy.EncountersRoot,
                HazardsRoot = hierarchy.HazardsRoot,
                DecorationRoot = hierarchy.DecorationRoot,
                TriggersRoot = hierarchy.TriggersRoot,
                CameraConfiner = confiner
            };
        }

        private Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            _createdObjects.Add(child);
            return child.transform;
        }

        private GameObject CreateGameObjectWithComponent<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<T>();
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateGameObjectWithTriggerColliderAndComponent<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            gameObject.AddComponent<T>();
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private T CreateScriptableObject<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            _createdObjects.Add(asset);
            return asset;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Could not find private field '{fieldName}' on {target.GetType().Name}.");
        }

        private sealed class RoomTestRig
        {
            public Room Room;
            public Transform NavigationRoot;
            public Transform GeometryRoot;
            public Transform OuterWallsRoot;
            public Transform InnerWallsRoot;
            public Transform ElementsRoot;
            public Transform EncountersRoot;
            public Transform HazardsRoot;
            public Transform DecorationRoot;
            public Transform TriggersRoot;
            public Transform CameraConfiner;
        }
    }
}
