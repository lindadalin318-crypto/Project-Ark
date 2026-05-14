#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaGlitchV2TestSceneBuilderTests
    {
        private const string TestScenePath = "Assets/Scenes/GGReplicaGlitchV2Test_Test.unity";

        [SetUp]
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestScenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void BuildTestScene_CreatesPlayableInputDrivenV2SceneWithoutButtonSwitcher()
        {
            GGReplicaGlitchV2PrefabBuilder.BuildPrefab();

            Assert.That(GGReplicaGlitchV2TestSceneBuilder.BuildTestSceneForTest(TestScenePath), Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath), Is.Not.Null);

            var player = GameObject.Find("GGReplicaGlitchV2_Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player!.GetComponent<GGReplicaGlitchInputDriver>(), Is.Not.Null);
            Assert.That(player.GetComponent<GGReplicaGlitchMotor>(), Is.Not.Null);
            Assert.That(player.GetComponent<GGReplicaGlitchView>(), Is.Not.Null);
            Assert.That(GameObject.Find("GGReplicaTestSwitcher"), Is.Null, "V2 validation must be input-driven, not 0-9 button-driven.");

            var help = GameObject.Find("GGReplicaGlitchV2_InputLegend");
            Assert.That(help, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Camera.main!.orthographic, Is.True);
        }
    }
}
#endif
