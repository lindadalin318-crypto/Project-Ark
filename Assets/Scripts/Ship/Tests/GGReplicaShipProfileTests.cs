using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Ship.Tests
{
    [TestFixture]
    public class GGReplicaShipProfileTests
    {
        [Test]
        public void VisualProfile_TryGetPack_ReturnsConfiguredPackByState()
        {
            var profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>();
            var normalPack = new GGReplicaSpritePack
            {
                State = GGReplicaVisualState.Normal,
                FadeDuration = 0.2f,
                SpritesOffset = new Vector3(1f, 2f, 0f)
            };
            var boostPack = new GGReplicaSpritePack
            {
                State = GGReplicaVisualState.Boost,
                FadeDuration = 0.1f,
                SpritesOffset = new Vector3(3f, 4f, 0f)
            };
            SetPrivateField(profile, "_spritePacks", new[] { normalPack, boostPack });

            bool found = profile.TryGetPack(GGReplicaVisualState.Boost, out var pack);

            Assert.That(found, Is.True);
            Assert.That(pack, Is.SameAs(boostPack));
            Assert.That(pack.FadeDuration, Is.EqualTo(0.1f));
            Assert.That(pack.SpritesOffset, Is.EqualTo(new Vector3(3f, 4f, 0f)));

            UnityEngine.Object.DestroyImmediate(profile);
        }

        [Test]
        public void VisualProfile_TryGetPack_ReturnsFalseWhenMissing()
        {
            var profile = ScriptableObject.CreateInstance<GGReplicaShipVisualProfileSO>();
            SetPrivateField(profile, "_spritePacks", Array.Empty<GGReplicaSpritePack>());

            bool found = profile.TryGetPack(GGReplicaVisualState.Fire, out var pack);

            Assert.That(found, Is.False);
            Assert.That(pack, Is.Null);

            UnityEngine.Object.DestroyImmediate(profile);
        }

        [Test]
        public void FeelProfile_DefaultValues_MatchInitialGGReplicaTuning()
        {
            var profile = ScriptableObject.CreateInstance<GGReplicaShipFeelProfileSO>();

            Assert.That(profile.DodgeForce, Is.EqualTo(13f));
            Assert.That(profile.DodgeForceAfterDodge, Is.EqualTo(6f));
            Assert.That(profile.DodgeInvulnerabilityTime, Is.EqualTo(0.15f));
            Assert.That(profile.DodgeCacheTime, Is.EqualTo(0.12f));
            Assert.That(profile.DodgeRechargeTime, Is.EqualTo(0.5f));
            Assert.That(profile.MaxDodgeCharges, Is.EqualTo(1));
            Assert.That(profile.BoostSpeedMultiplier, Is.EqualTo(1.2f));
            Assert.That(profile.AfterBoostDrag, Is.EqualTo(2.5f));
            Assert.That(profile.BoostStartImpulse, Is.EqualTo(4f));
            Assert.That(profile.BoostIgniteDuration, Is.EqualTo(0.08f));

            UnityEngine.Object.DestroyImmediate(profile);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
