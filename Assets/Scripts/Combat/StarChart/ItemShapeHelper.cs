using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// C1 — Single source of truth for all shape geometry queries.
    ///
    /// Shape Contract (C1-C4):
    ///   C1 Single Truth  — <see cref="GetCells"/> is the only authoritative cell list.
    ///                       <see cref="GetBounds"/> is for bounding-box sizing only.
    ///                       No module may infer occupancy from slotSize or area.
    ///   C2 Consistent    — Inventory packing, Track placement, Drop preview, and Ghost
    ///                       all use GetCells() for occupancy.
    ///   C3 Visual Holes  — Only active cells are colored; empty bounding-box positions
    ///                       stay transparent in every UI state.
    ///   C4 Extensible    — To add a new shape: add enum value + entry in GetCells() and
    ///                       GetBounds(). No other code changes required.
    ///
    /// Grid coordinate system: col increases right, row increases down (0 = top row).
    /// All offsets are relative to the anchor cell (col=0, row=0).
    /// </summary>
    public static class ItemShapeHelper
    {
        // Pre-allocated cell offset arrays per shape (col, row)
        private static readonly Vector2Int[] Cells1x1      = { new(0, 0) };
        private static readonly Vector2Int[] Cells1x2H     = { new(0, 0), new(1, 0) };
        private static readonly Vector2Int[] Cells2x1V     = { new(0, 0), new(0, 1) };
        private static readonly Vector2Int[] CellsL        = { new(0, 0), new(1, 0), new(0, 1) };
        private static readonly Vector2Int[] CellsLMirror  = { new(0, 0), new(0, 1), new(1, 1) };
        private static readonly Vector2Int[] Cells2x2      = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };

        /// <summary>
        /// C1: Returns the list of (col, row) offsets occupied by the given shape,
        /// relative to the anchor cell at (0, 0).
        /// This is the SINGLE SOURCE OF TRUTH for all occupancy queries.
        /// </summary>
        public static IReadOnlyList<Vector2Int> GetCells(ItemShape shape)
        {
            return shape switch
            {
                ItemShape.Shape1x1     => Cells1x1,
                ItemShape.Shape1x2H    => Cells1x2H,
                ItemShape.Shape2x1V    => Cells2x1V,
                ItemShape.ShapeL       => CellsL,
                ItemShape.ShapeLMirror => CellsLMirror,
                ItemShape.Shape2x2     => Cells2x2,
                // C4: Fallback for unknown shapes — logs a warning in development builds.
                // When adding a new ItemShape value, add a case here AND in GetBounds().
                _ => FallbackCells(shape)
            };
        }

        /// <summary>
        /// C4: Fallback for unregistered shapes. Returns 1x1 and warns the developer.
        /// This guard ensures new shapes added to the enum are immediately surfaced.
        /// </summary>
        private static IReadOnlyList<Vector2Int> FallbackCells(ItemShape shape)
        {
            Debug.LogWarning(
                $"[ItemShapeHelper] C4 Guard: Shape '{shape}' has no registered cell layout. " +
                "Add it to GetCells() and GetBounds() in ItemShapeHelper.cs. Falling back to 1×1.");
            return Cells1x1;
        }

        /// <summary>
        /// Returns the bounding box (cols wide, rows tall) of the given shape.
        /// Use for sizing visual containers only — NOT for occupancy checks (use GetCells instead).
        /// </summary>
        public static Vector2Int GetBounds(ItemShape shape)
        {
            return shape switch
            {
                ItemShape.Shape1x1     => new Vector2Int(1, 1),
                ItemShape.Shape1x2H    => new Vector2Int(2, 1),
                ItemShape.Shape2x1V    => new Vector2Int(1, 2),
                ItemShape.ShapeL       => new Vector2Int(2, 2),
                ItemShape.ShapeLMirror => new Vector2Int(2, 2),
                ItemShape.Shape2x2     => new Vector2Int(2, 2),
                _                      => new Vector2Int(1, 1)
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
        /// C4 Self-check: validates that every <see cref="ItemShape"/> enum value has a registered
        /// cell layout with cells that fit within GetBounds().
        /// Call this from a development-only Editor test or unit test to catch missing shapes early.
        /// Returns a report string (empty if all shapes pass).
        /// </summary>
        public static string ValidateAllShapes()
        {
            var sb = new StringBuilder();
            var allShapes = (ItemShape[])System.Enum.GetValues(typeof(ItemShape));

            foreach (var shape in allShapes)
            {
                var cells  = GetCells(shape);
                var bounds = GetBounds(shape);

                if (cells == null || cells.Count == 0)
                {
                    sb.AppendLine($"FAIL [{shape}]: GetCells() returned null or empty.");
                    continue;
                }

                // Every cell must be within [0, bounds)
                foreach (var cell in cells)
                {
                    if (cell.x < 0 || cell.x >= bounds.x || cell.y < 0 || cell.y >= bounds.y)
                    {
                        sb.AppendLine(
                            $"FAIL [{shape}]: cell ({cell.x},{cell.y}) is outside bounds {bounds}.");
                    }
                }

                // Bounds must be derived from actual max extents (not under-reported)
                int maxCol = 0, maxRow = 0;
                foreach (var cell in cells)
                {
                    if (cell.x > maxCol) maxCol = cell.x;
                    if (cell.y > maxRow) maxRow = cell.y;
                }
                if (bounds.x < maxCol + 1 || bounds.y < maxRow + 1)
                {
                    sb.AppendLine(
                        $"FAIL [{shape}]: GetBounds() {bounds} is smaller than actual extents " +
                        $"({maxCol + 1}×{maxRow + 1}).");
                }
            }

            return sb.ToString();
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

        /// <summary>
        /// Find the best anchor position for a shape given the cell the pointer is hovering over.
        /// Enumerates all candidate anchors by reversing each shape offset:
        ///   candidateAnchor = (hoverCol - dx, hoverRow - dy)
        /// Validates each candidate against grid bounds, then picks the one with
        /// the smallest row first, then smallest col (top-left preference).
        /// Returns true if a valid anchor was found; false if all candidates are out of bounds.
        /// </summary>
        /// <param name="shape">The shape being placed.</param>
        /// <param name="hoverCol">Column of the cell the pointer is currently over.</param>
        /// <param name="hoverRow">Row of the cell the pointer is currently over.</param>
        /// <param name="gridCols">Total columns in the grid.</param>
        /// <param name="gridRows">Total rows in the grid.</param>
        /// <param name="bestAnchor">Output: the best valid anchor (col, row), or (-1,-1) if none.</param>
        public static bool FindBestAnchor(
            ItemShape shape,
            int hoverCol, int hoverRow,
            int gridCols, int gridRows,
            out Vector2Int bestAnchor)
        {
            bestAnchor = new Vector2Int(-1, -1);
            var offsets = GetCells(shape);

            // Collect all valid candidate anchors
            // A candidate anchor is valid when the entire shape fits within the grid
            int bestRow = int.MaxValue;
            int bestCol = int.MaxValue;
            bool found = false;

            foreach (var offset in offsets)
            {
                int candidateCol = hoverCol - offset.x;
                int candidateRow = hoverRow - offset.y;

                if (FitsInGrid(shape, candidateCol, candidateRow, gridCols, gridRows))
                {
                    // Pick smallest row first, then smallest col (top-left preference)
                    if (candidateRow < bestRow ||
                        (candidateRow == bestRow && candidateCol < bestCol))
                    {
                        bestRow = candidateRow;
                        bestCol = candidateCol;
                        found = true;
                    }
                }
            }

            if (found)
                bestAnchor = new Vector2Int(bestCol, bestRow);

            return found;
        }
    }
}
