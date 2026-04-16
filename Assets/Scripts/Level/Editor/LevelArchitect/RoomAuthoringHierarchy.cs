using UnityEditor;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Shared editor authority for creating and repairing the standard Room authoring hierarchy.
    /// Keeps `Level Architect` room creation and repair flows on the same structure contract.
    /// </summary>
    public static class RoomAuthoringHierarchy
    {
        public const string NavigationRootName = "Navigation";
        public const string ElementsRootName = "Elements";
        public const string EncountersRootName = "Encounters";
        public const string HazardsRootName = "Hazards";
        public const string DecorationRootName = "Decoration";
        public const string TriggersRootName = "Triggers";
        public const string DoorsRootName = "Doors";
        public const string SpawnPointsRootName = "SpawnPoints";
        public const string GeometryRootName = "Geometry";
        public const string OuterWallsRootName = "OuterWalls";
        public const string InnerWallsRootName = "InnerWalls";

        public readonly struct RoomHierarchyRefs
        {
            public RoomHierarchyRefs(
                Transform navigationRoot,
                Transform elementsRoot,
                Transform encountersRoot,
                Transform hazardsRoot,
                Transform decorationRoot,
                Transform triggersRoot,
                Transform doorsRoot,
                Transform navigationSpawnPointsRoot,
                Transform geometryRoot,
                Transform outerWallsRoot,
                Transform innerWallsRoot)
            {
                NavigationRoot = navigationRoot;
                ElementsRoot = elementsRoot;
                EncountersRoot = encountersRoot;
                HazardsRoot = hazardsRoot;
                DecorationRoot = decorationRoot;
                TriggersRoot = triggersRoot;
                DoorsRoot = doorsRoot;
                NavigationSpawnPointsRoot = navigationSpawnPointsRoot;
                GeometryRoot = geometryRoot;
                OuterWallsRoot = outerWallsRoot;
                InnerWallsRoot = innerWallsRoot;
            }

            public Transform NavigationRoot { get; }
            public Transform ElementsRoot { get; }
            public Transform EncountersRoot { get; }
            public Transform HazardsRoot { get; }
            public Transform DecorationRoot { get; }
            public Transform TriggersRoot { get; }
            public Transform DoorsRoot { get; }
            public Transform NavigationSpawnPointsRoot { get; }
            public Transform GeometryRoot { get; }
            public Transform OuterWallsRoot { get; }
            public Transform InnerWallsRoot { get; }
        }

        public static RoomHierarchyRefs EnsureForRoom(Transform roomRoot)
        {
            var navigationRoot = EnsureChild(roomRoot, NavigationRootName);
            var elementsRoot = EnsureChild(roomRoot, ElementsRootName);
            var encountersRoot = EnsureChild(roomRoot, EncountersRootName);
            var hazardsRoot = EnsureChild(roomRoot, HazardsRootName);
            var decorationRoot = EnsureChild(roomRoot, DecorationRootName);
            var triggersRoot = EnsureChild(roomRoot, TriggersRootName);

            var doorsRoot = EnsureChild(navigationRoot, DoorsRootName);
            var navigationSpawnPointsRoot = EnsureChild(navigationRoot, SpawnPointsRootName);
            var geometryRoot = EnsureChild(navigationRoot, GeometryRootName);
            EnsureComponent<RoomGeometryRoot>(geometryRoot.gameObject);

            var outerWallsRoot = EnsureChild(geometryRoot, OuterWallsRootName);
            var innerWallsRoot = EnsureChild(geometryRoot, InnerWallsRootName);

            return new RoomHierarchyRefs(
                navigationRoot,
                elementsRoot,
                encountersRoot,
                hazardsRoot,
                decorationRoot,
                triggersRoot,
                doorsRoot,
                navigationSpawnPointsRoot,
                geometryRoot,
                outerWallsRoot,
                innerWallsRoot);
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(target);
        }
    }
}
