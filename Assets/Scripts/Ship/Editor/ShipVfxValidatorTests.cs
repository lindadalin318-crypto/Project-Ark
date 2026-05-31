#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class ShipVfxValidatorTests
    {
        private const string BoostTrailPrefabPath = "Assets/_Prefabs/VFX/BoostTrailRoot.prefab";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void RunAudit_ReportsNoErrorsForCurrentAuthorityChain()
        {
            ResetAuthorityState();

            var results = ShipVfxValidator.RunAudit(showDialog: false, logToConsole: false);

            Assert.That(results.Any(result => result.Severity == ShipVfxValidator.Severity.Error), Is.False);
        }

        [Test]
        public void RunAudit_ReportsErrorWhenBoostActivationHaloLegacyNodeReturns()
        {
            try
            {
                ResetAuthorityState();
                AddLegacyBoostActivationHaloNode();

                var results = ShipVfxValidator.RunAudit(showDialog: false, logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == ShipVfxValidator.Severity.Error &&
                        result.Scope == "BoostTrail Prefab" &&
                        result.Message.Contains("BoostActivationHalo")),
                    Is.True);
            }
            finally
            {
                ResetAuthorityState();
            }
        }

        [Test]
        public void SceneBinderRunAudit_ReportsMissingBloomVolumeWithoutCreatingIt()
        {
            try
            {
                ResetAuthorityState();
                DeleteBoostTrailBloomVolume();

                var results = ShipBoostTrailSceneBinder.RunAudit(logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == ShipBoostTrailSceneBinder.Severity.Error &&
                        result.Message.Contains("BoostTrailBloomVolume")),
                    Is.True);
                Assert.That(GameObject.Find("BoostTrailBloomVolume"), Is.Null, "Audit must be read-only and must not create the missing scene volume.");
            }
            finally
            {
                ResetAuthorityState();
            }
        }

        private static void AddLegacyBoostActivationHaloNode()
        {
            var root = PrefabUtility.LoadPrefabContents(BoostTrailPrefabPath);
            try
            {
                Assert.That(root, Is.Not.Null, "Test setup failed: missing BoostTrailRoot prefab.");
                var legacyNode = new GameObject("BoostActivationHalo");
                legacyNode.transform.SetParent(root.transform, false);
                PrefabUtility.SaveAsPrefabAsset(root, BoostTrailPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ResetAuthorityState()
        {
            EnsureSampleSceneLoaded();
            BoostTrailPrefabCreator.CreateOrRebuildBoostTrailRootPrefab(showDialog: false);
            ShipBoostTrailSceneBinder.SetupBoostTrailSceneReferences();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void DeleteBoostTrailBloomVolume()
        {
            var bloomVolume = GameObject.Find("BoostTrailBloomVolume");
            if (bloomVolume != null)
                Object.DestroyImmediate(bloomVolume);
        }

        private static void EnsureSampleSceneLoaded()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == SampleScenePath)
                return;

            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
