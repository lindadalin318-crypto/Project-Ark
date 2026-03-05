using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SlotLayer{T}"/>.
    /// Tests equip/unequip with SlotSize 1/2, overflow detection, clear,
    /// and the progressive column-unlock system.
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

        // =====================================================================
        // Initial state
        // =====================================================================

        [Test]
        public void InitialCapacity_Is2_With1Col()
        {
            var layer = new SlotLayer<StarCoreSO>(); // default initialCols = 1

            Assert.AreEqual(1, layer.Cols);
            Assert.AreEqual(SlotLayer<StarCoreSO>.FIXED_ROWS, layer.Rows);
            Assert.AreEqual(2, layer.Capacity); // 1 col × 2 rows = 2 cells
        }

        [Test]
        public void EmptyLayer_IsEmpty()
        {
            var layer = new SlotLayer<StarCoreSO>();

            Assert.IsTrue(layer.IsEmpty);
            Assert.AreEqual(0, layer.UsedSpace);
            Assert.AreEqual(layer.Capacity, layer.FreeSpace);
        }

        // =====================================================================
        // Basic equip / unequip (1-col layer = 2 cells)
        // =====================================================================

        [Test]
        public void EquipSize1_TakesOneSlot()
        {
            var layer = new SlotLayer<StarCoreSO>();
            var core = CreateCore(1);

            bool result = layer.TryEquip(core);

            Assert.IsTrue(result);
            Assert.AreEqual(1, layer.Items.Count);
            Assert.AreEqual(1, layer.UsedSpace);
            Assert.AreEqual(layer.Capacity - 1, layer.FreeSpace);
        }

        [Test]
        public void EquipTwoSize1_FillsInitialGrid()
        {
            var layer = new SlotLayer<StarCoreSO>(); // 1 col × 2 rows = 2 cells
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);

            Assert.IsTrue(layer.TryEquip(c1));
            Assert.IsTrue(layer.TryEquip(c2));
            Assert.AreEqual(2, layer.Items.Count);
            Assert.AreEqual(0, layer.FreeSpace);
        }

        [Test]
        public void Overflow_ReturnsFalse()
        {
            var layer = new SlotLayer<StarCoreSO>(); // 2 cells total
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);
            var c3 = CreateCore(1); // no room

            Assert.IsTrue(layer.TryEquip(c1));
            Assert.IsTrue(layer.TryEquip(c2));
            Assert.IsFalse(layer.TryEquip(c3));
            Assert.AreEqual(2, layer.Items.Count);
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
            Assert.AreEqual(layer.Capacity - 1, layer.FreeSpace);
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
            layer.TryEquip(CreateCore(1));

            layer.Clear();

            Assert.IsTrue(layer.IsEmpty);
            Assert.AreEqual(0, layer.UsedSpace);
            Assert.AreEqual(layer.Capacity, layer.FreeSpace);
        }

        [Test]
        public void EquipAfterUnequip_ContiguousCheck()
        {
            // Use a 2-col layer (4 cells) so we can test slot reuse
            var layer = new SlotLayer<StarCoreSO>(initialCols: 2);
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);
            var c3 = CreateCore(1);

            layer.TryEquip(c1);
            layer.TryEquip(c2);
            layer.TryEquip(c3);

            layer.Unequip(c2); // free one slot

            var c4 = CreateCore(1);
            Assert.IsTrue(layer.TryEquip(c4));
        }

        // =====================================================================
        // Progressive column unlock
        // =====================================================================

        [Test]
        public void TryUnlockColumn_IncreasesCapacityBy2()
        {
            var layer = new SlotLayer<StarCoreSO>(); // starts at 1 col = 2 cells

            bool unlocked = layer.TryUnlockColumn();

            Assert.IsTrue(unlocked);
            Assert.AreEqual(2, layer.Cols);
            Assert.AreEqual(4, layer.Capacity); // 2 cols × 2 rows = 4 cells
        }

        [Test]
        public void TryUnlockColumn_AtMaxCols_ReturnsFalse()
        {
            var layer = new SlotLayer<StarCoreSO>(initialCols: SlotLayer<StarCoreSO>.MAX_COLS);

            bool unlocked = layer.TryUnlockColumn();

            Assert.IsFalse(unlocked);
            Assert.AreEqual(SlotLayer<StarCoreSO>.MAX_COLS, layer.Cols);
            Assert.AreEqual(SlotLayer<StarCoreSO>.MAX_COLS * SlotLayer<StarCoreSO>.FIXED_ROWS, layer.Capacity);
        }

        [Test]
        public void TryUnlockColumn_MaxCapacity_Is8()
        {
            var layer = new SlotLayer<StarCoreSO>(initialCols: 1);

            // Unlock all the way to MAX_COLS (4)
            while (layer.TryUnlockColumn()) { }

            Assert.AreEqual(SlotLayer<StarCoreSO>.MAX_COLS, layer.Cols);
            Assert.AreEqual(8, layer.Capacity); // 4 cols × 2 rows = 8 cells
        }

        [Test]
        public void UnlockColumn_NewCellsAreEmpty_OldItemsUnaffected()
        {
            var layer = new SlotLayer<StarCoreSO>(); // 1 col = 2 cells
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);

            layer.TryEquip(c1);
            layer.TryEquip(c2);
            Assert.AreEqual(0, layer.FreeSpace); // full

            // Unlock a new column
            layer.TryUnlockColumn();

            // Old items still present
            Assert.AreEqual(2, layer.Items.Count);
            Assert.AreEqual(2, layer.UsedSpace);
            // New column adds 2 free cells
            Assert.AreEqual(2, layer.FreeSpace);

            // Can now equip into the new column
            var c3 = CreateCore(1);
            Assert.IsTrue(layer.TryEquip(c3));
        }

        [Test]
        public void InitialCols_Constructor_SetsCorrectCapacity()
        {
            var layer2 = new SlotLayer<StarCoreSO>(initialCols: 2);
            var layer4 = new SlotLayer<StarCoreSO>(initialCols: 4);

            Assert.AreEqual(4, layer2.Capacity);
            Assert.AreEqual(8, layer4.Capacity);
        }

        [Test]
        public void FreeSpace_EqualsCapacityMinusUsedSpace()
        {
            var layer = new SlotLayer<StarCoreSO>(initialCols: 2); // 4 cells
            var c1 = CreateCore(1);
            var c2 = CreateCore(1);

            layer.TryEquip(c1);
            layer.TryEquip(c2);

            Assert.AreEqual(layer.Capacity - layer.UsedSpace, layer.FreeSpace);
            Assert.AreEqual(2, layer.FreeSpace);
        }
    }
}
