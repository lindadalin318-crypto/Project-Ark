using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="DamageCalculator"/>.
    /// Covers resistance application, block reduction, edge cases.
    /// </summary>
    [TestFixture]
    public class DamageCalculatorTests
    {
        // ──────────────────── Test Doubles ────────────────────

        private class MockTarget : IDamageable
        {
            public bool IsAlive => true;
            public float LastDamage;
            public void TakeDamage(DamagePayload payload) => LastDamage = payload.BaseDamage;
            public void TakeDamage(float d, Vector2 dir, float f) => LastDamage = d;
        }

        private class ResistantTarget : IDamageable, IResistant
        {
            public float PhysicalResist;
            public float FireResist;
            public bool IsAlive => true;
            public void TakeDamage(DamagePayload payload) { }
            public void TakeDamage(float d, Vector2 dir, float f) { }

            public float GetResistance(DamageType type) => type switch
            {
                DamageType.Physical => PhysicalResist,
                DamageType.Fire => FireResist,
                _ => 0f
            };
        }

        private class BlockingTarget : IDamageable, IBlockable
        {
            public bool IsBlocking { get; set; }
            public float BlockDamageReduction { get; set; }
            public bool IsAlive => true;
            public void TakeDamage(DamagePayload payload) { }
            public void TakeDamage(float d, Vector2 dir, float f) { }
        }

        private class FullTarget : IDamageable, IResistant, IBlockable
        {
            public float Resist;
            public bool IsBlocking { get; set; }
            public float BlockDamageReduction { get; set; }
            public bool IsAlive => true;
            public void TakeDamage(DamagePayload payload) { }
            public void TakeDamage(float d, Vector2 dir, float f) { }
            public float GetResistance(DamageType type) => Resist;
        }

        // ──────────────────── Tests ────────────────────

        [Test]
        public void NoModifiers_ReturnsBaseDamage()
        {
            var payload = new DamagePayload(100f, Vector2.zero, 0f);
            var target = new MockTarget();

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(100f, result, 0.001f);
        }

        [Test]
        public void PhysicalResistance_ReducesDamage()
        {
            var payload = new DamagePayload(100f, DamageType.Physical, Vector2.zero, 0f);
            var target = new ResistantTarget { PhysicalResist = 0.3f };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(70f, result, 0.001f);
        }

        [Test]
        public void FullResistance_ZeroDamage()
        {
            var payload = new DamagePayload(100f, DamageType.Fire, Vector2.zero, 0f);
            var target = new ResistantTarget { FireResist = 1f };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void ZeroResistance_FullDamage()
        {
            var payload = new DamagePayload(50f, DamageType.Lightning, Vector2.zero, 0f);
            var target = new ResistantTarget { PhysicalResist = 0f, FireResist = 0f };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(50f, result, 0.001f);
        }

        [Test]
        public void BlockReduction_WhenBlocking()
        {
            var payload = new DamagePayload(100f, Vector2.zero, 0f);
            var target = new BlockingTarget { IsBlocking = true, BlockDamageReduction = 0.7f };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(30f, result, 0.001f);
        }

        [Test]
        public void BlockReduction_WhenNotBlocking_NoDamageReduction()
        {
            var payload = new DamagePayload(100f, Vector2.zero, 0f);
            var target = new BlockingTarget { IsBlocking = false, BlockDamageReduction = 0.7f };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(100f, result, 0.001f);
        }

        [Test]
        public void ResistAndBlock_StackMultiplicatively()
        {
            var payload = new DamagePayload(100f, DamageType.Physical, Vector2.zero, 0f);
            var target = new FullTarget
            {
                Resist = 0.5f,              // 50% resist → 50 damage
                IsBlocking = true,
                BlockDamageReduction = 0.6f  // 60% block → 50 * 0.4 = 20
            };

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(20f, result, 0.001f);
        }

        [Test]
        public void ZeroBaseDamage_ReturnsZero()
        {
            var payload = new DamagePayload(0f, Vector2.zero, 0f);
            var target = new MockTarget();

            float result = DamageCalculator.Calculate(payload, target);

            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void NegativeResistance_ClampedToZero()
        {
            // If resistance somehow goes negative, it should be clamped
            var payload = new DamagePayload(100f, DamageType.Physical, Vector2.zero, 0f);
            var target = new ResistantTarget { PhysicalResist = -0.5f };

            float result = DamageCalculator.Calculate(payload, target);

            // Clamp01(-0.5) = 0, so 1 - 0 = 1, damage = 100
            Assert.AreEqual(100f, result, 0.001f);
        }

        [Test]
        public void OverOneResistance_ClampedToOne()
        {
            var payload = new DamagePayload(100f, DamageType.Fire, Vector2.zero, 0f);
            var target = new ResistantTarget { FireResist = 1.5f };

            float result = DamageCalculator.Calculate(payload, target);

            // Clamp01(1.5) = 1, so 1 - 1 = 0, damage = 0
            Assert.AreEqual(0f, result, 0.001f);
        }
    }
}
