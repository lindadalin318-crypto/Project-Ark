using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Manages an independent layer of highlight tiles rendered on top of all ItemOverlayViews.
    /// Tiles are pooled (not destroyed/recreated each frame) and only rebuilt when
    /// shape/anchor/state changes. This avoids polluting SlotCellView._backgroundImage.color.
    ///
    /// Created and owned by TrackView; one instance per TypeColumn (Core / Prism).
    /// For SAIL/SAT single-cell columns, ShowSingleHighlight is used.
    /// </summary>
    public class DragHighlightLayer : MonoBehaviour
    {
        // =====================================================================
        // Runtime state
        // =====================================================================

        // Parent transform where highlight tiles are spawned (same as GridContainer)
        private RectTransform _container;

        // Pooled tile GameObjects
        private readonly List<Image> _tilePool = new();

        // Currently active tiles
        private readonly List<Image> _activeTiles = new();

        // Cache last call params to skip redundant rebuilds
        private int _lastAnchorCol = -999;
        private int _lastAnchorRow = -999;
        private ItemShape _lastShape;
        private DropPreviewState _lastState = DropPreviewState.None;

        // Cell layout (injected by TrackView)
        private float _cellSize = 56f;
        private float _cellGap  = 4f;
        private int   _gridCols = 2;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Initialize the layer with layout parameters.
        /// Reads cellSize and spacing directly from the GridLayoutGroup on the container
        /// (if present) so values are always accurate regardless of when Awake runs.
        /// Must be called once before ShowHighlight.
        /// </summary>
        public void Initialize(RectTransform container, float cellSize, float cellGap, int gridCols)
        {
            _container = container;
            _gridCols  = gridCols;

            // Prefer reading from GridLayoutGroup for accuracy (Awake runs before Layout)
            var glg = container != null ? container.GetComponent<GridLayoutGroup>() : null;
            if (glg != null)
            {
                _cellSize = glg.cellSize.x;
                _cellGap  = glg.spacing.x;
            }
            else
            {
                _cellSize = cellSize;
                _cellGap  = cellGap;
            }
        }

        /// <summary>
        /// Update grid column count (called when debug overrides change layer size).
        /// </summary>
        public void SetGridCols(int gridCols)
        {
            _gridCols = gridCols;
        }

        /// <summary>
        /// Show highlight tiles for the given shape placed at (anchorCol, anchorRow).
        /// Tiles are rendered on top of all siblings (SetAsLastSibling).
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

            float step = _cellSize + _cellGap;
            int tileIdx = 0;
            foreach (var offset in cells)
            {
                int col = anchorCol + offset.x;
                int row = anchorRow + offset.y;

                var tile = _tilePool[tileIdx++];
                tile.color = tileColor;
                tile.gameObject.SetActive(true);

                // Position: top-left origin, col increases right, row increases down
                var rt = tile.rectTransform;
                rt.anchoredPosition = new Vector2(col * step, -row * step);
                rt.sizeDelta        = new Vector2(_cellSize, _cellSize);

                // Always render on top of cells and overlays
                tile.transform.SetAsLastSibling();

                _activeTiles.Add(tile);
            }
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

            var rt = go.GetComponent<RectTransform>();
            // Match ItemOverlayView coordinate convention:
            // anchor at bottom-left (0,0), pivot at top-left (0,1)
            // anchoredPosition = (col * step, -row * step) places tiles correctly
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0f, 1f);

            go.SetActive(false);
            return img;
        }

        private void OnDestroy()
        {
            // Pool tiles are children of _container; destroyed with it automatically.
            _tilePool.Clear();
            _activeTiles.Clear();
        }
    }
}
