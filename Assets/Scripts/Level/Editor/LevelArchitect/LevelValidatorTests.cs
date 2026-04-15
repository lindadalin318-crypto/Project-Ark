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
