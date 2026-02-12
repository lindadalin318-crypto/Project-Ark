using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Combat.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SlotLayer{T}"/>.
    /// Tests equip/unequip with SlotSize 1/2/3, overflow detection, and clear.
    /// </summary>
    [TestFixture]
    public class SlotLayerTests
    {
        private static StarCoreSO CreateCore(int slotSize)
        {
            var core = ScriptableObject.CreateInstance<StarCoreSO>();
            SetPrivateField(core, "_slotSize", slotSize);
            SetPrivateField(core, "_baseDamage", 10f);
            SetPrivateField(core, "_fireRate", 5f);
            SetPrivateField(core, "_projectileSpeed", 20f);
            SetPrivateField(core, "_lifetime", 2f);
            SetPrivateField(core, "_knockback", 1f);
            SetPrivateField(core, "_recoilForce", 0.5f);
            SetPrivateField(core, "_spread", 0f);
            SetPrivateField(core, "_family", CoreFamily.Matter);
            SetPrivateField(core, "_heatCost", 0f);
            return core;
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

        [Test]
        public void EmptyLayer_IsEmpty()
        {
            var layer = new SlotLayer<StarCoreSO>();

            Assert.IsTrue(layer.IsEmpty);
            Assert.AreEqual(0, layer.UsedSpace);
            Assert.AreEqual(SlotLayer<StarCoreSO>.GRID_SIZE, layer.FreeSpace);
        }

        [Test]
        public void EquipSize1_TakesOneSlot()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var core = CreateCore(1);

            bool result = layer.TryEquip(core);

            Assert.IsTrue(result);
            Assert.AreEqual(1, layer.Items.Count);
            Assert.AreEqual(1, layer.UsedSpace);
            Assert.AreEqual(2, layer.FreeSpace);
        }

        [Test]
        public void EquipThreeSize1_FillsGrid()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);
            var c3 = CreateCore(1);

            Assert.IsTrue(layer.TryEquip(c1));
            Assert.IsTrue(layer.TryEquip(c2));
            Assert.IsTrue(layer.TryEquip(c3));
            Assert.AreEqual(3, layer.Items.Count);
            Assert.AreEqual(0, layer.FreeSpace);
        }

        [Test]
        public void EquipSize2AndSize1_Fits()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var big = CreateCore(2);
            var small = CreateCore(1);

            Assert.IsTrue(layer.TryEquip(big));
            Assert.IsTrue(layer.TryEquip(small));
            Assert.AreEqual(2, layer.Items.Count);
            Assert.AreEqual(0, layer.FreeSpace);
        }

        [Test]
        public void EquipSize3_FillsEntireGrid()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var mega = CreateCore(3);

            Assert.IsTrue(layer.TryEquip(mega));
            Assert.AreEqual(1, layer.Items.Count);
            Assert.AreEqual(0, layer.FreeSpace);
        }

        [Test]
        public void Overflow_ReturnsFalse()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var big = CreateCore(2);
            var alsoMedium = CreateCore(2);

            Assert.IsTrue(layer.TryEquip(big));
            Assert.IsFalse(layer.TryEquip(alsoMedium));
            Assert.AreEqual(1, layer.Items.Count);
        }

        [Test]
        public void Unequip_FreesSpace()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);

            layer.TryEquip(c1);
            layer.TryEquip(c2);

            bool removed = layer.Unequip(c1);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, layer.Items.Count);
            Assert.AreEqual(2, layer.FreeSpace);
        }

        [Test]
        public void Unequip_NotEquipped_ReturnsFalse()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var notEquipped = CreateCore(1);

            Assert.IsFalse(layer.Unequip(notEquipped));
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            var layer = new SlotLayer<StarCoreSO>();
            layer.TryEquip(CreateCore(1));
            layer.TryEquip(CreateCore(2));

            layer.Clear();

            Assert.IsTrue(layer.IsEmpty);
            Assert.AreEqual(0, layer.UsedSpace);
            Assert.AreEqual(3, layer.FreeSpace);
        }

        [Test]
        public void EquipAfterUnequip_ContiguousCheck()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);
            var c3 = CreateCore(1);

            layer.TryEquip(c1); // slot 0
            layer.TryEquip(c2); // slot 1
            layer.TryEquip(c3); // slot 2

            layer.Unequip(c2); // free slot 1

            // Can fit a size-1 item in the freed slot
            var c4 = CreateCore(1);
            Assert.IsTrue(layer.TryEquip(c4));
        }
    }
}
