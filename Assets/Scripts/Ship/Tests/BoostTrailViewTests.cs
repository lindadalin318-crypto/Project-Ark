using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class BoostTrailViewTests
    {
        [Test]
        public void ResetState_StopsAndClearsAresSustainParticles()
        {
            var root = new GameObject("BoostTrailViewTestRoot");
            var view = root.AddComponent<BoostTrailView>();
            var particleSystem = CreateParticleSystem(root.transform, "AresTrail_Test");

            particleSystem.Play();
            particleSystem.Emit(3);

            SetPrivateField(view, "_aresSustainParticles", new[] { particleSystem });

            view.ResetState();

            Assert.That(particleSystem.isPlaying, Is.False);
            Assert.That(particleSystem.particleCount, Is.EqualTo(0));

            Object.DestroyImmediate(root);
        }

        private static ParticleSystem CreateParticleSystem(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var particleSystem = go.AddComponent<ParticleSystem>();

            var main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = true;

            return particleSystem;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
