using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Combat.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SnapshotBuilder"/>.
    /// Tests prism modifier math: Add average distribution, Multiply full application,
    /// and projectile hard cap overflow.
    /// Uses runtime-created ScriptableObjects (no asset files needed).
    /// </summary>
    [TestFixture]
    public class SnapshotBuilderTests
    {
        // ──────────────────── Helpers ────────────────────

        private static StarCoreSO CreateCore(float damage = 10f, float fireRate = 5f,
                                              float speed = 20f, float lifetime = 2f,
                                              int slotSize = 1)
        {
            var core = ScriptableObject.CreateInstance<StarCoreSO>();
            // Use serialized field reflection to set values (since fields are private/serialized)
            SetPrivateField(core, "_baseDamage", damage);
            SetPrivateField(core, "_fireRate", fireRate);
            SetPrivateField(core, "_projectileSpeed", speed);
            SetPrivateField(core, "_lifetime", lifetime);
            SetPrivateField(core, "_knockback", 1f);
            SetPrivateField(core, "_recoilForce", 0.5f);
            SetPrivateField(core, "_spread", 0f);
            SetPrivateField(core, "_family", CoreFamily.Matter);
            SetPrivateField(core, "_slotSize", slotSize);
            SetPrivateField(core, "_heatCost", 5f);
            return core;
        }

        private static PrismSO CreatePrism(StatModifier[] modifiers, int slotSize = 1)
        {
            var prism = ScriptableObject.CreateInstance<PrismSO>();
            SetPrivateField(prism, "_statModifiers", modifiers);
            SetPrivateField(prism, "_slotSize", slotSize);
            SetPrivateField(prism, "_heatCost", 0f);
            SetPrivateField(prism, "_family", PrismFamily.Rheology);
            return prism;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        // ──────────────────── Tests ────────────────────

        [Test]
        public void SingleCore_NoPrism_BaseValues()
        {
            var core = CreateCore(damage: 25f, fireRate: 10f);
            var cores = new List<StarCoreSO> { core };
            var prisms = new List<PrismSO>();

            var snapshot = SnapshotBuilder.Build(cores, prisms);

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, snapshot.CoreSnapshots.Count);
            Assert.AreEqual(25f, snapshot.CoreSnapshots[0].Damage, 0.001f);
        }

        [Test]
        public void NoCores_ReturnsNull()
        {
            var cores = new List<StarCoreSO>();
            var prisms = new List<PrismSO>();

            var snapshot = SnapshotBuilder.Build(cores, prisms);

            Assert.IsNull(snapshot);
        }

        [Test]
        public void AddModifier_DistributedEvenlyAcrossCores()
        {
            var core1 = CreateCore(damage: 10f);
            var core2 = CreateCore(damage: 10f);
            var cores = new List<StarCoreSO> { core1, core2 };

            // +20 damage, spread across 2 cores = +10 each
            var prism = CreatePrism(new[]
            {
                new StatModifier { Stat = WeaponStatType.Damage, Operation = ModifierOperation.Add, Value = 20f }
            });
            var prisms = new List<PrismSO> { prism };

            var snapshot = SnapshotBuilder.Build(cores, prisms);

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(2, snapshot.CoreSnapshots.Count);
            // Each core: base 10 + (20/2) = 20
            Assert.AreEqual(20f, snapshot.CoreSnapshots[0].Damage, 0.001f);
            Assert.AreEqual(20f, snapshot.CoreSnapshots[1].Damage, 0.001f);
        }

        [Test]
        public void MultiplyModifier_AppliedFully()
        {
            var core = CreateCore(damage: 10f);
            var cores = new List<StarCoreSO> { core };

            // x2 damage
            var prism = CreatePrism(new[]
            {
                new StatModifier { Stat = WeaponStatType.Damage, Operation = ModifierOperation.Multiply, Value = 2f }
            });
            var prisms = new List<PrismSO> { prism };

            var snapshot = SnapshotBuilder.Build(cores, prisms);

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(20f, snapshot.CoreSnapshots[0].Damage, 0.001f);
        }

        [Test]
        public void ProjectileCountCap_ExcessConvertedToBonus()
        {
            // Create a core with +25 projectiles from prisms (above the 20 cap)
            var core = CreateCore(damage: 10f);
            var cores = new List<StarCoreSO> { core };

            var prism = CreatePrism(new[]
            {
                new StatModifier { Stat = WeaponStatType.ProjectileCount, Operation = ModifierOperation.Add, Value = 25f }
            });
            var prisms = new List<PrismSO> { prism };

            var snapshot = SnapshotBuilder.Build(cores, prisms);

            Assert.IsNotNull(snapshot);
            // Total projectiles should be capped at MAX_PROJECTILES_PER_FIRE (20)
            Assert.LessOrEqual(snapshot.TotalProjectileCount, SnapshotBuilder.MAX_PROJECTILES_PER_FIRE);
            // Excess bonus should be > 0
            Assert.Greater(snapshot.ExcessDamageBonus, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up runtime-created ScriptableObjects
            // (they persist in memory until explicitly destroyed in tests)
        }
    }
}
