using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Visual representation of one <see cref="WeaponTrack"/> (Primary or Secondary).
    /// Displays 3 TypeColumns: PRISM / CORE / SAT.
    /// Each column contains a 2×2 grid of <see cref="SlotCellView"/> cells.
    /// Subscribes to <see cref="WeaponTrack.OnLoadoutChanged"/> for reactive refresh.
    /// SAIL column is now a shared column managed by StarChartPanel, not per-track.
    /// </summary>
    public class TrackView : MonoBehaviour, IPointerEnterHandler
    {
        // TypeColumn is defined in TypeColumn.cs (top-level class in ProjectArk.UI namespace).
        // Keeping it as a top-level class ensures a stable Unity GUID and avoids
        // "script cannot be loaded" errors caused by nested-class GUID instability.

        // =====================================================================
        // TrackView fields
        // =====================================================================

        [Header("Track Label")]
        [SerializeField] private TMP_Text _trackLabel;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _selectionBorder;

        [Header("Type Columns (PRISM / CORE / SAT)")]
        [SerializeField] private TypeColumn _prismColumn;
        [SerializeField] private TypeColumn _coreColumn;
        [SerializeField] private TypeColumn _satColumn;

        [Header("Debug: Slot Counts (0 = use save data)")]
        [Tooltip("Override Core layer column count for debugging. 0 = use save data. Range 1-4.")]
        [SerializeField] [Range(0, 4)] private int _debugCoreCols = 0;
        [Tooltip("Override Prism layer column count for debugging. 0 = use save data. Range 1-4.")]
        [SerializeField] [Range(0, 4)] private int _debugPrismCols = 0;

        /// <summary> Fired when a cell with an equipped item is clicked (for unequip). </summary>
        public event Action<StarChartItemSO> OnCellClicked;

        /// <summary> Fired when the pointer enters a cell that has an equipped item. Params: item, location string. </summary>
        public event Action<StarChartItemSO, string> OnCellPointerEntered;

        /// <summary> Fired when the pointer exits a cell. </summary>
        public event Action OnCellPointerExited;

        /// <summary> Fired when the track select button is clicked. </summary>
        public event Action<TrackView> OnTrackSelected;

        private WeaponTrack _track;
        private StarChartController _controller;

        // Ordered array for iteration convenience
        private TypeColumn[] _columns;

        // Active overlay views — destroyed and recreated on each Refresh
        private readonly List<ItemOverlayView> _activeOverlays = new();

        // Debounce flag: prevents multiple Refresh() calls in the same frame
        // from creating duplicate overlays. Only the last call in a frame executes.
        private bool _refreshPending;

        // Independent highlight tile layers — one per column, never pollute cell background colors
        private DragHighlightLayer _coreHighlightLayer;
        private DragHighlightLayer _prismHighlightLayer;
        private DragHighlightLayer _satHighlightLayer;

        // Cell size and gap used for overlay positioning (must match UICanvasBuilder values)
        // These are read from the first cell's RectTransform at runtime.
        private float _cellSize = 56f;
        private float _cellGap  = 4f;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            _columns = new[] { _prismColumn, _coreColumn, _satColumn };

            // Detect cell size from the first available cell's RectTransform
            foreach (var col in new[] { _coreColumn, _prismColumn, _satColumn })
            {
                if (col?.Cells == null) continue;
                var firstCell = col.Cells.Length > 0 ? col.Cells[0] : null;
                if (firstCell == null) continue;
                var rt = firstCell.GetComponent<RectTransform>();
                if (rt != null)
                {
                    _cellSize = rt.sizeDelta.x;
                    // Gap = distance between cell[0] and cell[1] minus cellSize
                    if (col.Cells.Length > 1 && col.Cells[1] != null)
                    {
                        var rt1 = col.Cells[1].GetComponent<RectTransform>();
                        if (rt1 != null)
                            _cellGap = Mathf.Abs(rt1.anchoredPosition.x - rt.anchoredPosition.x) - _cellSize;
                    }
                    break;
                }
            }



            // Initialize each column
            InitColumn(_prismColumn, SlotType.Prism,     StarChartTheme.PrismColor);
            InitColumn(_coreColumn,  SlotType.Core,      StarChartTheme.CoreColor);
            InitColumn(_satColumn,   SlotType.Satellite, StarChartTheme.SatColor);

            // Create independent highlight layers for each column
            _coreHighlightLayer  = CreateHighlightLayer(_coreColumn);
            _prismHighlightLayer = CreateHighlightLayer(_prismColumn);
            _satHighlightLayer   = CreateHighlightLayer(_satColumn);

            // Apply debug slot count overrides (only when > 0)
            ApplyDebugSlotCounts();
        }

        /// <summary>
        /// Create and initialize a DragHighlightLayer for the given column.
        /// Returns null if the column is null.
        /// </summary>
        private DragHighlightLayer CreateHighlightLayer(TypeColumn col)
        {
            if (col == null) return null;
            var go = new GameObject("DragHighlightLayer", typeof(RectTransform));
            go.transform.SetParent(col.GridContainer, false);
            var layer = go.AddComponent<DragHighlightLayer>();

            // Pass the cells array directly — tiles are positioned by reading each cell's
            // RectTransform, so they are always pixel-perfect regardless of GridLayoutGroup settings.
            layer.Initialize(col.GridContainer, col.Cells, 2);
            return layer;
        }

        private void InitColumn(TypeColumn col, SlotType slotType, Color typeColor)
        {
            if (col == null) return;
            col.Initialize(slotType, typeColor, this);

            // Wire cell click and hover events
            var cells = col.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                var captured = cells[i];
                captured.OnClicked += () => HandleCellClick(captured);
                captured.OnPointerEntered += (item) => HandleCellPointerEnter(captured, item);
                captured.OnPointerExited += HandleCellPointerExit;
            }

            // SAT: no special delegate needed — uses SlotLayer<SatelliteSO> like Core/Prism
            // (HasSpaceForItem delegate is only for legacy SAIL-style single-slot types)
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Apply debug slot count overrides to the bound track's layers.
        /// Called from Awake (before Bind) and again from Bind if track is already set.
        /// </summary>
        private void ApplyDebugSlotCounts()
        {
            if (_track == null) return;
            if (_debugCoreCols > 0 || _debugPrismCols > 0)
            {
                int coreCols  = _debugCoreCols  > 0 ? _debugCoreCols  : _track.CoreLayer.Cols;
                int prismCols = _debugPrismCols > 0 ? _debugPrismCols : _track.PrismLayer.Cols;
                _track.SetLayerCols(coreCols, prismCols);
            }
        }

        /// <summary> Bind to a WeaponTrack and StarChartController, subscribe to loadout changes. </summary>
        public void Bind(WeaponTrack track, StarChartController controller = null)
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;

            _track = track;
            _controller = controller;

            if (_track != null)
            {
                _track.OnLoadoutChanged += Refresh;

                if (_trackLabel != null)
                {
                    _trackLabel.text = _track.Id == WeaponTrack.TrackId.Primary
                        ? "PRIMARY" : "SECONDARY";
                    _trackLabel.color = StarChartTheme.Cyan;
                }

                // Re-apply debug overrides now that track is bound
                ApplyDebugSlotCounts();
            }

            Refresh();
        }

        /// <summary> The bound WeaponTrack. </summary>
        public WeaponTrack Track => _track;

        /// <summary> Set visual selection state (border highlight). </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
                _selectionBorder.color = selected ? StarChartTheme.Cyan : StarChartTheme.Border;
        }

        /// <summary>
        /// Refresh all columns from current track loadout.
        /// Debounced: if called multiple times in the same frame (e.g. from OnLoadoutChanged
        /// firing several times during a single drop), only the last call executes.
        /// Also waits one frame so GridLayoutGroup has time to complete its Layout pass,
        /// ensuring cell anchoredPositions are correct when overlays are created.
        /// </summary>
        public void Refresh()
        {
            if (_refreshPending) return; // already scheduled — skip duplicate
            _refreshPending = true;
            DoRefreshAsync().Forget();
        }

        private async UniTaskVoid DoRefreshAsync()
        {
            // Wait one frame: GridLayoutGroup runs in LateUpdate, so after one yield
            // all cell RectTransforms will have their final anchoredPositions.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, destroyCancellationToken);

            // Reset flag BEFORE doing any work, so any Refresh() call that arrives
            // during the await (e.g. from RefreshAllViews) is NOT blocked by the old flag.
            // If another Refresh() sneaks in between the await and here, it will schedule
            // a new DoRefreshAsync — that is fine, it will run after this one completes.
            _refreshPending = false;

            // Eagerly destroy ALL active overlays before rebuilding.
            // Using DestroyImmediate here (Edit Mode safe) or Destroy + clear list.
            // We clear the list first so DestroyOverlaysForColumn won't find them,
            // then destroy the GameObjects directly.
            for (int i = _activeOverlays.Count - 1; i >= 0; i--)
            {
                var ov = _activeOverlays[i];
                if (ov != null) Destroy(ov.gameObject);
            }
            _activeOverlays.Clear();

            // Force canvas layout to be up-to-date before reading cell positions
            Canvas.ForceUpdateCanvases();

            RefreshColumn(_coreColumn,  _track?.CoreLayer,  StarChartItemType.Core);
            RefreshColumn(_prismColumn, _track?.PrismLayer, StarChartItemType.Prism);
            RefreshColumn(_satColumn,   _track?.SatLayer,   StarChartItemType.Satellite);
        }

        // =====================================================================
        // Overlay event forwarding (called by ItemOverlayView)
        // =====================================================================

        /// <summary> Called by ItemOverlayView when the pointer enters an overlay. </summary>
        internal void RaiseOverlayCellPointerEntered(StarChartItemSO item, SlotType slotType)
        {
            string trackName = _track?.Id == WeaponTrack.TrackId.Primary ? "PRIMARY" : "SECONDARY";
            string typeName = slotType switch
            {
                SlotType.Core      => "CORE",
                SlotType.Prism     => "PRISM",
                SlotType.Satellite => "SAT",
                _                  => string.Empty
            };
            OnCellPointerEntered?.Invoke(item, $"{trackName} · {typeName}");
        }

        /// <summary> Called by ItemOverlayView when the pointer exits an overlay. </summary>
        internal void RaiseOverlayCellPointerExited()
        {
            OnCellPointerExited?.Invoke();
        }

        /// <summary> Called by ItemOverlayView when an overlay is clicked. </summary>
        internal void RaiseOverlayCellClicked(StarChartItemSO item)
        {
            OnCellClicked?.Invoke(item);
        }

        // =====================================================================
        // Refresh helpers
        // =====================================================================

        private void RefreshColumn<T>(TypeColumn col, SlotLayer<T> layer, StarChartItemType layerType)
            where T : StarChartItemSO
        {
            if (col == null) return;
            var cells = col.Cells;

            // Determine how many cells are currently unlocked
            int unlockedCount = layer != null ? layer.Rows * layer.Cols : 0;

            // Show unlocked cells, hide locked cells via CanvasGroup (CLAUDE.md 第11条：禁止 SetActive)
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                bool unlocked = i < unlockedCount;
                var cg = cells[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = cells[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha          = unlocked ? 1f : 0f;
                cg.interactable   = unlocked;
                cg.blocksRaycasts = unlocked;
            }

            // Reset visible cells to empty (cells remain as drop targets)
            for (int i = 0; i < unlockedCount && i < cells.Length; i++)
                cells[i]?.SetEmpty();

            // Destroy previous overlays for this column
            DestroyOverlaysForColumn(col);

            if (layer == null) return;

            // Track which items have already had their overlay created
            var processedItems = new HashSet<T>();

            for (int r = 0; r < layer.Rows; r++)
            {
                for (int c = 0; c < layer.Cols; c++)
                {
                    var item = layer.GetAt(c, r);
                    if (item == null) continue;

                    // Hide the underlying cell visually — ItemOverlayView handles all visuals.
                    // SetHiddenByOverlay(item) clears background/icon but PRESERVES DisplayedItem,
                    // so SlotCellView.OnBeginDrag can still fire (same path as SAIL/SAT).
                    int cellIndex = r * layer.Cols + c;
                    if (cellIndex < cells.Length && cells[cellIndex] != null)
                        cells[cellIndex].SetHiddenByOverlay(item);

                    // Only create one overlay per item (at its anchor)
                    if (processedItems.Contains(item)) continue;
                    processedItems.Add(item);

                    var anchor = layer.GetAnchor(item as T);
                    if (anchor.x < 0) continue; // item not found in layer

                    // Create ItemOverlayView
                    var overlayGo = new GameObject($"Overlay_{item.DisplayName}",
                        typeof(RectTransform));
                    var overlayView = overlayGo.AddComponent<ItemOverlayView>();

                    // CRITICAL: Add LayoutElement.ignoreLayout=true BEFORE SetParent.
                    // If we SetParent first, GridLayoutGroup immediately repositions the
                    // overlay (overriding the stretch anchors set in Setup()).
                    // Adding LayoutElement before SetParent prevents this entirely.
                    var overlayLe = overlayGo.AddComponent<LayoutElement>();
                    overlayLe.ignoreLayout = true;

                    // Parent to the grid container (same parent as the cells)
                    var gridContainer = col.GridContainer;
                    overlayGo.transform.SetParent(gridContainer, false);
                    overlayGo.transform.SetAsLastSibling(); // render on top of cells

                    // Read cell size/gap fresh from the actual RectTransforms at Refresh time
                    // (GridLayoutGroup has completed Layout by now, unlike Awake)
                    float freshCellSize = _cellSize;
                    float freshCellGap  = _cellGap;
                    if (col.Cells.Length > 0 && col.Cells[0] != null)
                    {
                        var c0rt = col.Cells[0].GetComponent<RectTransform>();
                        if (c0rt != null)
                        {
                            freshCellSize = c0rt.sizeDelta.x;
                            if (col.Cells.Length > 1 && col.Cells[1] != null)
                            {
                                var c1rt = col.Cells[1].GetComponent<RectTransform>();
                                if (c1rt != null)
                                    freshCellGap = Mathf.Abs(c1rt.anchoredPosition.x - c0rt.anchoredPosition.x) - freshCellSize;
                            }
                        }
                    }
                    overlayView.Setup(
                        item,
                        col.SlotType,
                        this,
                        anchor.x, anchor.y,
                        freshCellSize, freshCellGap,
                        cells,          // pass cells array for pixel-perfect anchor
                        layer.Cols);    // pass grid cols for anchorRow * cols + anchorCol

                    _activeOverlays.Add(overlayView);
                }
            }
        }

        /// <summary> Destroy all active overlays belonging to the given column. </summary>
        private void DestroyOverlaysForColumn(TypeColumn col)
        {
            if (col == null) return;
            var gridContainer = col.GridContainer;
            for (int i = _activeOverlays.Count - 1; i >= 0; i--)
            {
                var ov = _activeOverlays[i];
                if (ov == null) { _activeOverlays.RemoveAt(i); continue; }
                if (ov.transform.parent == gridContainer)
                {
                    Destroy(ov.gameObject);
                    _activeOverlays.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        // Cell click & hover
        // =====================================================================

        private void HandleCellClick(SlotCellView cell)
        {
            if (cell.DisplayedItem != null)
                OnCellClicked?.Invoke(cell.DisplayedItem);
        }

        private void HandleCellPointerEnter(SlotCellView cell, StarChartItemSO item)
        {
            if (item == null) return;
            string trackName = _track?.Id == WeaponTrack.TrackId.Primary ? "PRIMARY" : "SECONDARY";
            string typeName = cell.SlotType switch
            {
                SlotType.Core      => "CORE",
                SlotType.Prism     => "PRISM",
                SlotType.Satellite => "SAT",
                _                  => string.Empty
            };
            string location = $"{trackName} · {typeName}";
            OnCellPointerEntered?.Invoke(item, location);
        }

        private void HandleCellPointerExit()
        {
            OnCellPointerExited?.Invoke();
        }

        // =====================================================================
        // Drag-and-Drop Support
        // =====================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging)
                OnTrackSelected?.Invoke(this);
        }

        /// <summary>
        /// Check whether this track's layer has enough space for the given item.
        /// Uses CanPlace to scan all possible anchor positions.
        /// </summary>
        public bool HasSpaceForItem(StarChartItemSO item, bool isCoreLayer)
        {
            if (_track == null) return false;
            if (isCoreLayer)
                return FindFirstAnchor(_track.CoreLayer, item, out _);
            else
                return FindFirstAnchor(_track.PrismLayer, item, out _);
        }

        /// <summary>
        /// Find the first valid anchor position for the item in the given layer.
        /// Returns true if found, and sets anchor to the (col, row) position.
        /// </summary>
        public bool FindFirstAnchor<T>(SlotLayer<T> layer, StarChartItemSO item, out Vector2Int anchor)
            where T : StarChartItemSO
        {
            anchor = new Vector2Int(-1, -1);
            if (layer == null || item == null) return false;
            for (int r = 0; r < layer.Rows; r++)
            {
                for (int c = 0; c < layer.Cols; c++)
                {
                    var (canPlace, evictList) = layer.CanPlace(item as T, c, r);
                    if (canPlace && (evictList == null || evictList.Count == 0))
                    {
                        anchor = new Vector2Int(c, r);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasSpaceForSat(StarChartItemSO item)
        {
            // Legacy: kept for reference. SAT now uses SlotLayer<SatelliteSO> like Core/Prism.
            // This method is no longer called.
            if (_track == null) return true;
            return _track.SatLayer.FreeSpace > 0;
        }

        /// <summary>
        /// Highlight multiple consecutive cells for a SlotSize > 1 item preview.
        /// Supports replace (orange) highlight.
        /// </summary>
        public void SetMultiCellHighlight(int startIndex, int count, bool isCoreLayer, bool valid, bool isReplace = false)
        {
            var col = isCoreLayer ? _coreColumn : _prismColumn;
            col?.SetCellHighlight(startIndex, count, valid, isReplace);
        }

        /// <summary>
        /// Highlight all cells covered by the given shape placed at (anchorCol, anchorRow)
        /// in the specified column (Core or Prism layer).
        /// Delegates to DragHighlightLayer — does NOT modify SlotCellView._backgroundImage.
        /// </summary>
        public void SetShapeHighlight(int anchorCol, int anchorRow, ItemShape shape, DropPreviewState state, bool isCoreLayer)
        {
            // Update grid col count in case debug overrides changed it
            int gridCols = isCoreLayer ? _track?.CoreLayer?.Cols ?? SlotLayer<StarCoreSO>.MAX_COLS
                                       : _track?.PrismLayer?.Cols ?? SlotLayer<PrismSO>.MAX_COLS;

            var layer = isCoreLayer ? _coreHighlightLayer : _prismHighlightLayer;
            layer?.SetGridCols(gridCols);
            layer?.ShowHighlight(anchorCol, anchorRow, shape, state);
        }

        /// <summary>
        /// Show a single-cell highlight for SAT column.
        /// </summary>
        public void SetSingleHighlight(SlotType slotType, int col, int row, DropPreviewState state)
        {
            var layer = slotType switch
            {
                SlotType.Satellite => _satHighlightLayer,
                _                  => null
            };
            layer?.ShowSingleHighlight(col, row, state);
        }

        /// <summary>
        /// Show a single-cell highlight for SAT column using a direct cell index.
        /// Bypasses col/row conversion — always pixel-perfect regardless of grid layout.
        /// </summary>
        public void SetSingleHighlightAtIndex(SlotType slotType, int cellIndex, DropPreviewState state)
        {
            var layer = slotType switch
            {
                SlotType.Satellite => _satHighlightLayer,
                _                  => null
            };
            layer?.ShowHighlightAtCellIndex(cellIndex, state);
        }

        /// <summary>
        /// Clear shape highlight tiles for the given column type.
        /// </summary>
        public void ClearShapeHighlight(bool isCoreLayer)
        {
            var layer = isCoreLayer ? _coreHighlightLayer : _prismHighlightLayer;
            layer?.ClearHighlight();
        }

        /// <summary>
        /// Clear all highlight tiles across all columns.
        /// </summary>
        public void ClearAllHighlightTiles()
        {
            _coreHighlightLayer?.ClearHighlight();
            _prismHighlightLayer?.ClearHighlight();
            _satHighlightLayer?.ClearHighlight();
        }

        /// <summary>
        /// Highlight the TypeColumn border for a given slot type (drag-over feedback).
        /// </summary>
        public void SetColumnDropHighlight(SlotType slotType, bool valid)
        {
            GetColumn(slotType)?.SetDropHighlight(valid);
        }

        /// <summary>
        /// Clear all drag highlights on every cell and column border.
        /// </summary>
        public void ClearAllHighlights()
        {
            if (_columns == null) return;
            foreach (var col in _columns)
            {
                col?.ClearAllCellHighlights();
                col?.ClearDropHighlight();
            }
            // Also clear independent highlight tile layers
            ClearAllHighlightTiles();
        }

        /// <summary> Get the TypeColumn for a given SlotType. </summary>
        public TypeColumn GetColumn(SlotType slotType)
        {
            return slotType switch
            {
                SlotType.Prism     => _prismColumn,
                SlotType.Core      => _coreColumn,
                SlotType.Satellite => _satColumn,
                _                  => null
            };
        }

        /// <summary> Get all cells across all columns (for iteration). </summary>
        public IEnumerable<SlotCellView> GetAllCells()
        {
            if (_columns == null) yield break;
            foreach (var col in _columns)
            {
                if (col == null) continue;
                foreach (var cell in col.Cells)
                    if (cell != null) yield return cell;
            }
        }

        private void OnDestroy()
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;
        }
    }
}
