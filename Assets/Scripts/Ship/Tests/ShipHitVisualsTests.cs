using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class ShipHitVisualsTests
    {
        [Test]
        public void OnDamageTaken_IgnoresHealingOrRefreshEvents()
        {
            var root = new GameObject("ShipHitVisualsTestRoot");
            var solid = CreateRenderer(root.transform, "Solid", new Color(0.2f, 0.3f, 0.4f, 1f));
            var liquid = CreateRenderer(root.transform, "Liquid", new Color(0.1f, 0.2f, 0.3f, 0.5f));
            var highlight = CreateRenderer(root.transform, "Highlight", new Color(0.4f, 0.5f, 0.6f, 0.7f));
            var core = CreateRenderer(root.transform, "Core", new Color(0.6f, 0.7f, 0.8f, 0.3f));
            var visuals = root.AddComponent<ShipHitVisuals>();
            var settings = ScriptableObject.CreateInstance<ShipJuiceSettingsSO>();

            SetPrivateField(visuals, "_solidRenderer", solid);
            SetPrivateField(visuals, "_liquidRenderer", liquid);
            SetPrivateField(visuals, "_hlRenderer", highlight);
            SetPrivateField(visuals, "_coreRenderer", core);
            SetPrivateField(visuals, "_juiceSettings", settings);
            visuals.Initialize(liquid.color, solid.color, highlight.color, core.color, null);

            visuals.OnDamageTaken(-10f, 50f);
            visuals.OnDamageTaken(0f, 50f);

            Assert.That(solid.color, Is.EqualTo(new Color(0.2f, 0.3f, 0.4f, 1f)));
            Assert.That(liquid.color, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.5f)));
            Assert.That(highlight.color, Is.EqualTo(new Color(0.4f, 0.5f, 0.6f, 0.7f)));
            Assert.That(core.color, Is.EqualTo(new Color(0.6f, 0.7f, 0.8f, 0.3f)));

            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnDamageTaken_PositiveDamagePlaysHitSpark()
        {
            var root = new GameObject("ShipHitVisualsSparkTestRoot");
            var spark = CreateParticleSystem(root.transform, "HitSpark");
            var visuals = root.AddComponent<ShipHitVisuals>();
            var settings = ScriptableObject.CreateInstance<ShipJuiceSettingsSO>();

            SetPrivateField(visuals, "_hitSparkParticles", spark);
            SetPrivateField(visuals, "_juiceSettings", settings);
            visuals.Initialize(Color.white, Color.white, Color.white, Color.white, null);

            visuals.OnDamageTaken(10f, 40f);

            Assert.That(spark.isPlaying, Is.True);

            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnDamageTaken_PositiveDamageShowsHitMaskOverlay()
        {
            var root = new GameObject("ShipHitVisualsMaskTestRoot");
            var hitMask = CreateRenderer(root.transform, "HitMask", Color.clear);
            hitMask.enabled = false;
            var visuals = root.AddComponent<ShipHitVisuals>();
            var settings = ScriptableObject.CreateInstance<ShipJuiceSettingsSO>();

            SetPrivateField(visuals, "_hitMaskRenderer", hitMask);
            SetPrivateField(visuals, "_juiceSettings", settings);
            visuals.Initialize(Color.white, Color.white, Color.white, Color.white, null);

            visuals.OnDamageTaken(10f, 40f);

            Assert.That(hitMask.enabled, Is.True);
            Assert.That(hitMask.color.a, Is.GreaterThan(0f));

            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ResetState_StopsAndClearsHitSpark()
        {
            var root = new GameObject("ShipHitVisualsSparkResetTestRoot");
            var spark = CreateParticleSystem(root.transform, "HitSpark");
            var visuals = root.AddComponent<ShipHitVisuals>();
            var settings = ScriptableObject.CreateInstance<ShipJuiceSettingsSO>();

            SetPrivateField(visuals, "_hitSparkParticles", spark);
            SetPrivateField(visuals, "_juiceSettings", settings);
            visuals.Initialize(Color.white, Color.white, Color.white, Color.white, null);

            visuals.OnDamageTaken(10f, 40f);
            Assert.That(spark.isPlaying, Is.True);

            visuals.ResetState();

            Assert.That(spark.isPlaying, Is.False);
            Assert.That(spark.particleCount, Is.EqualTo(0));

            Object.DestroyImmediate(settings);
            Object.DestroyImmediate(root);
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.color = color;
            return renderer;
        }

        private static ParticleSystem CreateParticleSystem(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.08f;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.maxParticles = 8;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
