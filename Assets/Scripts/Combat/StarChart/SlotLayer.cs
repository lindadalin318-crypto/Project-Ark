using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// A 2D grid layer (Rows × Cols) that holds Star Chart items.
    /// Each item occupies one or more cells according to its <see cref="ItemShape"/>.
    /// Both columns and rows can be unlocked progressively.
    /// Initial state: Cols=2, Rows=1 (2 cells, horizontal layout).
    /// Maximum state: Cols=4, Rows=4 (16 cells).
    /// Pure data class — not a MonoBehaviour.
    /// </summary>
    /// <typeparam name="T">StarCoreSO or PrismSO</typeparam>
    public class SlotLayer<T> where T : StarChartItemSO
    {
        // =====================================================================
        // Constants
        // =====================================================================

        /// <summary> Maximum unlockable column count. </summary>
        public const int MAX_COLS = 4;

        /// <summary> Maximum unlockable row count. </summary>
        public const int MAX_ROWS = 4;

        // =====================================================================
        // Dynamic capacity
        // =====================================================================

        /// <summary> Current number of unlocked rows (1–4). </summary>
        public int Rows { get; private set; }

        /// <summary> Current number of unlocked columns (1–4). </summary>
        public int Cols { get; private set; }

        /// <summary> Total number of cells (Rows × Cols). </summary>
        public int Capacity => Rows * Cols;

        // grid[row, col] — null means empty; sized to MAX_ROWS × MAX_COLS to avoid reallocation
        private readonly T[,] _grid = new T[MAX_ROWS, MAX_COLS];

        // Anchor positions for each placed item (col, row of anchor cell)
        private readonly Dictionary<T, Vector2Int> _anchors = new();

        // =====================================================================
        // Read-only views
        // =====================================================================

        // Ordered list kept in sync with _anchors for index-based access
        private readonly List<T> _itemList = new();

        /// <summary> All currently equipped items (no duplicates), in insertion order. </summary>
        public IReadOnlyList<T> Items => _itemList;

        /// <summary> True if no items are equipped. </summary>
        public bool IsEmpty => _anchors.Count == 0;

        /// <summary> Number of free cells (not occupied by any item). </summary>
        public int FreeCells
        {
            get
            {
                int free = 0;
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                        if (_grid[r, c] == null) free++;
                return free;
            }
        }

        /// <summary>
        /// Total grid units currently in use — sum of actual occupied cell counts per shape.
        /// Authority: <see cref="ItemShapeHelper.GetCells"/> (Shape Contract C1).
        /// Does NOT read item.SlotSize — that is a legacy 1D field kept for backward compat only.
        /// </summary>
        public int UsedSpace
        {
            get
            {
                int used = 0;
                foreach (var item in _itemList)
                    used += ItemShapeHelper.GetCells(item.Shape).Count;
                return used;
            }
        }

        /// <summary> Remaining free grid units (Capacity - UsedSpace). </summary>
        public int FreeSpace => Capacity - UsedSpace;

        // =====================================================================
        // Constructor
        // =====================================================================

        /// <summary>
        /// Creates a new SlotLayer with the given initial column and row counts.
        /// Default: Cols=2, Rows=1 (horizontal 2-cell layout).
        /// </summary>
        /// <param name="initialCols">Starting column count (default 2). Clamped to [1, MAX_COLS].</param>
        /// <param name="initialRows">Starting row count (default 1). Clamped to [1, MAX_ROWS].</param>
        public SlotLayer(int initialCols = 2, int initialRows = 1)
        {
            Cols = Mathf.Clamp(initialCols, 1, MAX_COLS);
            Rows = Mathf.Clamp(initialRows, 1, MAX_ROWS);
        }

        // =====================================================================
        // Capacity unlock
        // =====================================================================

        /// <summary>
        /// Attempts to unlock one additional column (expand right).
        /// Returns true if successful; false if already at MAX_COLS.
        /// Existing items are unaffected — the new column starts empty.
        /// </summary>
        public bool TryUnlockColumn()
        {
            if (Cols >= MAX_COLS) return false;
            Cols++;
            return true;
        }

        /// <summary>
        /// Attempts to unlock one additional row (expand downward).
        /// Returns true if successful; false if already at MAX_ROWS.
        /// Existing items are unaffected — the new row starts empty.
        /// </summary>
        public bool TryUnlockRow()
        {
            if (Rows >= MAX_ROWS) return false;
            Rows++;
            return true;
        }

        // =====================================================================
        // Query API
        // =====================================================================

        /// <summary>
        /// Returns the item at grid position (col, row), or null if empty.
        /// </summary>
        public T GetAt(int col, int row)
        {
            if (!InBounds(col, row)) return null;
            return _grid[row, col];
        }

        /// <summary>
        /// Returns the anchor position of the given item, or (-1,-1) if not found.
        /// </summary>
        public Vector2Int GetAnchor(T item)
        {
            return _anchors.TryGetValue(item, out var anchor) ? anchor : new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Checks whether the given item can be placed at (anchorCol, anchorRow).
        /// Returns:
        ///   canPlace — true if placement is possible (either free or only evictable items block it).
        ///   evictList — items that would be displaced (empty if all cells are free).
        /// Returns canPlace=false if the shape goes out of bounds.
        /// </summary>
        public (bool canPlace, List<T> evictList) CanPlace(T item, int anchorCol, int anchorRow)
        {
            var shape = item.Shape;
            if (!ItemShapeHelper.FitsInGrid(shape, anchorCol, anchorRow, Cols, Rows))
                return (false, null);

            var evictSet = new HashSet<T>();
            foreach (var offset in ItemShapeHelper.GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                var occupant = _grid[r, c];
                if (occupant != null && !ReferenceEquals(occupant, item))
                    evictSet.Add(occupant);
            }

            return (true, new List<T>(evictSet));
        }

        // =====================================================================
        // Mutation API
        // =====================================================================

        /// <summary>
        /// Places the item at (anchorCol, anchorRow), evicting any blocking items first.
        /// Returns false if the shape goes out of bounds.
        /// Evicted items are added to <paramref name="evicted"/>.
        /// </summary>
        public bool TryPlace(T item, int anchorCol, int anchorRow, List<T> evicted = null)
        {
            var (canPlace, evictList) = CanPlace(item, anchorCol, anchorRow);
            if (!canPlace) return false;

            // Evict blocking items
            if (evictList != null)
            {
                foreach (var e in evictList)
                {
                    RemoveItem(e);
                    evicted?.Add(e);
                }
            }

            // Place item
            var shape = item.Shape;
            foreach (var offset in ItemShapeHelper.GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                _grid[r, c] = item;
            }
            _anchors[item] = new Vector2Int(anchorCol, anchorRow);
            if (!_itemList.Contains(item)) _itemList.Add(item);
            return true;
        }

        /// <summary>
        /// Removes the item at (col, row) from the grid.
        /// Returns the removed item, or null if the cell was empty.
        /// </summary>
        public T Remove(int col, int row)
        {
            var item = GetAt(col, row);
            if (item == null) return null;
            RemoveItem(item);
            return item;
        }

        /// <summary>
        /// Removes a specific item from the grid.
        /// Returns true if the item was found and removed.
        /// </summary>
        public bool RemoveItem(T item)
        {
            if (!_anchors.TryGetValue(item, out var anchor)) return false;

            foreach (var offset in ItemShapeHelper.GetCells(item.Shape))
            {
                int c = anchor.x + offset.x;
                int r = anchor.y + offset.y;
                if (InBounds(c, r) && ReferenceEquals(_grid[r, c], item))
                    _grid[r, c] = null;
            }
            _anchors.Remove(item);
            _itemList.Remove(item);
            return true;
        }

        /// <summary> Removes all items and clears the grid. </summary>
        public void Clear()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _grid[r, c] = null;
            _anchors.Clear();
            _itemList.Clear();
        }

        // =====================================================================
        // Legacy compatibility (1D API used by existing code)
        // =====================================================================

        /// <summary>
        /// Legacy: attempts to equip an item by finding the first available anchor position.
        /// Tries row 0 first, then row 1, scanning left to right.
        /// </summary>
        public bool TryEquip(T item)
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var (canPlace, evictList) = CanPlace(item, c, r);
                    if (canPlace && (evictList == null || evictList.Count == 0))
                    {
                        TryPlace(item, c, r);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Legacy: removes an item from the layer.
        /// </summary>
        public bool Unequip(T item) => RemoveItem(item);

        // =====================================================================
        // Helpers
        // =====================================================================

        private bool InBounds(int col, int row)
            => col >= 0 && col < Cols && row >= 0 && row < Rows;
    }
}