using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
            var lockObject = CreateGameObjectWithComponent<Lock>("Lock_MissingKey");
            lockObject.AddComponent<BoxCollider2D>().isTrigger = true;
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
            var checkpointObject = CreateGameObjectWithComponent<Checkpoint>("Checkpoint_MissingData");
            checkpointObject.AddComponent<BoxCollider2D>().isTrigger = true;
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
            var lockObject = CreateGameObjectWithComponent<Lock>("Lock_WrongRoot");
            lockObject.transform.SetParent(roomRig.TriggersRoot, false);
            lockObject.AddComponent<BoxCollider2D>().isTrigger = true;
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

        private RoomTestRig CreateValidRoomRig(string roomName)
        {
            var roomObject = CreateGameObjectWithComponent<Room>(roomName);
            roomObject.AddComponent<BoxCollider2D>().isTrigger = true;

            var roomData = CreateScriptableObject<RoomSO>($"{roomName}_Data");
            SetPrivateField(roomData, "_roomID", roomName);
            SetPrivateField(roomData, "_displayName", roomName);
            SetPrivateField(roomObject.GetComponent<Room>(), "_data", roomData);

            var navigationRoot = CreateChild(roomObject.transform, "Navigation");
            var elementsRoot = CreateChild(roomObject.transform, "Elements");
            var encountersRoot = CreateChild(roomObject.transform, "Encounters");
            var hazardsRoot = CreateChild(roomObject.transform, "Hazards");
            var decorationRoot = CreateChild(roomObject.transform, "Decoration");
            var triggersRoot = CreateChild(roomObject.transform, "Triggers");
            var confiner = CreateChild(roomObject.transform, "CameraConfiner");
            confiner.gameObject.layer = 2;

            return new RoomTestRig
            {
                Room = roomObject.GetComponent<Room>(),
                NavigationRoot = navigationRoot,
                ElementsRoot = elementsRoot,
                EncountersRoot = encountersRoot,
                HazardsRoot = hazardsRoot,
                DecorationRoot = decorationRoot,
                TriggersRoot = triggersRoot,
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
            public Transform ElementsRoot;
            public Transform EncountersRoot;
            public Transform HazardsRoot;
            public Transform DecorationRoot;
            public Transform TriggersRoot;
            public Transform CameraConfiner;
        }
    }
}
