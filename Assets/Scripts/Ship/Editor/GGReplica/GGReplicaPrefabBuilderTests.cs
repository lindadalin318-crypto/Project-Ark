#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaPrefabBuilderTests
    {
        private const string LiveShipPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string ReplicaShipPath = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";
        private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";

        [Test]
        public void BuildExperimentalPrefab_CreatesReplicaWithPlayerViewHierarchyAndModulesWired()
        {
            var liveShip = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShipPath);
            Assert.That(liveShip, Is.Not.Null, "Live Ship.prefab must exist for replica builder test.");
            Assert.That(liveShip.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Live Ship.prefab must not contain legacy replica adapter before build.");
            Assert.That(liveShip.GetComponent<GGReplicaPlayerViewAdapter>(), Is.Null, "Live Ship.prefab must not contain new replica adapter before build.");

            GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();
            GGReplicaPrefabBuilder.BuildExperimentalPrefab();

            var replica = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShipPath);
            Assert.That(replica, Is.Not.Null);
            Assert.That(replica.name, Is.EqualTo("Ship_GGReplica"));
            Assert.That(liveShip.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Builder must not modify live Ship.prefab.");
            Assert.That(liveShip.GetComponent<GGReplicaPlayerViewAdapter>(), Is.Null, "Builder must not modify live Ship.prefab.");
            Assert.That(replica.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Rebuilt prefab must not use legacy five-state adapter.");

            var playerView = replica.GetComponent<GGReplicaPlayerViewAdapter>();
            var coreModule = replica.GetComponent<GGReplicaCoreVisualModule>();
            var boostModule = replica.GetComponent<GGReplicaBoostVisualModule>();
            var shapeModule = replica.GetComponent<GGReplicaShapeVisualModule>();
            var materialModule = replica.GetComponent<GGReplicaMaterialVisualModule>();
            Assert.That(playerView, Is.Not.Null);
            Assert.That(coreModule, Is.Not.Null);
            Assert.That(boostModule, Is.Not.Null);
            Assert.That(shapeModule, Is.Not.Null);
            Assert.That(materialModule, Is.Not.Null);

            var liveShipView = replica.GetComponent<ShipView>();
            Assert.That(liveShipView, Is.Not.Null);
            Assert.That(liveShipView.enabled, Is.False, "GGReplica PlayerView lane must own ship visuals, not live ShipView.");

            var shipVisual = replica.transform.Find("ShipVisual");
            Assert.That(shipVisual, Is.Not.Null);
            AssertDirectRendererDisabled(shipVisual, "Ship_Sprite_Liquid");
            AssertDirectRendererDisabled(shipVisual, "Ship_Sprite_HL");
            AssertDirectRendererDisabled(shipVisual, "Dodge_Sprite");
            AssertDirectRendererDisabled(shipVisual, "Ship_Sprite_Solid");
            AssertDirectRendererDisabled(shipVisual, "Ship_Sprite_Back");
            AssertDirectRendererDisabled(shipVisual, "Ship_Sprite_Core");

            var viewRoot = shipVisual.Find("GGPlayerViewRoot");
            Assert.That(viewRoot, Is.Not.Null);
            var requiredChildren = new[]
            {
                "Ship_Sprite_Liquid",
                "Ship_Sprite_HL",
                "Dodge_Sprite",
                "Ship_Sprite_Solid",
                "Ship_Sprite_Back",
                "Ship_Sprite_Solid_Grab_R",
                "Ship_Sprite_Solid_Grab_L",
                "Core_Sprite_Reactor",
                "Core_Sprite_Eye",
                "View",
                "Dodge_Half_Sprite"
            };
            Assert.That(viewRoot.childCount, Is.EqualTo(requiredChildren.Length), "GGPlayerViewRoot should contain only the required PlayerView render nodes.");
            foreach (var childName in requiredChildren)
            {
                var child = viewRoot.Find(childName);
                Assert.That(child, Is.Not.Null, $"Missing GGPlayerViewRoot/{childName}.");
                Assert.That(child.GetComponent<SpriteRenderer>(), Is.Not.Null, $"Missing SpriteRenderer on {childName}.");
            }
            var solidRenderer = viewRoot.Find("Ship_Sprite_Solid").GetComponent<SpriteRenderer>();
            var viewSilhouetteRenderer = viewRoot.Find("View").GetComponent<SpriteRenderer>();
            Assert.That(viewSilhouetteRenderer.sortingOrder, Is.LessThan(solidRenderer.sortingOrder), "View silhouette must render behind state body sprites so state changes remain visible.");
            AssertRendererMaterial(viewRoot, "Ship_Sprite_HL", GGReplicaMaterialAssetBuilder.PlayerShipHlMaterialPath);
            AssertRendererMaterial(viewRoot, "View", GGReplicaMaterialAssetBuilder.TeleportSchemeMaterialPath);
            AssertLegacyBoostTrailRenderersDisabled(shipVisual);
            Assert.That(shipVisual.Find("GGBoostVisualRoot"), Is.Not.Null, "Boost module root should live outside GGPlayerViewRoot.");
            AssertPlayerLqTrail(shipVisual);

            var playerViewSO = new SerializedObject(playerView);
            AssertReference(playerViewSO, "_skin", PlayerSkinPath);
            AssertTransformReference(playerViewSO, "_spritesRoot", viewRoot);
            AssertRendererReference(playerViewSO, "_shipLiquidRenderer", viewRoot, "Ship_Sprite_Liquid");
            AssertRendererReference(playerViewSO, "_shipHighlightRenderer", viewRoot, "Ship_Sprite_HL");
            AssertRendererReference(playerViewSO, "_dodgeRenderer", viewRoot, "Dodge_Sprite");
            AssertRendererReference(playerViewSO, "_shipSolidRenderer", viewRoot, "Ship_Sprite_Solid");
            AssertRendererReference(playerViewSO, "_shipBackRenderer", viewRoot, "Ship_Sprite_Back");
            AssertRendererReference(playerViewSO, "_shipGrabRightRenderer", viewRoot, "Ship_Sprite_Solid_Grab_R");
            AssertRendererReference(playerViewSO, "_shipGrabLeftRenderer", viewRoot, "Ship_Sprite_Solid_Grab_L");
            AssertRendererReference(playerViewSO, "_coreRenderer", viewRoot, "Core_Sprite_Reactor");
            AssertRendererReference(playerViewSO, "_eyeRenderer", viewRoot, "Core_Sprite_Eye");
            AssertRendererReference(playerViewSO, "_viewSilhouetteRenderer", viewRoot, "View");
            AssertRendererReference(playerViewSO, "_dodgeHalfRenderer", viewRoot, "Dodge_Half_Sprite");
            Assert.That(playerViewSO.FindProperty("_coreModule").objectReferenceValue, Is.SameAs(coreModule));
            Assert.That(playerViewSO.FindProperty("_boostModule").objectReferenceValue, Is.SameAs(boostModule));
            Assert.That(playerViewSO.FindProperty("_shapeModule").objectReferenceValue, Is.SameAs(shapeModule));
            Assert.That(playerViewSO.FindProperty("_materialModule").objectReferenceValue, Is.SameAs(materialModule));

            var audioSource = replica.GetComponent<AudioSource>();
            Assert.That(audioSource, Is.Not.Null);

            var boostAdapter = replica.GetComponent<GGReplicaBoostVfxAdapter>();
            Assert.That(boostAdapter, Is.Not.Null);
            var boostSO = new SerializedObject(boostAdapter);
            AssertReference(boostSO, "_profile", "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset");
            Assert.That(boostSO.FindProperty("_audioSource").objectReferenceValue, Is.SameAs(audioSource));
            Assert.That(boostSO.FindProperty("_boost").objectReferenceValue, Is.Not.Null);

            var dashAdapter = replica.GetComponent<GGReplicaDashVfxAdapter>();
            Assert.That(dashAdapter, Is.Not.Null);
            var dashSO = new SerializedObject(dashAdapter);
            AssertReference(dashSO, "_profile", "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset");
            Assert.That(dashSO.FindProperty("_audioSource").objectReferenceValue, Is.SameAs(audioSource));
            Assert.That(dashSO.FindProperty("_dash").objectReferenceValue, Is.Not.Null);

            var feelAdapter = replica.GetComponent<GGReplicaShipFeelAdapter>();
            Assert.That(feelAdapter, Is.Not.Null);
            var feelSO = new SerializedObject(feelAdapter);
            AssertReference(feelSO, "_profile", "Assets/_Data/Ship/GGReplicaShipFeelProfile.asset");
            Assert.That(feelSO.FindProperty("_rigidbody").objectReferenceValue, Is.Not.Null);
            Assert.That(feelSO.FindProperty("_boost").objectReferenceValue, Is.Not.Null);
            Assert.That(feelSO.FindProperty("_dash").objectReferenceValue, Is.Not.Null);
        }

        private static void AssertDirectRendererDisabled(Transform shipVisual, string childName)
        {
            var child = shipVisual.Find(childName);
            if (child == null) return;
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Assert.That(renderer.enabled, Is.False, $"Direct ShipVisual/{childName} renderer should be disabled to avoid double rendering.");
            }
        }

        private static void AssertLegacyBoostTrailRenderersDisabled(Transform shipVisual)
        {
            var boostTrailRoot = shipVisual.Find("BoostTrailRoot");
            if (boostTrailRoot == null) return;

            Assert.That(boostTrailRoot.gameObject.activeSelf, Is.False, "Legacy ShipVisual/BoostTrailRoot should be inactive in the replica prefab; GGBoostVisualRoot owns boost visuals.");
            foreach (var renderer in boostTrailRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(!boostTrailRoot.gameObject.activeSelf || !renderer.enabled, Is.True, $"Legacy ShipVisual/BoostTrailRoot renderer {renderer.name} must not be visible in the replica prefab.");
            }
        }

        private static void AssertRendererMaterial(Transform viewRoot, string childName, string materialPath)
        {
            var renderer = viewRoot.Find(childName)?.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null, $"Missing renderer {childName}.");
            var expected = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(expected, Is.Not.Null, $"Missing expected material {materialPath}.");
            Assert.That(renderer.sharedMaterial, Is.SameAs(expected), $"{childName} must use {materialPath}.");
        }

        private static void AssertPlayerLqTrail(Transform shipVisual)
        {
            var trail = shipVisual.Find("GGPlayerLQTrail")?.GetComponent<TrailRenderer>();
            Assert.That(trail, Is.Not.Null, "Replica should include ShipVisual/GGPlayerLQTrail TrailRenderer.");
            var expectedTrailMaterial = AssetDatabase.LoadAssetAtPath<Material>(GGReplicaMaterialAssetBuilder.PlayerLqTrailMaterialPath);
            Assert.That(expectedTrailMaterial, Is.Not.Null);
            Assert.That(trail.sharedMaterial, Is.SameAs(expectedTrailMaterial));
            Assert.That(trail.time, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(trail.widthMultiplier, Is.EqualTo(4f).Within(0.001f));
            Assert.That(trail.emitting, Is.False);
        }

        private static void AssertTransformReference(SerializedObject so, string propertyName, Transform expected)
        {
            var property = so.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyName}.");
            Assert.That(property.objectReferenceValue, Is.SameAs(expected));
        }

        private static void AssertRendererReference(SerializedObject so, string propertyName, Transform viewRoot, string childName)
        {
            var expected = viewRoot.Find(childName)?.GetComponent<SpriteRenderer>();
            Assert.That(expected, Is.Not.Null, $"Missing expected renderer GGPlayerViewRoot/{childName}.");
            var property = so.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyName}.");
            Assert.That(property.objectReferenceValue, Is.SameAs(expected), $"{propertyName} must reference GGPlayerViewRoot/{childName}, not another same-named renderer.");
        }

        private static void AssertReference(SerializedObject so, string propertyName, string expectedNameOrPath)
        {
            var property = so.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyName}.");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"{propertyName} was not wired.");

            if (expectedNameOrPath.StartsWith("Assets/"))
            {
                Assert.That(AssetDatabase.GetAssetPath(property.objectReferenceValue), Is.EqualTo(expectedNameOrPath));
            }
            else
            {
                Assert.That(property.objectReferenceValue.name, Is.EqualTo(expectedNameOrPath));
            }
        }
    }
}
#endif
