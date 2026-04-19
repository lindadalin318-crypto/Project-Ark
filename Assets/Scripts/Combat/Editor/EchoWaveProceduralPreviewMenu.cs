using UnityEditor;
using UnityEngine;
using ProjectArk.Combat.Samples;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Editor helpers for creating and cleaning up the standalone procedural EchoWave preview rig.
    /// Does not create prefabs or other authored assets.
    /// </summary>
    public static class EchoWaveProceduralPreviewMenu
    {
        private const string CreateMenuPath = "ProjectArk/Combat/Samples/Create Procedural EchoWave Preview Rig";
        private const string DeleteMenuPath = "ProjectArk/Combat/Samples/Delete Procedural EchoWave Preview Rigs";
        private const string RigName = "__EchoWaveProceduralPreviewRig";

        [MenuItem(CreateMenuPath)]
        public static void CreatePreviewRig()
        {
            var root = new GameObject(RigName);
            Undo.RegisterCreatedObjectUndo(root, "Create Procedural EchoWave Preview Rig");
            root.transform.position = GetPreferredRigPosition();
            root.AddComponent<EchoWaveProceduralPreviewRig>();

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log(
                "[EchoWavePreview] Created standalone preview rig in the current scene. " +
                "Enter Play Mode to inspect the procedural placeholder wave. " +
                "The rig now prefers a visible camera position and the runtime sample includes a bright center marker for render diagnostics. " +
                "Delete the rig or remove the sample scripts folder to cleanly roll back.");
        }

        [MenuItem(DeleteMenuPath)]
        public static void DeletePreviewRigs()
        {
            var rigs = Object.FindObjectsByType<EchoWaveProceduralPreviewRig>(FindObjectsSortMode.None);
            int deletedCount = 0;

            for (int i = 0; i < rigs.Length; i++)
            {
                if (rigs[i] == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(rigs[i].gameObject);
                deletedCount++;
            }

            Debug.Log($"[EchoWavePreview] Removed {deletedCount} procedural preview rig(s) from the current scene.");
        }

        private static Vector3 GetPreferredRigPosition()
        {
            Camera camera = FindPreferredCamera();
            if (camera == null)
            {
                return Vector3.zero;
            }

            const float spawnPlaneZ = 0f;
            float depthFromCamera = Mathf.Abs(spawnPlaneZ - camera.transform.position.z);
            Vector3 worldCenter = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depthFromCamera));
            worldCenter.z = spawnPlaneZ;
            return worldCenter;
        }

        private static Camera FindPreferredCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled)
                {
                    return cameras[i];
                }
            }

            return null;
        }
    }
}
