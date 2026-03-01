using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
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
        // =====================================================================
        // TypeColumn — inner class representing one type column (2×2 grid)
        // =====================================================================

        /// <summary>
        /// Represents a single type column (SAIL / PRISM / CORE / SAT) in the TrackView.
        /// Holds a 2×2 grid of SlotCellView cells and handles per-column highlight/hover.
        /// </summary>
        [Serializable]
        public class TypeColumn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            [SerializeField] private TMP_Text _columnLabel;
            [SerializeField] private Image _columnDot;
            [SerializeField] private Image _columnBorder;

            /// <summary> 4 cells in row-major order: [0]=top-left, [1]=top-right, [2]=bottom-left, [3]=bottom-right </summary>
            [SerializeField] private SlotCellView[] _cells = new SlotCellView[4];

            private SlotType _slotType;
            private Color _typeColor;
            private Color _dimColor;
            private Tween _borderTween;

            /// <summary> The slot type this column represents. </summary>
            public SlotType SlotType => _slotType;

            /// <summary> All 4 cells in this column. </summary>
            public SlotCellView[] Cells => _cells;

            /// <summary> Initialize column identity and colors. </summary>
            public void Initialize(SlotType slotType, Color typeColor, TrackView ownerTrack)
            {
                _slotType = slotType;
                _typeColor = typeColor;
                _dimColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.18f);

                // Label
                if (_columnLabel != null)
                {
                    _columnLabel.text = slotType switch
                    {
                        SlotType.LightSail => "SAIL",
                        SlotType.Prism     => "PRISM",
                        SlotType.Core      => "CORE",
                        SlotType.Satellite => "SAT",
                        _                  => slotType.ToString().ToUpper()
                    };
                    _columnLabel.color = typeColor;
                }

                // Dot
                if (_columnDot != null)
                    _columnDot.color = typeColor;

                // Border default dim
                if (_columnBorder != null)
                    _columnBorder.color = _dimColor;

                // Wire up cells
                for (int i = 0; i < _cells.Length; i++)
                {
                    if (_cells[i] == null) continue;
                    _cells[i].SlotType = slotType;
                    _cells[i].CellIndex = i;
                    _cells[i].OwnerTrack = ownerTrack;
                    _cells[i].SetThemeColor(typeColor);
                }
            }

            // ── Hover border animation ──────────────────────────────────────

            public void OnPointerEnter(PointerEventData eventData)
            {
                _borderTween.Stop();
                if (_columnBorder != null)
                    _borderTween = Tween.Color(_columnBorder, endValue: _typeColor,
                        duration: 0.15f, ease: Ease.OutQuad, useUnscaledTime: true);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                _borderTween.Stop();
                if (_columnBorder != null)
                    _borderTween = Tween.Color(_columnBorder, endValue: _dimColor,
                        duration: 0.25f, ease: Ease.OutQuad, useUnscaledTime: true);
            }

            // ── Highlight helpers ───────────────────────────────────────────

            /// <summary> Highlight the column border as a valid drop target. </summary>
            public void SetDropHighlight(bool valid)
            {
                _borderTween.Stop();
                if (_columnBorder != null)
                    _columnBorder.color = valid ? StarChartTheme.HighlightValid : StarChartTheme.HighlightInvalid;
            }

            /// <summary> Restore border to dim color. </summary>
            public void ClearDropHighlight()
            {
                _borderTween.Stop();
                if (_columnBorder != null)
                    _borderTween = Tween.Color(_columnBorder, endValue: _dimColor,
                        duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true);
            }

            /// <summary> Set highlight on a range of cells (for multi-size items). </summary>
            public void SetCellHighlight(int startIndex, int count, bool valid, bool isReplace = false)
            {
                for (int i = startIndex; i < startIndex + count && i < _cells.Length; i++)
                {
                    if (_cells[i] == null) continue;
                    if (isReplace)
                        _cells[i].SetReplaceHighlight();
                    else
                        _cells[i].SetHighlight(valid);
                }
            }

            /// <summary> Clear all cell highlights in this column. </summary>
            public void ClearAllCellHighlights()
            {
                foreach (var cell in _cells)
                    cell?.ClearHighlight();
            }

            private void OnDestroy()
            {
                _borderTween.Stop();
            }
        }

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

        /// <summary> Fired when the track select button is clicked. </summary>
        public event Action<TrackView> OnTrackSelected;

        private WeaponTrack _track;
        private StarChartController _controller;

        // Ordered array for iteration convenience
        private TypeColumn[] _columns;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            _columns = new[] { _sailColumn, _prismColumn, _coreColumn, _satColumn };

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

            // Wire cell click events
            var cells = col.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                var captured = cells[i];
                captured.OnClicked += () => HandleCellClick(captured);
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
        // Refresh helpers
        // =====================================================================

        private void RefreshColumn<T>(TypeColumn col, SlotLayer<T> layer, StarChartItemType layerType)
            where T : StarChartItemSO
        {
            if (col == null) return;
            var cells = col.Cells;

            // Reset all cells to empty
            foreach (var cell in cells)
                cell?.SetEmpty();

            if (layer == null) return;

            int cellIndex = 0;
            var items = layer.Items;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int size = item.SlotSize;

                if (cellIndex < cells.Length && cells[cellIndex] != null)
                    cells[cellIndex].SetItem(item);

                for (int s = 1; s < size; s++)
                {
                    int spanIndex = cellIndex + s;
                    if (spanIndex < cells.Length && cells[spanIndex] != null)
                        cells[spanIndex].SetSpannedBy(item);
                }

                cellIndex += size;
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
        // Cell click
        // =====================================================================

        private void HandleCellClick(SlotCellView cell)
        {
            if (cell.DisplayedItem != null)
                OnCellClicked?.Invoke(cell.DisplayedItem);
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
        /// Supports Core, Prism, LightSail, and Satellite types.
        /// </summary>
        public bool HasSpaceForItem(StarChartItemSO item, bool isCoreLayer)
        {
            if (_track == null) return false;
            return isCoreLayer
                ? _track.CoreLayer.FreeSpace >= item.SlotSize
                : _track.PrismLayer.FreeSpace >= item.SlotSize;
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
