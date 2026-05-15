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
        private const string FakeFluxyMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaFakeFluxy.mat";
        private const string EngineTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaEngineTrail.mat";
        private const string DodgeParticlesMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaDodgeParticles.mat";

        [Test]
        public void BuildVisualMaterials_CreatesMaterialsWithOriginalGGParameters()
        {
            GGReplicaMaterialAssetBuilder.BuildVisualMaterials();

            var playerShipHl = AssetDatabase.LoadAssetAtPath<Material>(PlayerShipHlMaterialPath);
            var teleportScheme = AssetDatabase.LoadAssetAtPath<Material>(TeleportSchemeMaterialPath);
            var playerLqTrail = AssetDatabase.LoadAssetAtPath<Material>(PlayerLqTrailMaterialPath);
            var fakeFluxy = AssetDatabase.LoadAssetAtPath<Material>(FakeFluxyMaterialPath);
            var engineTrail = AssetDatabase.LoadAssetAtPath<Material>(EngineTrailMaterialPath);
            var dodgeParticles = AssetDatabase.LoadAssetAtPath<Material>(DodgeParticlesMaterialPath);

            Assert.That(playerShipHl, Is.Not.Null);
            Assert.That(teleportScheme, Is.Not.Null);
            Assert.That(playerLqTrail, Is.Not.Null);
            Assert.That(fakeFluxy, Is.Not.Null);
            Assert.That(engineTrail, Is.Not.Null);
            Assert.That(dodgeParticles, Is.Not.Null);

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

            Assert.That(fakeFluxy.shader.name, Is.EqualTo("ProjectArk/GGReplica/FakeFluxy"));
            AssertColor(fakeFluxy.GetColor("_BaseColor"), new Color(0f, 0f, 0f, 0f));
            AssertColor(fakeFluxy.GetColor("_GlowColor"), new Color(1.1607844f, 0f, 2.9960785f, 0f));
            Assert.That(fakeFluxy.GetFloat("_DistortionOffset"), Is.EqualTo(-1f).Within(0.001f));
            Assert.That(fakeFluxy.GetFloat("_DepthOffset"), Is.EqualTo(-0.3f).Within(0.001f));
            Assert.That(fakeFluxy.GetFloat("_NoiseScale"), Is.EqualTo(6f).Within(0.001f));
            Assert.That(fakeFluxy.GetFloat("_RimWidth"), Is.EqualTo(0.07f).Within(0.001f));
            Assert.That(fakeFluxy.GetFloat("_FlowPower"), Is.EqualTo(3.77f).Within(0.001f));

            Assert.That(engineTrail.shader.name, Is.EqualTo("ProjectArk/GGReplica/EngineTrail"));
            AssertColor(engineTrail.GetColor("_BottomColor"), new Color(1f, 0f, 0.91456747f, 1f));
            AssertColor(engineTrail.GetColor("_TopColor"), new Color(0.0990566f, 0.8460265f, 1f, 1f));
            AssertColor(engineTrail.GetColor("_GhostColor"), new Color(0.09211465f, 0.8490566f, 0.6468398f, 0.3882353f));
            Assert.That(engineTrail.GetFloat("_MixEffect"), Is.EqualTo(1f).Within(0.001f));
            Assert.That(engineTrail.GetFloat("_NoiseScale"), Is.EqualTo(1.31f).Within(0.001f));
            Assert.That(engineTrail.GetFloat("_Spread"), Is.EqualTo(2f).Within(0.001f));
            Assert.That(engineTrail.GetFloat("_Power"), Is.EqualTo(7f).Within(0.001f));
            Assert.That(engineTrail.GetFloat("_WobbleSpeed"), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(engineTrail.GetVector("_Speed1"), Is.EqualTo(new Vector4(-2f, 0f, 1f, 1f)));
            Assert.That(engineTrail.GetVector("_Speed2"), Is.EqualTo(new Vector4(-1f, 0f, 1f, 1f)));
            Assert.That(engineTrail.GetVector("_Speed3"), Is.EqualTo(new Vector4(-1.61f, 0f, 1f, 0.51f)));

            Assert.That(dodgeParticles.shader.name, Is.EqualTo("ProjectArk/GGReplica/DodgeParticles"));
            AssertColor(dodgeParticles.GetColor("_Color"), new Color(1f, 0.78035855f, 0f, 1f));
            AssertColor(dodgeParticles.GetColor("_TintColor"), new Color(1f, 1f, 1f, 1f));
            AssertColor(dodgeParticles.GetColor("_EmissionColor"), new Color(0f, 0f, 0f, 1f));
            Assert.That(dodgeParticles.GetFloat("_InvFade"), Is.EqualTo(3f).Within(0.001f));
            Assert.That(dodgeParticles.GetFloat("_SrcBlend"), Is.EqualTo(1f).Within(0.001f));
            Assert.That(dodgeParticles.GetFloat("_DstBlend"), Is.EqualTo(0f).Within(0.001f));
            Assert.That(dodgeParticles.GetFloat("_ZWrite"), Is.EqualTo(1f).Within(0.001f));
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
