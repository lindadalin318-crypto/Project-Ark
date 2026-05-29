#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class ShipPrefabHitSparkTests
    {
        private const string ShipPrefabPath = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string HitSparkPath = "ShipVisual/Ship_HitSpark";

        [Test]
        public void ShipPrefab_HitSparkRenderer_UsesSupportedExplicitMaterial()
        {
            var root = PrefabUtility.LoadPrefabContents(ShipPrefabPath);
            try
            {
                var hitSpark = root.transform.Find(HitSparkPath);
                Assert.That(hitSpark, Is.Not.Null, $"Missing {HitSparkPath} in Ship.prefab.");

                var renderer = hitSpark!.GetComponent<ParticleSystemRenderer>();
                Assert.That(renderer, Is.Not.Null, "Ship_HitSpark must have a ParticleSystemRenderer.");

                var material = renderer!.sharedMaterial;
                Assert.That(material, Is.Not.Null, "Ship_HitSpark must use an explicit material to avoid Unity magenta fallback rendering.");
                Assert.That(material!.shader, Is.Not.Null, "Ship_HitSpark material must have a shader.");
                Assert.That(material.shader.isSupported, Is.True, $"Ship_HitSpark shader '{material.shader.name}' must be supported by the active render pipeline.");
                Assert.That(material.shader.name, Does.Not.Contain("Hidden/InternalErrorShader"), "Ship_HitSpark must not render with Unity's magenta error shader.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
