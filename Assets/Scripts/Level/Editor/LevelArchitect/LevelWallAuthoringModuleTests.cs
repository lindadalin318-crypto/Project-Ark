using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class LevelWallAuthoringModuleTests
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
        public void GetTools_ReturnsAllWallAuthoringActionsInSingleModule()
        {
            var tools = LevelWallAuthoringModule.GetTools().ToArray();

            Assert.That(tools.Select(tool => tool.Kind), Is.EqualTo(new[]
            {
                LevelWallAuthoringModule.WallToolKind.OuterWallCanvas,
                LevelWallAuthoringModule.WallToolKind.InnerWallCanvas,
                LevelWallAuthoringModule.WallToolKind.BreakableWallStarter
            }));
        }

        [Test]
        public void Create_CreatesBreakableWallStarterUnderElementsRoot()
        {
            var room = CreateRoom("Room_WallModule_Breakable");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var created = LevelWallAuthoringModule.Create(
                room.GetComponent<Room>(),
                LevelWallAuthoringModule.WallToolKind.BreakableWallStarter);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.SameAs(hierarchy.ElementsRoot));
            Assert.That(created.GetComponent<BreakableWall>(), Is.Not.Null);
            Assert.That(created.GetComponent<DestroyableObject>(), Is.Not.Null);
        }

        [Test]
        public void Create_CreatesOuterWallCanvasUnderGeometryOuterWallsRoot()
        {
            var room = CreateRoom("Room_WallModule_Outer");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var created = LevelWallAuthoringModule.Create(
                room.GetComponent<Room>(),
                LevelWallAuthoringModule.WallToolKind.OuterWallCanvas);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.SameAs(hierarchy.OuterWallsRoot));
            Assert.That(created.GetComponent<UnityEngine.Tilemaps.Tilemap>(), Is.Not.Null);
        }

        private GameObject CreateRoom(string roomId)
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

            return roomObject;
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
