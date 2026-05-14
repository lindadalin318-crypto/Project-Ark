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
                "HealModule",
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

            var trails = visualRoot.Find("LQTrailsContainer")!.GetComponentsInChildren<TrailRenderer>(true);
            Assert.That(trails.Length, Is.GreaterThanOrEqualTo(2), "V2 must use a trail stack, not one decorative line.");
            Assert.That(trails.Any(t => t.sharedMaterial != null && t.sharedMaterial.name == "GGReplicaPlayerLQTrail"), Is.True);

            var motor = prefab.GetComponent<GGReplicaGlitchMotor>();
            var motorSO = new SerializedObject(motor);
            AssertReference(motorSO, "_feelProfile", "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset");

            var view = prefab.GetComponent<GGReplicaGlitchView>();
            var viewSO = new SerializedObject(view);
            Assert.That(viewSO.FindProperty("_visualRoot").objectReferenceValue, Is.SameAs(visualRoot));
            Assert.That(viewSO.FindProperty("_boostModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("BoostModule").gameObject));
            Assert.That(viewSO.FindProperty("_lqTrailsContainer").objectReferenceValue, Is.SameAs(visualRoot.Find("LQTrailsContainer").gameObject));
            Assert.That(viewSO.FindProperty("_grabModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("GrabModule").gameObject));
            Assert.That(viewSO.FindProperty("_healModuleRoot").objectReferenceValue, Is.SameAs(visualRoot.Find("HealModule").gameObject));
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
