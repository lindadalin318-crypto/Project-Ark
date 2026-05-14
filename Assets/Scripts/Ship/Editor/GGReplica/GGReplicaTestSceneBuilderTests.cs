#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaTestSceneBuilderTests
    {
        private const string TestScenePath = "Assets/Scenes/GGReplicaShipTest_Test.unity";
        private const string MissingAdapterScenePath = "Assets/Scenes/GGReplicaShipTest_MissingAdapter_Test.unity";
        private const string TempLivePrefabPath = "Assets/_Prefabs/Ship/GGReplicaSceneBuilder_Live_Test.prefab";
        private const string TempReplicaPrefabPath = "Assets/_Prefabs/Ship/GGReplicaSceneBuilder_ReplicaMissingAdapter_Test.prefab";

        [SetUp]
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestScenePath);
            AssetDatabase.DeleteAsset(MissingAdapterScenePath);
            AssetDatabase.DeleteAsset(TempLivePrefabPath);
            AssetDatabase.DeleteAsset(TempReplicaPrefabPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BuildTestScene_CreatesABSceneWithSwitcherAndCamera()
        {
            GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();
            GGReplicaPrefabBuilder.BuildExperimentalPrefab();

            Assert.That(GGReplicaTestSceneBuilder.BuildTestSceneForTest(TestScenePath), Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath), Is.Not.Null);

            var live = GameObject.Find("LiveShip_A");
            var replica = GameObject.Find("GGReplicaShip_B");
            var switcherGo = GameObject.Find("GGReplicaTestSwitcher");
            var camera = Camera.main;

            Assert.That(live, Is.Not.Null);
            Assert.That(replica, Is.Not.Null);
            var replicaView = replica.GetComponent<GGReplicaPlayerViewAdapter>();
            Assert.That(replicaView, Is.Not.Null);
            Assert.That(replica.GetComponent<GGReplicaShipViewAdapter>(), Is.Null);
            Assert.That(ContainsComponentNamed(live, "StarChartController"), Is.False);
            Assert.That(ContainsComponentNamed(replica, "StarChartController"), Is.False);
            Assert.That(ContainsComponentNamed(live, "BoostTrailView"), Is.False);
            Assert.That(ContainsComponentNamed(replica, "BoostTrailView"), Is.False);
            Assert.That(switcherGo, Is.Not.Null);
            var switcher = switcherGo.GetComponent<GGReplicaTestSwitcher>();
            Assert.That(switcher, Is.Not.Null);
            var switcherSO = new SerializedObject(switcher);
            Assert.That(switcherSO.FindProperty("_replicaView").objectReferenceValue, Is.SameAs(replicaView));
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
        }

        [Test]
        public void BuildTestScene_AbortsWhenReplicaPrefabMissingPlayerViewAdapter()
        {
            CreateTempPrefab(TempLivePrefabPath, "TempLiveShip");
            CreateTempPrefab(TempReplicaPrefabPath, "TempReplicaMissingAdapter");

            LogAssert.Expect(LogType.Error, $"[GGReplicaTestSceneBuilder] Missing GGReplicaPlayerViewAdapter on replica prefab: {TempReplicaPrefabPath}");
            Assert.That(GGReplicaTestSceneBuilder.BuildTestScene(TempLivePrefabPath, TempReplicaPrefabPath, MissingAdapterScenePath), Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(MissingAdapterScenePath), Is.Null);
        }

        private static bool ContainsComponentNamed(GameObject root, string typeName)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateTempPrefab(string path, string name)
        {
            var go = new GameObject(name);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
