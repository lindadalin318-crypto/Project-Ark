#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectArk.Ship.Editor
{
    public static class GGReplicaGlitchV2TestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GGReplicaGlitchV2Test.unity";

        [MenuItem("ProjectArk/Ship/GG Replica/V2/Build Glitch V2 Test Scene")]
        public static void BuildTestScene()
        {
            BuildTestSceneForTest(ScenePath);
        }

        internal static bool BuildTestSceneForTest(string scenePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GGReplicaGlitchV2PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[GGReplicaGlitchV2TestSceneBuilder] Missing V2 prefab: {GGReplicaGlitchV2PrefabBuilder.PrefabPath}");
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.name = "GGReplicaGlitchV2_Player";
            player.transform.position = Vector3.zero;

            var cameraGo = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.tag = "MainCamera";

            var legend = new GameObject("GGReplicaGlitchV2_InputLegend");
            SceneManager.MoveGameObjectToScene(legend, scene);
            legend.transform.position = new Vector3(-4.6f, 3.9f, 0f);
            var text = legend.AddComponent<TextMesh>();
            text.text = "GGReplica Glitch V2\\nWASD/Arrows Move\\nShift Boost Hold\\nSpace Dodge Burst\\nE Grab Hold\\nQ Heal\\nMouse Left Fire/Aim";
            text.characterSize = 0.18f;
            text.anchor = TextAnchor.UpperLeft;
            text.color = new Color(0.85f, 0.65f, 1f, 1f);

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaGlitchV2TestSceneBuilder] Built {scenePath}");
            return true;
        }
    }
}
#endif
