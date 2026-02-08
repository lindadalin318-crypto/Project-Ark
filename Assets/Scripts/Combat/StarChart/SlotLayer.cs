using System.Collections.Generic;

namespace ProjectArk.Combat
{
    /// <summary>
    /// A fixed-capacity (3 grid units) layer that holds Star Chart items.
    /// Manages slot occupancy and prevents size conflicts.
    /// Pure data class — not a MonoBehaviour.
    /// </summary>
    /// <typeparam name="T">StarCoreSO or PrismSO</typeparam>
    public class SlotLayer<T> where T : StarChartItemSO
    {
        public const int GRID_SIZE = 3;

        private readonly List<T> _items = new();
        private readonly bool[] _occupied = new bool[GRID_SIZE];

        /// <summary> Read-only view of equipped items. </summary>
        public IReadOnlyList<T> Items => _items;

        /// <summary> Total grid units currently in use. </summary>
        public int UsedSpace
        {
            get
            {
                int used = 0;
                for (int i = 0; i < _items.Count; i++)
                    used += _items[i].SlotSize;
                return used;
            }
        }

        /// <summary> Remaining free grid units. </summary>
        public int FreeSpace => GRID_SIZE - UsedSpace;

        /// <summary> True if no items are equipped. </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Attempts to equip an item. Returns false if insufficient contiguous space.
        /// </summary>
        public bool TryEquip(T item)
        {
            int size = item.SlotSize;
            if (size > FreeSpace) return false;

            // 查找第一段连续空闲格子
            int startIndex = FindContiguousFreeSlot(size);
            if (startIndex < 0) return false;

            // 标记占用
            for (int i = startIndex; i < startIndex + size; i++)
                _occupied[i] = true;

            _items.Add(item);
            return true;
        }

        /// <summary>
        /// Removes an item from the layer, freeing its grid units.
        /// </summary>
        public bool Unequip(T item)
        {
            int index = _items.IndexOf(item);
            if (index < 0) return false;

            // 计算该物品占用的起始格子
            int startSlot = 0;
            for (int i = 0; i < index; i++)
                startSlot += _items[i].SlotSize;

            int size = item.SlotSize;
            for (int i = startSlot; i < startSlot + size; i++)
                _occupied[i] = false;

            _items.RemoveAt(index);
            return true;
        }

        /// <summary> Removes all items and clears occupancy. </summary>
        public void Clear()
        {
            _items.Clear();
            for (int i = 0; i < GRID_SIZE; i++)
                _occupied[i] = false;
        }

        /// <summary>
        /// Finds the first contiguous run of free slots with the required size.
        /// Returns the starting index, or -1 if not found.
        /// </summary>
        private int FindContiguousFreeSlot(int size)
        {
            int consecutive = 0;
            for (int i = 0; i < GRID_SIZE; i++)
            {
                if (!_occupied[i])
                {
                    consecutive++;
                    if (consecutive >= size)
                        return i - size + 1;
                }
                else
                {
                    consecutive = 0;
                }
            }
            return -1;
        }
    }
}
