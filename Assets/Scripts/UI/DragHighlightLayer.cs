using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Manages an independent layer of highlight tiles rendered on top of all ItemOverlayViews.
    /// Tiles are pooled and positioned by directly reading each SlotCellView's RectTransform —
    /// no formula, no offset math, always pixel-perfect regardless of GridLayoutGroup settings.
    ///
    /// Created and owned by TrackView; one instance per TypeColumn (Core / Prism / Sail / Sat).
    /// </summary>
    public class DragHighlightLayer : MonoBehaviour
    {
        // =====================================================================
        // Runtime state
        // =====================================================================

        // Parent transform where highlight tiles are spawned (same as GridContainer)
        private RectTransform _container;

        // The actual cell views — tiles are positioned by reading their RectTransforms directly.
        private SlotCellView[] _cells;

        // Grid column count (needed to convert col+row → cell index)
        private int _gridCols = 2;

        // Pooled tile GameObjects
        private readonly List<Image> _tilePool = new();

        // Currently active tiles
        private readonly List<Image> _activeTiles = new();

        // Cache last call params to skip redundant rebuilds
        private int _lastAnchorCol = -999;
        private int _lastAnchorRow = -999;
        private ItemShape _lastShape;
        private DropPreviewState _lastState = DropPreviewState.None;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Initialize the layer.
        /// Tiles will be positioned by reading each cell's RectTransform directly —
        /// no formula, no offset math.
        /// </summary>
        /// <param name="container">GridContainer — parent for spawned tiles.</param>
        /// <param name="cells">All SlotCellViews in this column (row-major order).</param>
        /// <param name="gridCols">Number of columns in the grid (used for col+row → index).</param>
        public void Initialize(RectTransform container, SlotCellView[] cells, int gridCols)
        {
            _container = container;
            _cells     = cells;
            _gridCols  = Mathf.Max(1, gridCols);
        }

        /// <summary>
        /// Update grid column count (called when debug overrides change layer size).
        /// </summary>
        public void SetGridCols(int gridCols)
        {
            _gridCols = Mathf.Max(1, gridCols);
        }

        /// <summary>
        /// Show highlight tiles for the given shape placed at (anchorCol, anchorRow).
        /// Tiles are positioned by reading the corresponding SlotCellView's RectTransform —
        /// guaranteed to be pixel-perfect regardless of GridLayoutGroup alignment.
        /// </summary>
public void ShowHighlight(int anchorCol, int anchorRow, ItemShape shape, DropPreviewState state)
        {
            if (_container == null) return;

            // Skip rebuild if nothing changed
            if (anchorCol == _lastAnchorCol &&
                anchorRow == _lastAnchorRow &&
                shape     == _lastShape     &&
                state     == _lastState)
                return;

            _lastAnchorCol = anchorCol;
            _lastAnchorRow = anchorRow;
            _lastShape     = shape;
            _lastState     = state;

            var cells = ItemShapeHelper.GetCells(shape);
            int needed = cells.Count;

            // Ensure pool has enough tiles
            while (_tilePool.Count < needed)
                _tilePool.Add(CreateTile());

            // Deactivate all active tiles first
            foreach (var t in _activeTiles)
                if (t != null) t.gameObject.SetActive(false);
            _activeTiles.Clear();

            if (state == DropPreviewState.None)
                return;

            Color tileColor = state switch
            {
                DropPreviewState.Valid   => StarChartTheme.HighlightValid,
                DropPreviewState.Replace => StarChartTheme.HighlightReplace,
                DropPreviewState.Invalid => StarChartTheme.HighlightInvalid,
                _                        => Color.clear
            };

            int tileIdx = 0;
            foreach (var offset in cells)
            {
                int col = anchorCol + offset.x;
                int row = anchorRow + offset.y;
                int cellIndex = row * _gridCols + col;

                var tile = _tilePool[tileIdx++];
                tile.color = tileColor;
                tile.gameObject.SetActive(true);

                var rt = tile.rectTransform;

                // Directly copy position and size from the actual cell's RectTransform.
                if (_cells != null && cellIndex >= 0 && cellIndex < _cells.Length
                    && _cells[cellIndex] != null)
                {
                    var cellRt = _cells[cellIndex].GetComponent<RectTransform>();

                    rt.anchorMin        = cellRt.anchorMin;
                    rt.anchorMax        = cellRt.anchorMax;
                    rt.pivot            = cellRt.pivot;
                    rt.anchoredPosition = cellRt.anchoredPosition;
                    rt.sizeDelta        = cellRt.sizeDelta;
                }
                else
                {
                    Debug.LogWarning($"[DragHighlightLayer] MISS cellIndex={cellIndex} " +
                                     $"col={col} row={row} gridCols={_gridCols}");
                }

                // Always render on top of cells and overlays
                tile.transform.SetAsLastSibling();

                _activeTiles.Add(tile);
            }
        }

        /// <summary>
        /// Show a single-cell highlight directly at the given cell index.
        /// Bypasses col/row → index conversion entirely — always pixel-perfect for any grid layout.
        /// </summary>
        public void ShowHighlightAtCellIndex(int cellIndex, DropPreviewState state)
        {
            if (_container == null) return;
            if (_cells == null || cellIndex < 0 || cellIndex >= _cells.Length
                || _cells[cellIndex] == null)
            {
                Debug.LogWarning($"[DragHighlightLayer] ShowHighlightAtCellIndex MISS cellIndex={cellIndex}");
                return;
            }

            // Skip rebuild if nothing changed
            if (cellIndex == _lastAnchorCol && _lastAnchorRow == -1 && state == _lastState)
                return;

            _lastAnchorCol = cellIndex;
            _lastAnchorRow = -1;  // sentinel: means "direct cellIndex mode"
            _lastShape     = ItemShape.Shape1x1;
            _lastState     = state;

            // Deactivate all active tiles
            foreach (var t in _activeTiles)
                if (t != null) t.gameObject.SetActive(false);
            _activeTiles.Clear();

            if (state == DropPreviewState.None) return;

            Color tileColor = state switch
            {
                DropPreviewState.Valid   => StarChartTheme.HighlightValid,
                DropPreviewState.Replace => StarChartTheme.HighlightReplace,
                DropPreviewState.Invalid => StarChartTheme.HighlightInvalid,
                _                        => Color.clear
            };

            // Ensure pool has at least 1 tile
            while (_tilePool.Count < 1)
                _tilePool.Add(CreateTile());

            var tile = _tilePool[0];
            tile.color = tileColor;
            tile.gameObject.SetActive(true);

            var rt     = tile.rectTransform;
            var cellRt = _cells[cellIndex].GetComponent<RectTransform>();

            rt.anchorMin        = cellRt.anchorMin;
            rt.anchorMax        = cellRt.anchorMax;
            rt.pivot            = cellRt.pivot;
            rt.anchoredPosition = cellRt.anchoredPosition;
            rt.sizeDelta        = cellRt.sizeDelta;

            tile.transform.SetAsLastSibling();
            _activeTiles.Add(tile);
        }

        /// <summary>
        /// Show a single-cell highlight at (col, row) — for SAIL / SAT columns.
        /// </summary>
        public void ShowSingleHighlight(int col, int row, DropPreviewState state)
        {
            if (_container == null) return;
            ShowHighlight(col, row, ItemShape.Shape1x1, state);
        }

        /// <summary>
        /// Clear all active highlight tiles immediately (same frame, no coroutine).
        /// </summary>
        public void ClearHighlight()
        {
            foreach (var t in _activeTiles)
                if (t != null) t.gameObject.SetActive(false);
            _activeTiles.Clear();

            // Reset cache so next ShowHighlight always rebuilds
            _lastAnchorCol = -999;
            _lastAnchorRow = -999;
            _lastShape     = default;
            _lastState     = DropPreviewState.None;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private Image CreateTile()
        {
            var go = new GameObject("HighlightTile", typeof(RectTransform));
            go.transform.SetParent(_container, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false; // tiles must NOT block pointer events on cells below

            // Prevent GridLayoutGroup from repositioning this tile
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            go.SetActive(false);
            return img;
        }

        private void OnDestroy()
        {
            _tilePool.Clear();
            _activeTiles.Clear();
        }
    }
}
