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
            BuildTestScene(LiveShipPrefabPath, ReplicaShipPrefabPath, ScenePath);
        }

        internal static bool BuildTestSceneForTest(string scenePath)
        {
            return BuildTestScene(LiveShipPrefabPath, ReplicaShipPrefabPath, scenePath);
        }

        internal static bool BuildTestScene(string liveShipPrefabPath, string replicaShipPrefabPath, string scenePath)
        {
            var livePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(liveShipPrefabPath);
            var replicaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(replicaShipPrefabPath);
            if (livePrefab == null || replicaPrefab == null)
            {
                Debug.LogError("[GGReplicaTestSceneBuilder] Missing live or replica ship prefab.");
                return false;
            }

            if (replicaPrefab.GetComponent<GGReplicaPlayerViewAdapter>() == null)
            {
                Debug.LogError($"[GGReplicaTestSceneBuilder] Missing GGReplicaPlayerViewAdapter on replica prefab: {replicaShipPrefabPath}");
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var live = (GameObject)PrefabUtility.InstantiatePrefab(livePrefab, scene);
            live.name = "LiveShip_A";
            live.transform.position = new Vector3(-3f, 0f, 0f);
            StripNonVisualValidationComponents(live);

            var replica = (GameObject)PrefabUtility.InstantiatePrefab(replicaPrefab, scene);
            replica.name = "GGReplicaShip_B";
            replica.transform.position = new Vector3(3f, 0f, 0f);
            StripNonVisualValidationComponents(replica);

            var replicaView = replica.GetComponent<GGReplicaPlayerViewAdapter>();
            if (replicaView == null)
            {
                Debug.LogError("[GGReplicaTestSceneBuilder] Instantiated replica is missing GGReplicaPlayerViewAdapter. Scene was not saved.");
                return false;
            }

            var switcher = new GameObject("GGReplicaTestSwitcher");
            SceneManager.MoveGameObjectToScene(switcher, scene);
            var switcherComponent = switcher.AddComponent<GGReplicaTestSwitcher>();
            var switcherSO = new SerializedObject(switcherComponent);
            SetObject(switcherSO, "_liveShip", live);
            SetObject(switcherSO, "_replicaShip", replica);
            SetObject(switcherSO, "_replicaView", replicaView);
            switcherSO.ApplyModifiedPropertiesWithoutUndo();
            switcherSO.Dispose();
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
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            if (!saved)
            {
                Debug.LogError($"[GGReplicaTestSceneBuilder] Failed to save {scenePath}");
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaTestSceneBuilder] Built {scenePath}");
            return true;
        }

        private static void StripNonVisualValidationComponents(GameObject root)
        {
            RemoveComponentsByTypeName(root, "StarChartController");
            RemoveComponentsByTypeName(root, "BoostTrailView");
        }

        private static void RemoveComponentsByTypeName(GameObject root, string typeName)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[GGReplicaTestSceneBuilder] Missing serialized property {propertyName} on GGReplicaTestSwitcher.");
                return;
            }

            if (value == null)
            {
                Debug.LogError($"[GGReplicaTestSceneBuilder] Cannot wire {propertyName}; value is null.");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}
#endif
