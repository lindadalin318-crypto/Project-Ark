using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    [TestFixture]
    public class RoomAuthoringHierarchyTests
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
        public void EnsureForRoom_CreatesGeometryRootsAndMarker()
        {
            var roomObject = new GameObject("Room_Geometry_Test");
            _createdObjects.Add(roomObject);

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);

            Assert.That(hierarchy.NavigationRoot, Is.Not.Null);
            Assert.That(hierarchy.GeometryRoot, Is.Not.Null);
            Assert.That(hierarchy.OuterWallsRoot, Is.Not.Null);
            Assert.That(hierarchy.InnerWallsRoot, Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry"), Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry/OuterWalls"), Is.Not.Null);
            Assert.That(roomObject.transform.Find("Navigation/Geometry/InnerWalls"), Is.Not.Null);
            Assert.That(hierarchy.GeometryRoot.GetComponent<RoomGeometryRoot>(), Is.Not.Null);
        }

        [Test]
        public void EnsureForRoom_IsIdempotentAndDoesNotDuplicateGeometryRoots()
        {
            var roomObject = new GameObject("Room_Geometry_Idempotent");
            _createdObjects.Add(roomObject);

            var first = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);
            var second = RoomAuthoringHierarchy.EnsureForRoom(roomObject.transform);

            Assert.That(first.NavigationRoot, Is.SameAs(second.NavigationRoot));
            Assert.That(first.GeometryRoot, Is.SameAs(second.GeometryRoot));
            Assert.That(first.OuterWallsRoot, Is.SameAs(second.OuterWallsRoot));
            Assert.That(first.InnerWallsRoot, Is.SameAs(second.InnerWallsRoot));
            Assert.That(roomObject.transform.Find("Navigation").GetComponentsInChildren<RoomGeometryRoot>(true).Length, Is.EqualTo(1));
        }
    }
}
