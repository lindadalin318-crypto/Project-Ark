using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Visual representation of one <see cref="WeaponTrack"/> (Primary or Secondary).
    /// Displays 4 TypeColumns: SAIL / PRISM / CORE / SAT.
    /// Each column contains a 2×2 grid of <see cref="SlotCellView"/> cells.
    /// Subscribes to <see cref="WeaponTrack.OnLoadoutChanged"/> for reactive refresh.
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

        [Header("Type Columns (SAIL / PRISM / CORE / SAT)")]
        [SerializeField] private TypeColumn _sailColumn;
        [SerializeField] private TypeColumn _prismColumn;
        [SerializeField] private TypeColumn _coreColumn;
        [SerializeField] private TypeColumn _satColumn;

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

        // Cell size and gap used for overlay positioning (must match UICanvasBuilder values)
        // These are read from the first cell's RectTransform at runtime.
        private float _cellSize = 56f;
        private float _cellGap  = 4f;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            _columns = new[] { _sailColumn, _prismColumn, _coreColumn, _satColumn };

            // Detect cell size from the first available cell's RectTransform
            foreach (var col in new[] { _coreColumn, _prismColumn, _sailColumn, _satColumn })
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
            InitColumn(_sailColumn,  SlotType.LightSail, StarChartTheme.SailColor);
            InitColumn(_prismColumn, SlotType.Prism,     StarChartTheme.PrismColor);
            InitColumn(_coreColumn,  SlotType.Core,      StarChartTheme.CoreColor);
            InitColumn(_satColumn,   SlotType.Satellite, StarChartTheme.SatColor);
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

            // SAIL/SAT: inject space-check delegates
            if (slotType == SlotType.LightSail)
            {
                foreach (var cell in cells)
                    if (cell != null) cell.HasSpaceForItem = (item) => HasSpaceForSail(item);
            }
            else if (slotType == SlotType.Satellite)
            {
                foreach (var cell in cells)
                    if (cell != null) cell.HasSpaceForItem = (item) => HasSpaceForSat(item);
            }
        }

        // =====================================================================
        // Public API
        // =====================================================================

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

        /// <summary> Refresh all columns from current track loadout. </summary>
        public void Refresh()
        {
            RefreshColumn(_coreColumn,  _track?.CoreLayer,  StarChartItemType.Core);
            RefreshColumn(_prismColumn, _track?.PrismLayer, StarChartItemType.Prism);
            RefreshSailColumn();
            RefreshSatColumn();
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
                SlotType.LightSail => "SAIL",
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

            // Reset all cells to empty (cells remain as drop targets)
            foreach (var cell in cells)
                cell?.SetEmpty();

            // Destroy previous overlays for this column
            DestroyOverlaysForColumn(col);

            if (layer == null) return;

            // Track which items have already had their overlay created
            var processedItems = new HashSet<T>();

            for (int r = 0; r < SlotLayer<T>.GRID_ROWS; r++)
            {
                for (int c = 0; c < SlotLayer<T>.GRID_COLS; c++)
                {
                    var item = layer.GetAt(c, r);
                    if (item == null) continue;

                    // Mark the underlying cell as occupied (hide '+' placeholder)
                    int cellIndex = r * SlotLayer<T>.GRID_COLS + c;
                    if (cellIndex < cells.Length && cells[cellIndex] != null)
                        cells[cellIndex].SetOverlay(item, isPrimary: false); // hide placeholder, no icon

                    // Only create one overlay per item (at its anchor)
                    if (processedItems.Contains(item)) continue;
                    processedItems.Add(item);

                    var anchor = layer.GetAnchor(item as T);
                    if (anchor.x < 0) continue; // item not found in layer

                    // Create ItemOverlayView
                    var overlayGo = new GameObject($"Overlay_{item.DisplayName}",
                        typeof(RectTransform));
                    var overlayView = overlayGo.AddComponent<ItemOverlayView>();

                    // Parent to the grid container (same parent as the cells)
                    var gridContainer = col.GridContainer;
                    overlayGo.transform.SetParent(gridContainer, false);
                    overlayGo.transform.SetAsLastSibling(); // render on top of cells

                    overlayView.Setup(
                        item,
                        col.SlotType,
                        this,
                        anchor.x, anchor.y,
                        _cellSize, _cellGap);

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

        private void RefreshSailColumn()
        {
            if (_sailColumn == null) return;
            var cells = _sailColumn.Cells;

            if (_controller == null)
                Debug.LogWarning("[TrackView] _controller is null, SAIL slot will be empty.");

            var sail = _controller?.GetEquippedLightSail();

            // SAIL column: only cell[0] is used (1 slot)
            if (cells[0] != null)
            {
                if (sail != null)
                    cells[0].SetItem(sail);
                else
                    cells[0].SetEmpty();
            }

            // Remaining cells stay empty
            for (int i = 1; i < cells.Length; i++)
                cells[i]?.SetEmpty();
        }

        private void RefreshSatColumn()
        {
            if (_satColumn == null) return;
            var cells = _satColumn.Cells;

            if (_controller == null)
                Debug.LogWarning("[TrackView] _controller is null, SAT slots will be empty.");

            var sats = _controller?.GetEquippedSatellites();

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;

                if (sats != null && i < sats.Count)
                    cells[i].SetItem(sats[i]);
                else
                    cells[i].SetEmpty();
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
                SlotType.LightSail => "SAIL",
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
            for (int r = 0; r < SlotLayer<T>.GRID_ROWS; r++)
            {
                for (int c = 0; c < SlotLayer<T>.GRID_COLS; c++)
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

        private bool HasSpaceForSail(StarChartItemSO item)
        {
            if (_controller == null) return true;
            return _controller.GetEquippedLightSail() == null;
        }

        private bool HasSpaceForSat(StarChartItemSO item)
        {
            if (_controller == null) return true;
            var sats = _controller.GetEquippedSatellites();
            return sats == null || sats.Count < 4; // 2×2 = 4 SAT slots
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
        /// </summary>
        public void SetShapeHighlight(int anchorCol, int anchorRow, ItemShape shape, DropPreviewState state, bool isCoreLayer)
        {
            var col = isCoreLayer ? _coreColumn : _prismColumn;
            if (col == null) return;
            var cells = col.Cells;

            // Use a local constant to avoid misleading generic type parameter on GRID_COLS
            // (GRID_COLS is a constant independent of the type argument)
            const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS;

            foreach (var offset in ItemShapeHelper.GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                int cellIndex = r * gridCols + c;
                if (cellIndex < 0 || cellIndex >= cells.Length || cells[cellIndex] == null) continue;

                switch (state)
                {
                    case DropPreviewState.Valid:
                        cells[cellIndex].SetHighlight(true);
                        break;
                    case DropPreviewState.Replace:
                        cells[cellIndex].SetReplaceHighlight();
                        break;
                    case DropPreviewState.Invalid:
                        cells[cellIndex].SetHighlight(false);
                        break;
                    default:
                        cells[cellIndex].ClearHighlight();
                        break;
                }
            }
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
        }

        /// <summary> Get the TypeColumn for a given SlotType. </summary>
        public TypeColumn GetColumn(SlotType slotType)
        {
            return slotType switch
            {
                SlotType.LightSail => _sailColumn,
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
