using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class RoomGeometryCanvasFactoryTests
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
        public void CreateCanvas_CreatesOuterWallCanvasUnderGeometryRootWithStandardColliderChain()
        {
            var room = CreateRoom("Room_Geometry_Canvas_Outer");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var created = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls);

            Assert.That(created, Is.Not.Null);
            Assert.That(created.transform.parent, Is.SameAs(hierarchy.OuterWallsRoot));
            Assert.That(created.name, Is.EqualTo("OuterWalls_Main"));
            Assert.That(hierarchy.OuterWallsRoot.GetComponent<Grid>(), Is.Not.Null);
            Assert.That(created.GetComponent<Tilemap>(), Is.Not.Null);
            Assert.That(created.GetComponent<TilemapRenderer>(), Is.Not.Null);
            Assert.That(created.GetComponent<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<CompositeCollider2D>(), Is.Not.Null);
            Assert.That(created.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(created.GetComponent<TilemapCollider2D>().compositeOperation, Is.Not.EqualTo(Collider2D.CompositeOperation.None));
        }

        [Test]
        public void CreateCanvas_CreatesUniqueInnerWallCanvasNamesOnRepeatedCalls()
        {
            var room = CreateRoom("Room_Geometry_Canvas_Inner");
            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);

            var first = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls);
            var second = RoomGeometryCanvasFactory.CreateCanvas(
                room.GetComponent<Room>(),
                RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first.transform.parent, Is.SameAs(hierarchy.InnerWallsRoot));
            Assert.That(second.transform.parent, Is.SameAs(hierarchy.InnerWallsRoot));
            Assert.That(first.name, Is.EqualTo("InnerWalls_Main"));
            Assert.That(second.name, Is.Not.EqualTo(first.name));
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
