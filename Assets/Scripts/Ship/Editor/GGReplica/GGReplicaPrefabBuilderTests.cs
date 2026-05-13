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

        [Test]
        public void BuildExperimentalPrefab_CreatesReplicaWithAdapterWired()
        {
            var liveShip = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShipPath);
            Assert.That(liveShip, Is.Not.Null, "Live Ship.prefab must exist for replica builder test.");
            Assert.That(liveShip.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Live Ship.prefab must not contain replica-only adapter before build.");

            GGReplicaPrefabBuilder.BuildExperimentalPrefab();

            var replica = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShipPath);
            Assert.That(replica, Is.Not.Null);
            Assert.That(replica.name, Is.EqualTo("Ship_GGReplica"));
            Assert.That(liveShip.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Builder must not modify live Ship.prefab.");

            var adapter = replica.GetComponent<GGReplicaShipViewAdapter>();
            Assert.That(adapter, Is.Not.Null);

            var adapterSO = new SerializedObject(adapter);
            AssertReference(adapterSO, "_profile", "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset");
            AssertReference(adapterSO, "_backRenderer", "Ship_Sprite_Back");
            AssertReference(adapterSO, "_liquidRenderer", "Ship_Sprite_Liquid");
            AssertReference(adapterSO, "_highlightRenderer", "Ship_Sprite_HL");
            AssertReference(adapterSO, "_solidRenderer", "Ship_Sprite_Solid");
            AssertReference(adapterSO, "_coreRenderer", "Ship_Sprite_Core");
            AssertReference(adapterSO, "_dodgeGhostRenderer", "Dodge_Sprite");

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
