#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Builds an isolated A/B validation scene for comparing live Ship.prefab with Ship_GGReplica.prefab.
    /// Does not modify SampleScene or the live ship prefab.
    /// </summary>
    public static class GGReplicaTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GGReplicaShipTest.unity";
        private const string LiveShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string ReplicaShipPrefabPath = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Test Scene")]
        public static void BuildTestScene()
        {
            var livePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShipPrefabPath);
            var replicaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShipPrefabPath);
            if (livePrefab == null || replicaPrefab == null)
            {
                Debug.LogError("[GGReplicaTestSceneBuilder] Missing live or replica ship prefab.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var live = (GameObject)PrefabUtility.InstantiatePrefab(livePrefab, scene);
            live.name = "LiveShip_A";
            live.transform.position = new Vector3(-3f, 0f, 0f);

            var replica = (GameObject)PrefabUtility.InstantiatePrefab(replicaPrefab, scene);
            replica.name = "GGReplicaShip_B";
            replica.transform.position = new Vector3(3f, 0f, 0f);

            var switcher = new GameObject("GGReplicaTestSwitcher");
            SceneManager.MoveGameObjectToScene(switcher, scene);
            var switcherComponent = switcher.AddComponent<GGReplicaTestSwitcher>();
            var switcherSO = new SerializedObject(switcherComponent);
            switcherSO.FindProperty("_liveShip").objectReferenceValue = live;
            switcherSO.FindProperty("_replicaShip").objectReferenceValue = replica;
            switcherSO.FindProperty("_replicaView").objectReferenceValue = replica.GetComponent<GGReplicaShipViewAdapter>();
            switcherSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(switcherComponent);

            var cameraGo = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaTestSceneBuilder] Built {ScenePath}");
        }
    }
}
#endif
