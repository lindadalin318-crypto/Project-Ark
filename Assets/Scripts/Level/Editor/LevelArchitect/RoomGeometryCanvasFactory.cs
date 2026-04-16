using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Creates blank Tilemap canvases for scene-backed static wall authoring.
    /// This is not a runtime assist factory: it only prepares the geometry host
    /// and the standard collision chain under Navigation/Geometry.
    /// </summary>
    public static class RoomGeometryCanvasFactory
    {
        public enum WallCanvasKind
        {
            OuterWalls,
            InnerWalls
        }

        public static GameObject CreateCanvas(Room room, WallCanvasKind kind)
        {
            if (room == null)
            {
                Debug.LogWarning("[RoomGeometryCanvasFactory] Cannot create canvas: room is null.");
                return null;
            }

            var hierarchy = RoomAuthoringHierarchy.EnsureForRoom(room.transform);
            Transform parent = kind == WallCanvasKind.OuterWalls ? hierarchy.OuterWallsRoot : hierarchy.InnerWallsRoot;
            EnsureGridRoot(parent.gameObject);

            string baseName = kind == WallCanvasKind.OuterWalls ? "OuterWalls_Main" : "InnerWalls_Main";
            string objectName = GameObjectUtility.GetUniqueNameForSibling(parent, baseName);

            var canvas = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(canvas, $"Create {objectName}");
            canvas.transform.SetParent(parent, false);
            canvas.transform.position = room.transform.position;

            Undo.AddComponent<Tilemap>(canvas);
            var renderer = Undo.AddComponent<TilemapRenderer>(canvas);
            var tilemapCollider = Undo.AddComponent<TilemapCollider2D>(canvas);
            var rigidbody = Undo.AddComponent<Rigidbody2D>(canvas);
            Undo.AddComponent<CompositeCollider2D>(canvas);

            Undo.RecordObject(renderer, "Configure TilemapRenderer");
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;

            Undo.RecordObject(rigidbody, "Configure Static Rigidbody2D");
            rigidbody.bodyType = RigidbodyType2D.Static;
            rigidbody.simulated = true;

            Undo.RecordObject(tilemapCollider, "Configure TilemapCollider2D");
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            Selection.activeGameObject = canvas;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
            EditorUtility.SetDirty(canvas);

            Debug.Log($"[RoomGeometryCanvasFactory] Created {kind} canvas '{objectName}' in room '{room.RoomID}'. Paint tiles manually to author wall geometry.");
            return canvas;
        }

        public static string GetDisplayName(WallCanvasKind kind)
        {
            return kind switch
            {
                WallCanvasKind.OuterWalls => "Create Outer Wall Canvas",
                WallCanvasKind.InnerWalls => "Create Inner Wall Canvas",
                _ => kind.ToString()
            };
        }

        private static void EnsureGridRoot(GameObject target)
        {
            if (target == null || target.GetComponent<Grid>() != null)
            {
                return;
            }

            Undo.AddComponent<Grid>(target);
        }
    }
}
