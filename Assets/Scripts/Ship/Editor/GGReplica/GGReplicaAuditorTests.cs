#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaAuditorTests
    {
        private const string ReplicaShipPath = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";

        [Test]
        public void RunAudit_ReportsNoErrorsForIsolatedReplicaLane()
        {
            GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();
            GGReplicaPrefabBuilder.BuildExperimentalPrefab();
            GGReplicaTestSceneBuilder.BuildTestScene();

            var results = GGReplicaAuditor.RunAudit(logToConsole: false);

            Assert.That(results.Any(result => result.Severity == GGReplicaAuditor.Severity.Error), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("Missing GGReplicaPlayerSkin")), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("Missing required GGPlayerViewRoot child")), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("Live Ship.prefab has GGReplicaPlayerViewAdapter")), Is.False);
            Assert.That(results.Any(result => result.Message.Contains("SampleScene contains GGReplica")), Is.False);
        }

        [Test]
        public void RunAudit_ReportsErrorWhenPlayerViewMaterialChainIsBroken()
        {
            GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();
            GGReplicaPrefabBuilder.BuildExperimentalPrefab();
            try
            {
                BreakHighlightMaterial();

                var results = GGReplicaAuditor.RunAudit(logToConsole: false);

                Assert.That(results.Any(result => result.Severity == GGReplicaAuditor.Severity.Error && result.Message.Contains("Ship_Sprite_HL material")), Is.True);
            }
            finally
            {
                GGReplicaPrefabBuilder.BuildExperimentalPrefab();
                GGReplicaTestSceneBuilder.BuildTestScene();
            }
        }

        private static void BreakHighlightMaterial()
        {
            var root = PrefabUtility.LoadPrefabContents(ReplicaShipPath);
            try
            {
                var renderer = root.transform.Find("ShipVisual/GGPlayerViewRoot/Ship_Sprite_HL")?.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, "Test setup failed: missing GGPlayerViewRoot/Ship_Sprite_HL.");
                renderer!.sharedMaterial = null;
                PrefabUtility.SaveAsPrefabAsset(root, ReplicaShipPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
