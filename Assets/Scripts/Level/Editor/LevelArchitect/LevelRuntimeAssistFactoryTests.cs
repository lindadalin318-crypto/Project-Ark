using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class LevelRuntimeAssistFactoryTests
    {
        private readonly List<Object> _createdObjects = new();

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
        public void CreateRoomAssist_CreatesContactHazardStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_Contact");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.ContactHazard);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<ContactHazard>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<ContactHazard>());
        }

        [Test]
        public void CreateRoomAssist_CreatesDamageZoneStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_DamageZone");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.DamageZone);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<DamageZone>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<DamageZone>());
        }

        [Test]
        public void CreateRoomAssist_CreatesTimedHazardStarterUnderHazardsRoot()
        {
            var room = CreateRoom("Room_Hazard_Timed");

            var created = LevelRuntimeAssistFactory.CreateRoomAssist(
                room,
                LevelRuntimeAssistFactory.RoomAssistType.TimedHazard);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.Not.Null);
            Assert.That(created.transform.parent.name, Is.EqualTo("Hazards"));
            Assert.That(created.GetComponent<TimedHazard>(), Is.Not.Null);
            AssertHazardStarter(created.GetComponent<TimedHazard>());
        }

        private Room CreateRoom(string roomId)
        {
            var roomObject = new GameObject(roomId);
            _createdObjects.Add(roomObject);

            var room = roomObject.AddComponent<Room>();
            var box = roomObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(20f, 12f);

            var roomData = ScriptableObject.CreateInstance<RoomSO>();
            roomData.name = $"{roomId}_Data";
            _createdObjects.Add(roomData);

            SetPrivateField(roomData, "_roomID", roomId);
            SetPrivateField(roomData, "_displayName", roomId);
            SetPrivateField(room, "_data", roomData);

            return room;
        }

        private static void AssertHazardStarter(EnvironmentHazard hazard)
        {
            Assert.That(hazard, Is.Not.Null);

            var collider = hazard.GetComponent<Collider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.True);

            var serialized = new SerializedObject(hazard);
            int bits = serialized.FindProperty("_targetLayer").FindPropertyRelative("m_Bits").intValue;
            Assert.That(bits, Is.Not.EqualTo(0));
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
    }
}
