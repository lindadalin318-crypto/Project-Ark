#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaTestSceneBuilderTests
    {
        private const string ScenePath = "Assets/Scenes/GGReplicaShipTest.unity";

        [Test]
        public void BuildTestScene_CreatesABSceneWithSwitcherAndCamera()
        {
            GGReplicaTestSceneBuilder.BuildTestScene();

            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);

            var live = GameObject.Find("LiveShip_A");
            var replica = GameObject.Find("GGReplicaShip_B");
            var switcherGo = GameObject.Find("GGReplicaTestSwitcher");
            var camera = Camera.main;

            Assert.That(live, Is.Not.Null);
            Assert.That(replica, Is.Not.Null);
            Assert.That(replica.GetComponent<GGReplicaShipViewAdapter>(), Is.Not.Null);
            Assert.That(switcherGo, Is.Not.Null);
            Assert.That(switcherGo.GetComponent<GGReplicaTestSwitcher>(), Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
        }
    }
}
#endif
