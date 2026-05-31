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
        private const string ShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
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
        public void ShipPrefabRebuilderRunAudit_ReportsMissingShipBodyWithoutRebuildingIt()
        {
            try
            {
                ResetAuthorityState();
                DeleteShipPrefabVisualChild("Ship_Sprite_Body");

                var results = ShipPrefabRebuilder.RunAudit(logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == ShipPrefabRebuilder.Severity.Error &&
                        result.Message.Contains("Ship_Sprite_Body")),
                    Is.True);
                Assert.That(ShipPrefabHasVisualChild("Ship_Sprite_Body"), Is.False, "Audit must be read-only and must not rebuild missing Ship.prefab visual children.");
            }
            finally
            {
                ResetAuthorityState();
            }
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

        [Test]
        public void MaterialTextureLinkerRunAudit_ReportsBrokenTextureBindingWithoutRepairingIt()
        {
            const string materialPath = "Assets/_Art/VFX/BoostTrail/Materials/mat_flame_trail.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, "Test setup failed: missing mat_flame_trail material.");

            var originalTexture = material.GetTexture("_BaseMap");

            try
            {
                material.SetTexture("_BaseMap", null);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                var results = MaterialTextureLinker.RunAudit(logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == MaterialTextureLinker.Severity.Error &&
                        result.Message.Contains("mat_flame_trail._BaseMap")),
                    Is.True);
                Assert.That(material.GetTexture("_BaseMap"), Is.Null, "Audit must be read-only and must not repair the broken texture binding.");
            }
            finally
            {
                material.SetTexture("_BaseMap", originalTexture);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void RunAudit_IncludesMaterialTextureLinkerAuthorityErrorsWithoutRepairingIt()
        {
            const string materialPath = "Assets/_Art/VFX/BoostTrail/Materials/mat_flame_trail.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, "Test setup failed: missing mat_flame_trail material.");

            var originalTexture = material.GetTexture("_BaseMap");

            try
            {
                ResetAuthorityState();
                material.SetTexture("_BaseMap", null);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                var results = ShipVfxValidator.RunAudit(showDialog: false, logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == ShipVfxValidator.Severity.Error &&
                        result.Scope == "Authority Audit/MaterialTextureLinker" &&
                        result.Message.Contains("mat_flame_trail._BaseMap")),
                    Is.True);
                Assert.That(material.GetTexture("_BaseMap"), Is.Null, "Aggregated audit must remain read-only and must not repair the broken texture binding.");
            }
            finally
            {
                material.SetTexture("_BaseMap", originalTexture);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void BoostTrailPrefabCreatorRunAudit_ReportsMissingAresTrailWithoutRebuildingIt()
        {
            try
            {
                ResetAuthorityState();
                DeleteBoostTrailPrefabChild("AresBoostTrail");

                var results = BoostTrailPrefabCreator.RunAudit(logToConsole: false);

                Assert.That(
                    results.Any(result =>
                        result.Severity == BoostTrailPrefabCreator.Severity.Error &&
                        result.Message.Contains("AresBoostTrail")),
                    Is.True);
                Assert.That(BoostTrailPrefabHasChild("AresBoostTrail"), Is.False, "Audit must be read-only and must not rebuild missing prefab children.");
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

        private static void DeleteBoostTrailPrefabChild(string childName)
        {
            var root = PrefabUtility.LoadPrefabContents(BoostTrailPrefabPath);
            try
            {
                Assert.That(root, Is.Not.Null, "Test setup failed: missing BoostTrailRoot prefab.");
                var child = root.transform.Find(childName);
                Assert.That(child, Is.Not.Null, $"Test setup failed: missing {childName} before deletion.");
                Object.DestroyImmediate(child.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, BoostTrailPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool BoostTrailPrefabHasChild(string childName)
        {
            var root = PrefabUtility.LoadPrefabContents(BoostTrailPrefabPath);
            try
            {
                Assert.That(root, Is.Not.Null, "Test setup failed: missing BoostTrailRoot prefab.");
                return root.transform.Find(childName) != null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void DeleteShipPrefabVisualChild(string childName)
        {
            var root = PrefabUtility.LoadPrefabContents(ShipPrefabPath);
            try
            {
                Assert.That(root, Is.Not.Null, "Test setup failed: missing Ship prefab.");
                var shipVisual = root.transform.Find("ShipVisual");
                Assert.That(shipVisual, Is.Not.Null, "Test setup failed: missing ShipVisual before deletion.");
                var child = shipVisual.Find(childName);
                Assert.That(child, Is.Not.Null, $"Test setup failed: missing {childName} before deletion.");
                Object.DestroyImmediate(child.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, ShipPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ShipPrefabHasVisualChild(string childName)
        {
            var root = PrefabUtility.LoadPrefabContents(ShipPrefabPath);
            try
            {
                Assert.That(root, Is.Not.Null, "Test setup failed: missing Ship prefab.");
                var shipVisual = root.transform.Find("ShipVisual");
                return shipVisual != null && shipVisual.Find(childName) != null;
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
            ShipPrefabRebuilder.ForceRebuildSpriteLayersSilently();
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
