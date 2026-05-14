#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaMaterialAssetBuilderTests
    {
        private const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
        private const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
        private const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";

        [Test]
        public void BuildVisualMaterials_CreatesMaterialsWithOriginalGGParameters()
        {
            GGReplicaMaterialAssetBuilder.BuildVisualMaterials();

            var playerShipHl = AssetDatabase.LoadAssetAtPath<Material>(PlayerShipHlMaterialPath);
            var teleportScheme = AssetDatabase.LoadAssetAtPath<Material>(TeleportSchemeMaterialPath);
            var playerLqTrail = AssetDatabase.LoadAssetAtPath<Material>(PlayerLqTrailMaterialPath);

            Assert.That(playerShipHl, Is.Not.Null);
            Assert.That(teleportScheme, Is.Not.Null);
            Assert.That(playerLqTrail, Is.Not.Null);

            Assert.That(playerShipHl.shader.name, Is.EqualTo("ProjectArk/GGReplica/PlayerShipHighlight"));
            Assert.That(playerShipHl.GetFloat("_Intensity"), Is.EqualTo(8f).Within(0.001f));
            Assert.That(playerShipHl.GetFloat("_Smooth"), Is.EqualTo(0.01f).Within(0.001f));
            AssertColor(playerShipHl.GetColor("_Tint"), new Color(0.54509807f, 0.09019608f, 1f, 1f));

            Assert.That(teleportScheme.shader.name, Is.EqualTo("ProjectArk/GGReplica/TeleportScheme"));
            Assert.That(teleportScheme.GetFloat("_Intensity"), Is.EqualTo(1f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_State"), Is.EqualTo(0f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_ScanScale"), Is.EqualTo(8f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_GlitchStrength"), Is.EqualTo(0.3f).Within(0.001f));

            Assert.That(playerLqTrail.shader.name, Is.EqualTo("ProjectArk/GGReplica/PlayerLQTrail"));
            AssertColor(playerLqTrail.GetColor("_MainColor"), new Color(0.12054504f, 0f, 0.18867922f, 1f));
            AssertColor(playerLqTrail.GetColor("_EdgeColor"), new Color(0.61328405f, 0f, 0.80784315f, 0f));
            Assert.That(playerLqTrail.GetVector("_NoiseParams"), Is.EqualTo(new Vector4(1f, 1f, 0.2f, 0.7f)));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
#endif
