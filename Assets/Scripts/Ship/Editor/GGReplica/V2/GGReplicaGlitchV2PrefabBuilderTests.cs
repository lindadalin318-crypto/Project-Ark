#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaGlitchV2PrefabBuilderTests
    {
        private const string PrefabPath = "Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab";

        [Test]
        public void BuildPrefab_CreatesOriginalPlayerViewModuleRigAndInputRuntime()
        {
            GGReplicaGlitchV2PrefabBuilder.BuildPrefab();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo("Ship_GGReplicaV2"));
            Assert.That(prefab.GetComponent<GGReplicaPlayerViewAdapter>(), Is.Null, "V2 must not reuse the sprite-pack PlayerView MVP adapter.");
            Assert.That(prefab.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "V2 must not reuse the legacy five-state sprite switcher.");

            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GGReplicaGlitchInputDriver>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GGReplicaGlitchMotor>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GGReplicaGlitchView>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<AudioSource>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GGReplicaGlitchAudioFeedback>(), Is.Not.Null);

            var visualRoot = prefab.transform.Find("GGGlitchVisualRoot");
            Assert.That(visualRoot, Is.Not.Null);

            string[] requiredRoots =
            {
                "BodyLayers",
                "CoreModule",
                "BoostModule",
                "LQTrailModule",
                "LQTrailsContainer",
                "ShapeTrailModule",
                "DarkTrailModule",
                "FluxySolver",
                "FluxyGrabModule",
                "GrabModule",
                "HoldModule",
                "HoldParticles",
                "HoldProgress",
                "HoldFieldRing",
                "HoldTetherLine",
                "Grab_Hands",
                "FluxyGrabHolo_R",
                "FluxyGrabHolo_L",
                "GrabThrowPointer",
                "GrabLockRing",
                "GrabReleasePulse",
                "GrabReleaseBurst",
                "GrabReleaseThrowLine",
                "GrabTargetHolo",
                "GrabRippableOverlay",
                "HealModule",
                "FireAimModule",
                "AdditiveCore_Dodge",
                "DodgeHalf_Sprite",
                "Dodge_Sprite (used for old outline trail)",
                "ShapeTrail_Dodge (old outline trail)",
                "AdditiveTrail_Dodge",
                "MainAttackState",
                "MainAttackFireState",
                "MainAttackStateHitbox",
                "GlitchEnergyReadyParticles (weapon once)",
                "vfx_boost_trail_loop_enhanced",
                "vfx_boost_trail_burst_enhanced",
                "ps_techno_flame_trail_R",
                "ps_techno_flame_trail_quick",
                "ps_techno_flame_trail_start",
                "ps_ember_trail",
                "startrails",
                "startrails_long",
                "ShapeShiftStateHitbox"
            };

            foreach (string childName in requiredRoots)
            {
                Assert.That(FindDeep(visualRoot, childName), Is.Not.Null, $"Missing original PlayerView module category `{childName}`.");
            }

            var body = visualRoot.Find("BodyLayers");
            AssertLayerRenderer(body, "Ship_Sprite_Solid");
            AssertLayerRenderer(body, "Ship_Sprite_Liquid");
            AssertLayerRenderer(body, "Ship_Sprite_HL");
            AssertLayerRenderer(body, "Ship_Sprite_Back");
            AssertLayerRenderer(body, "Ship_Sprite_Core");
            AssertLayerRenderer(body, "Dodge_Sprite");
            Assert.That(body!.Find("Ship_Sprite_HL")!.GetComponent<SpriteRenderer>().sharedMaterial.name, Is.EqualTo("GGReplicaPlayerShipHL"));

            var boostParticles = visualRoot.Find("BoostModule")!.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(boostParticles.Select(p => p.name), Does.Contain("vfx_boost_trail_loop_enhanced"));
            Assert.That(boostParticles.Select(p => p.name), Does.Contain("ps_techno_flame_trail_start"));
            Assert.That(boostParticles.Select(p => p.name), Does.Contain("ps_ember_trail"));
            var flameTrail = boostParticles.First(p => p.name == "ps_techno_flame_trail_R").GetComponent<ParticleSystemRenderer>();
            Assert.That(flameTrail.sharedMaterial, Is.Not.Null);
            Assert.That(flameTrail.sharedMaterial.name, Is.EqualTo("GGReplicaEngineTrail"));
            var dodgeShell = visualRoot.Find("DodgeModule/ps_dodge_shell")!.GetComponent<ParticleSystemRenderer>();
            Assert.That(dodgeShell.sharedMaterial, Is.Not.Null);
            Assert.That(dodgeShell.sharedMaterial.name, Is.EqualTo("GGReplicaDodgeParticles"));
            var grabRight = visualRoot.Find("GrabModule/Ship_Sprite_Solid_Grab_R")!.GetComponent<SpriteRenderer>();
            var grabLeft = visualRoot.Find("GrabModule/Ship_Sprite_Solid_Grab_L")!.GetComponent<SpriteRenderer>();
            Assert.That(grabRight.sharedMaterial, Is.Not.Null);
            Assert.That(grabRight.sharedMaterial.name, Is.EqualTo("GGReplicaFakeFluxy"));
            Assert.That(grabLeft.sharedMaterial.name, Is.EqualTo("GGReplicaFakeFluxy"));

            var trails = visualRoot.Find("LQTrailsContainer")!.GetComponentsInChildren<TrailRenderer>(true);
            Assert.That(trails.Length, Is.GreaterThanOrEqualTo(2), "V2 must use a trail stack, not one decorative line.");
            Assert.That(trails.Any(t => t.sharedMaterial != null && t.sharedMaterial.name == "GGReplicaPlayerLQTrail"), Is.True);
            var fluxyTrail = visualRoot.Find("LQTrailModule/fluxy_like_lq_trail")!.GetComponent<TrailRenderer>();
            Assert.That(fluxyTrail.sharedMaterial, Is.Not.Null);
            Assert.That(fluxyTrail.sharedMaterial.name, Is.EqualTo("GGReplicaFakeFluxy"));

            var audio = prefab.GetComponent<GGReplicaGlitchAudioFeedback>();
            var audioSO = new SerializedObject(audio);
            AssertReference(audioSO, "_profile", "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset");
            Assert.That(audioSO.FindProperty("_audioSource").objectReferenceValue, Is.SameAs(prefab.GetComponent<AudioSource>()));

            var motor = prefab.GetComponent<GGReplicaGlitchMotor>();
            var motorSO = new SerializedObject(motor);
            AssertReference(motorSO, "_feelProfile", "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset");
            Assert.That(motorSO.FindProperty("_audioFeedback").objectReferenceValue, Is.SameAs(audio));

            var view = prefab.GetComponent<GGReplicaGlitchView>();
            var viewSO = new SerializedObject(view);
            Assert.That(viewSO.FindProperty("_visualRoot").objectReferenceValue, Is.SameAs(visualRoot));
            AssertReference(viewSO, "_feelProfile", "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset");
            AssertReference(viewSO, "_playerSkin", "Assets/_Data/Ship/GGReplicaPlayerSkin.asset");
            Assert.That(viewSO.FindProperty("_bodyLayersRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("BodyLayers")));
            Assert.That(viewSO.FindProperty("_solidRenderer").objectReferenceValue, Is.SameAs(body.Find("Ship_Sprite_Solid").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_liquidRenderer").objectReferenceValue, Is.SameAs(body.Find("Ship_Sprite_Liquid").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_highlightRenderer").objectReferenceValue, Is.SameAs(body.Find("Ship_Sprite_HL").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_boostModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("BoostModule").gameObject));
            Assert.That(viewSO.FindProperty("_boostBurstParticles").arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(viewSO.FindProperty("_dodgeBurstParticles").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_dodgeTrailParticles").arraySize, Is.EqualTo(2));
            Assert.That(viewSO.FindProperty("_lqTrailRenderers").arraySize, Is.EqualTo(2), "PlayerViewLQTrailModule should serialize startrails separately from Fluxy/Shape/Dark lanes.");
            Assert.That(viewSO.FindProperty("_lqTrailRenderers").GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(visualRoot.Find("LQTrailsContainer/startrails").GetComponent<TrailRenderer>()));
            Assert.That(viewSO.FindProperty("_lqTrailRenderers").GetArrayElementAtIndex(1).objectReferenceValue, Is.SameAs(visualRoot.Find("LQTrailsContainer/startrails_long").GetComponent<TrailRenderer>()));
            Assert.That(viewSO.FindProperty("_darkTrailRenderers").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_darkTrailRenderers").GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(visualRoot.Find("DarkTrailModule/dark_trail").GetComponent<TrailRenderer>()));
            Assert.That(viewSO.FindProperty("_shapeTrailRenderers").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_shapeTrailRenderers").GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(visualRoot.Find("ShapeTrailModule/shape_trail").GetComponent<TrailRenderer>()));
            Assert.That(viewSO.FindProperty("_fluxyTrailRenderer").objectReferenceValue, Is.SameAs(fluxyTrail));
            Assert.That(viewSO.FindProperty("_dodgeHalfRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("DodgeModule/DodgeHalf_Sprite").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_dodgeAdditiveCoreRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("DodgeModule/AdditiveCore_Dodge").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_lqTrailsContainer").objectReferenceValue, Is.SameAs(visualRoot.Find("LQTrailsContainer").gameObject));
            Assert.That(viewSO.FindProperty("_grabModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("GrabModule").gameObject));
            Assert.That(viewSO.FindProperty("_fluxyGrabModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule").gameObject));
            Assert.That(viewSO.FindProperty("_grabRenderers").arraySize, Is.EqualTo(2));
            Assert.That(viewSO.FindProperty("_grabFluxyRenderers").arraySize, Is.EqualTo(2));
            Assert.That(viewSO.FindProperty("_grabThrowPointer").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabThrowPointer").GetComponent<LineRenderer>()));
            Assert.That(viewSO.FindProperty("_grabLockRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabLockRing").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_grabReleaseRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabReleasePulse").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_grabReleaseParticles").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_grabReleaseThrowLine").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabReleaseThrowLine").GetComponent<LineRenderer>()));
            Assert.That(viewSO.FindProperty("_grabTargetRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabTargetHolo").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_grabTargetOverlayRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("FluxyGrabModule/GrabRippableOverlay").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_holdModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("HoldModule").gameObject));
            Assert.That(viewSO.FindProperty("_holdParticles").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_holdFieldRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("HoldModule/HoldFieldRing").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_holdProgressRenderer").objectReferenceValue, Is.SameAs(visualRoot.Find("HoldModule/HoldProgress").GetComponent<SpriteRenderer>()));
            Assert.That(viewSO.FindProperty("_holdTetherLine").objectReferenceValue, Is.SameAs(visualRoot.Find("HoldModule/HoldTetherLine").GetComponent<LineRenderer>()));
            Assert.That(viewSO.FindProperty("_healModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("HealModule").gameObject));
            Assert.That(viewSO.FindProperty("_healParticles").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_healRenderers").arraySize, Is.GreaterThanOrEqualTo(2));
            Assert.That(visualRoot.Find("HealModule/Healing_0"), Is.Not.Null);
            Assert.That(visualRoot.Find("HealModule/vfx_dot_001"), Is.Not.Null);
            Assert.That(viewSO.FindProperty("_fireAimModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("FireAimModule").gameObject));
            Assert.That(viewSO.FindProperty("_fireAimParticles").arraySize, Is.EqualTo(1));
            Assert.That(viewSO.FindProperty("_fireAimRenderers").arraySize, Is.GreaterThanOrEqualTo(3));
        }

        private static void AssertReference(SerializedObject serializedObject, string propertyName, string expectedAssetPath)
        {
            var expected = AssetDatabase.LoadAssetAtPath<Object>(expectedAssetPath);
            Assert.That(expected, Is.Not.Null, $"Missing expected asset `{expectedAssetPath}`.");
            Assert.That(serializedObject.FindProperty(propertyName).objectReferenceValue, Is.SameAs(expected), $"{propertyName} should reference `{expectedAssetPath}`.");
        }

        private static void AssertLayerRenderer(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            Assert.That(child, Is.Not.Null, $"Missing body layer `{childName}`.");
            Assert.That(child!.GetComponent<SpriteRenderer>(), Is.Not.Null, $"Missing SpriteRenderer on `{childName}`.");
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            if (root.name == childName) return root;
            foreach (Transform child in root)
            {
                var result = FindDeep(child, childName);
                if (result != null) return result;
            }

            return null;
        }
    }
}
#endif
