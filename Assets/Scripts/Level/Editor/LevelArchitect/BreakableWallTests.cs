using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class BreakableWallTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void DestroyableObject_RaisesDestroyedEventOnlyOnce_WhenFatalDamageApplied()
        {
            var roomRig = CreateRoomRig("Room_Destroyable_Event");
            var registry = CreateRegistry();
            var wallRoot = CreateBreakableWallObject(roomRig.ElementsRoot, "BreakableWall_Event", out var breakableWall, out var destroyable, out _, out _, out _);

            InvokeLifecycle(destroyable, "Awake");
            InvokeLifecycle(breakableWall, "Awake");
            InvokeLifecycle(breakableWall, "OnEnable");
            InvokeLifecycle(destroyable, "Start");
            InvokeLifecycle(breakableWall, "Start");

            int destroyedEventCount = 0;
            destroyable.OnDestroyed += () => destroyedEventCount++;

            destroyable.TakeDamage(new DamagePayload(1f, Vector2.zero, 0f));
            destroyable.TakeDamage(new DamagePayload(1f, Vector2.zero, 0f));

            Assert.That(destroyable.IsDestroyed, Is.True);
            Assert.That(destroyedEventCount, Is.EqualTo(1));
            Assert.That(registry.GetFlag(roomRig.Room.RoomID, wallRoot.name), Is.True);
        }

        [Test]
        public void BreakableWall_SwapsPresentation_WhenDestroyableDies()
        {
            var roomRig = CreateRoomRig("Room_BreakableWall_Runtime");
            CreateRegistry();
            CreateBreakableWallObject(roomRig.ElementsRoot, "BreakableWall_Runtime", out var breakableWall, out var destroyable, out var signalRenderer, out var intactObject, out var destroyedObject);

            InvokeLifecycle(destroyable, "Awake");
            InvokeLifecycle(breakableWall, "Awake");
            InvokeLifecycle(breakableWall, "OnEnable");
            InvokeLifecycle(destroyable, "Start");
            InvokeLifecycle(breakableWall, "Start");

            Assert.That(signalRenderer.enabled, Is.True);
            Assert.That(intactObject.activeSelf, Is.True);
            Assert.That(destroyedObject.activeSelf, Is.False);

            destroyable.TakeDamage(new DamagePayload(1f, Vector2.zero, 0f));

            Assert.That(destroyable.GetComponent<Collider2D>().enabled, Is.False);
            Assert.That(signalRenderer.enabled, Is.False);
            Assert.That(intactObject.activeSelf, Is.False);
            Assert.That(destroyedObject.activeSelf, Is.True);
        }

        [Test]
        public void BreakableWall_AppliesDestroyedPresentationOnStart_WhenFlagAlreadyPersisted()
        {
            var roomRig = CreateRoomRig("Room_BreakableWall_Persisted");
            var registry = CreateRegistry();
            CreateBreakableWallObject(roomRig.ElementsRoot, "BreakableWall_Persisted", out var breakableWall, out var destroyable, out var signalRenderer, out var intactObject, out var destroyedObject);

            registry.SetFlag(roomRig.Room.RoomID, "BreakableWall_Persisted", true);

            InvokeLifecycle(destroyable, "Awake");
            InvokeLifecycle(breakableWall, "Awake");
            InvokeLifecycle(breakableWall, "OnEnable");
            InvokeLifecycle(destroyable, "Start");
            InvokeLifecycle(breakableWall, "Start");

            Assert.That(destroyable.IsDestroyed, Is.True);
            Assert.That(signalRenderer.enabled, Is.False);
            Assert.That(intactObject.activeSelf, Is.False);
            Assert.That(destroyedObject.activeSelf, Is.True);
        }

        private RoomTestRig CreateRoomRig(string roomId)
        {
            var roomObject = new GameObject(roomId);
            _createdObjects.Add(roomObject);

            var room = roomObject.AddComponent<Room>();
            var boxCollider = roomObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(20f, 12f);

            var roomData = ScriptableObject.CreateInstance<RoomSO>();
            roomData.name = $"{roomId}_Data";
            _createdObjects.Add(roomData);

            SetPrivateField(roomData, "_roomID", roomId);
            SetPrivateField(roomData, "_displayName", roomId);
            SetPrivateField(room, "_data", roomData);

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);
            return new RoomTestRig(room, hierarchy.ElementsRoot);
        }

        private RoomFlagRegistry CreateRegistry()
        {
            var registryObject = new GameObject("RoomFlagRegistry");
            _createdObjects.Add(registryObject);

            var registry = registryObject.AddComponent<RoomFlagRegistry>();
            InvokeLifecycle(registry, "Awake");
            return registry;
        }

        private GameObject CreateBreakableWallObject(
            Transform parent,
            string name,
            out BreakableWall breakableWall,
            out DestroyableObject destroyable,
            out SpriteRenderer signalRenderer,
            out GameObject intactObject,
            out GameObject destroyedObject)
        {
            var wallRoot = new GameObject(name);
            wallRoot.transform.SetParent(parent, false);
            wallRoot.AddComponent<BoxCollider2D>();
            wallRoot.AddComponent<SpriteRenderer>();
            destroyable = wallRoot.AddComponent<DestroyableObject>();
            breakableWall = wallRoot.AddComponent<BreakableWall>();
            _createdObjects.Add(wallRoot);

            var signalObject = new GameObject("Signal");
            signalObject.transform.SetParent(wallRoot.transform, false);
            signalRenderer = signalObject.AddComponent<SpriteRenderer>();

            intactObject = new GameObject("IntactVisual");
            intactObject.transform.SetParent(wallRoot.transform, false);

            destroyedObject = new GameObject("DestroyedVisual");
            destroyedObject.transform.SetParent(wallRoot.transform, false);
            destroyedObject.SetActive(false);

            SetPrivateField(breakableWall, "_suspiciousSignalRenderers", new[] { signalRenderer });
            SetPrivateField(breakableWall, "_intactOnlyObjects", new[] { intactObject });
            SetPrivateField(breakableWall, "_destroyedOnlyObjects", new[] { destroyedObject });

            return wallRoot;
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                Assert.Fail($"Could not find lifecycle method '{methodName}' on {target.GetType().Name}.");
            }

            method.Invoke(target, null);
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

        private readonly struct RoomTestRig
        {
            public RoomTestRig(Room room, Transform elementsRoot)
            {
                Room = room;
                ElementsRoot = elementsRoot;
            }

            public Room Room { get; }
            public Transform ElementsRoot { get; }
        }
    }
}
