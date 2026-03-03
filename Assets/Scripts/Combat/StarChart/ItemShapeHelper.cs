using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Static helper for <see cref="ItemShape"/> geometry queries.
    /// Grid coordinate system: col increases right, row increases down (0 = top row, 1 = bottom row).
    /// All offsets are relative to the anchor cell (col=0, row=0).
    /// </summary>
    public static class ItemShapeHelper
    {
        // Pre-allocated cell offset arrays per shape (col, row)
        private static readonly Vector2Int[] Cells1x1  = { new(0, 0) };
        private static readonly Vector2Int[] Cells1x2H = { new(0, 0), new(1, 0) };
        private static readonly Vector2Int[] Cells2x1V = { new(0, 0), new(0, 1) };
        private static readonly Vector2Int[] CellsL    = { new(0, 0), new(1, 0), new(0, 1) };
        private static readonly Vector2Int[] Cells2x2  = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };

        /// <summary>
        /// Returns the list of (col, row) offsets occupied by the given shape,
        /// relative to the anchor cell at (0, 0).
        /// </summary>
        public static IReadOnlyList<Vector2Int> GetCells(ItemShape shape)
        {
            return shape switch
            {
                ItemShape.Shape1x1  => Cells1x1,
                ItemShape.Shape1x2H => Cells1x2H,
                ItemShape.Shape2x1V => Cells2x1V,
                ItemShape.ShapeL    => CellsL,
                ItemShape.Shape2x2  => Cells2x2,
                _                   => Cells1x1
            };
        }

        /// <summary>
        /// Returns the bounding box (cols wide, rows tall) of the given shape.
        /// </summary>
        public static Vector2Int GetBounds(ItemShape shape)
        {
            return shape switch
            {
                ItemShape.Shape1x1  => new Vector2Int(1, 1),
                ItemShape.Shape1x2H => new Vector2Int(2, 1),
                ItemShape.Shape2x1V => new Vector2Int(1, 2),
                ItemShape.ShapeL    => new Vector2Int(2, 2),
                ItemShape.Shape2x2  => new Vector2Int(2, 2),
                _                   => new Vector2Int(1, 1)
            };
        }

        /// <summary>
        /// Returns the absolute (col, row) positions occupied by a shape placed at the given anchor.
        /// Allocates a new List each call — use the overload with an output parameter on hot paths.
        /// </summary>
        public static List<Vector2Int> GetAbsoluteCells(ItemShape shape, int anchorCol, int anchorRow)
        {
            var offsets = GetCells(shape);
            var result = new List<Vector2Int>(offsets.Count);
            foreach (var offset in offsets)
                result.Add(new Vector2Int(anchorCol + offset.x, anchorRow + offset.y));
            return result;
        }

        /// <summary>
        /// GC-friendly overload: fills the provided <paramref name="result"/> list in-place.
        /// Callers on hot paths (e.g. per-frame drag hover) should cache and reuse the list.
        /// </summary>
        public static void GetAbsoluteCells(ItemShape shape, int anchorCol, int anchorRow, List<Vector2Int> result)
        {
            result.Clear();
            foreach (var offset in GetCells(shape))
                result.Add(new Vector2Int(anchorCol + offset.x, anchorRow + offset.y));
        }

        /// <summary>
        /// Returns true if all cells of the shape placed at the anchor are within the grid bounds.
        /// </summary>
        public static bool FitsInGrid(ItemShape shape, int anchorCol, int anchorRow, int gridCols, int gridRows)
        {
            foreach (var offset in GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                if (c < 0 || c >= gridCols || r < 0 || r >= gridRows)
                    return false;
            }
            return true;
        }
    }
}
