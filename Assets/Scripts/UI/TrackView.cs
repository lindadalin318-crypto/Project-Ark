using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Visual representation of one <see cref="WeaponTrack"/> (Primary or Secondary).
    /// Displays 4 columns: SAIL (1 slot) / PRISM (3 slots) / CORE (3 slots) / SAT (2 slots).
    /// Subscribes to <see cref="WeaponTrack.OnLoadoutChanged"/> for reactive refresh.
    /// </summary>
    public class TrackView : MonoBehaviour, IPointerEnterHandler
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _trackLabel;
        [SerializeField] private TMP_Text _prismLabel;
        [SerializeField] private TMP_Text _coreLabel;
        [SerializeField] private TMP_Text _sailLabel;
        [SerializeField] private TMP_Text _satLabel;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _selectionBorder;

        [Header("Slot Cells — Core / Prism")]
        [SerializeField] private SlotCellView[] _prismCells = new SlotCellView[3];
        [SerializeField] private SlotCellView[] _coreCells = new SlotCellView[3];

        [Header("Slot Cells — SAIL / SAT")]
        [SerializeField] private SlotCellView _sailCell;
        [SerializeField] private SlotCellView[] _satCells = new SlotCellView[2];

        /// <summary> Fired when a cell with an equipped item is clicked (for unequip). </summary>
        public event Action<StarChartItemSO> OnCellClicked;

        /// <summary> Fired when the track select button is clicked. </summary>
        public event Action<TrackView> OnTrackSelected;

        private WeaponTrack _track;
        private StarChartController _controller;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            // Core cells
            for (int i = 0; i < _coreCells.Length; i++)
            {
                int index = i;
                if (_coreCells[i] != null)
                {
                    _coreCells[i].SlotType = SlotType.Core;
                    _coreCells[i].CellIndex = i;
                    _coreCells[i].OwnerTrack = this;
                    _coreCells[i].OnClicked += () => HandleCellClick(_coreCells[index]);
                }
            }

            // Prism cells
            for (int i = 0; i < _prismCells.Length; i++)
            {
                int index = i;
                if (_prismCells[i] != null)
                {
                    _prismCells[i].SlotType = SlotType.Prism;
                    _prismCells[i].CellIndex = i;
                    _prismCells[i].OwnerTrack = this;
                    _prismCells[i].OnClicked += () => HandleCellClick(_prismCells[index]);
                }
            }

            // SAIL cell
            if (_sailCell != null)
            {
                _sailCell.SlotType = SlotType.LightSail;
                _sailCell.CellIndex = 0;
                _sailCell.OwnerTrack = this;
                _sailCell.HasSpaceForItem = (item) => HasSpaceForSail(item);
                _sailCell.OnClicked += () => HandleCellClick(_sailCell);
            }

            // SAT cells
            for (int i = 0; i < _satCells.Length; i++)
            {
                int index = i;
                if (_satCells[i] != null)
                {
                    _satCells[i].SlotType = SlotType.Satellite;
                    _satCells[i].CellIndex = i;
                    _satCells[i].OwnerTrack = this;
                    _satCells[i].HasSpaceForItem = (item) => HasSpaceForSat(item);
                    _satCells[i].OnClicked += () => HandleCellClick(_satCells[index]);
                }
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

                if (_prismLabel != null)
                {
                    _prismLabel.text = "PRISM";
                    _prismLabel.color = StarChartTheme.PrismColor;
                }
                if (_coreLabel != null)
                {
                    _coreLabel.text = "CORE";
                    _coreLabel.color = StarChartTheme.CoreColor;
                }
                if (_sailLabel != null)
                {
                    _sailLabel.text = "SAIL";
                    _sailLabel.color = StarChartTheme.SailColor;
                }
                if (_satLabel != null)
                {
                    _satLabel.text = "SAT";
                    _satLabel.color = StarChartTheme.SatColor;
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

        /// <summary>
        /// Refresh all cells from current track loadout.
        /// </summary>
        public void Refresh()
        {
            RefreshLayer(_coreCells, _track?.CoreLayer, StarChartItemType.Core);
            RefreshLayer(_prismCells, _track?.PrismLayer, StarChartItemType.Prism);
            RefreshSailCell();
            RefreshSatCells();
        }

        private void RefreshLayer<T>(SlotCellView[] cells, SlotLayer<T> layer, StarChartItemType layerType) where T : StarChartItemSO
        {
            if (cells == null || cells.Length == 0) return;

            Color typeColor = StarChartTheme.GetTypeColor(layerType);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetThemeColor(typeColor);
            }

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetEmpty();
            }

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

        private void RefreshSailCell()
        {
            if (_sailCell == null) return;

            if (_controller == null)
                Debug.LogWarning("[TrackView] _controller is null, SAIL slot will be empty.");

            _sailCell.SetThemeColor(StarChartTheme.SailColor);

            var sail = _controller?.GetEquippedLightSail();
            if (sail != null)
                _sailCell.SetItem(sail);
            else
                _sailCell.SetEmpty();
        }

        private void RefreshSatCells()
        {
            if (_satCells == null) return;

            if (_controller == null)
                Debug.LogWarning("[TrackView] _controller is null, SAT slots will be empty.");

            var sats = _controller?.GetEquippedSatellites();

            for (int i = 0; i < _satCells.Length; i++)
            {
                if (_satCells[i] == null) continue;
                _satCells[i].SetThemeColor(StarChartTheme.SatColor);

                if (sats != null && i < sats.Count)
                    _satCells[i].SetItem(sats[i]);
                else
                    _satCells[i].SetEmpty();
            }
        }

        private void HandleCellClick(SlotCellView cell)
        {
            if (cell.DisplayedItem != null)
                OnCellClicked?.Invoke(cell.DisplayedItem);
        }

        // ========== Drag-and-Drop Support ==========

        public void OnPointerEnter(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging)
            {
                OnTrackSelected?.Invoke(this);
            }
        }

        /// <summary>
        /// Check whether this track's layer has enough space for the given item.
        /// Supports Core, Prism, LightSail, and Satellite types.
        /// </summary>
        public bool HasSpaceForItem(StarChartItemSO item, bool isCoreLayer)
        {
            if (_track == null) return false;

            if (isCoreLayer)
                return _track.CoreLayer.FreeSpace >= item.SlotSize;
            else
                return _track.PrismLayer.FreeSpace >= item.SlotSize;
        }

        private bool HasSpaceForSail(StarChartItemSO item)
        {
            if (_controller == null) return true; // assume space if no controller
            return _controller.GetEquippedLightSail() == null;
        }

        private bool HasSpaceForSat(StarChartItemSO item)
        {
            if (_controller == null) return true;
            var sats = _controller.GetEquippedSatellites();
            return sats == null || sats.Count < _satCells.Length;
        }

        /// <summary>
        /// Highlight multiple consecutive cells for a SlotSize > 1 item preview.
        /// Supports replace (orange) highlight.
        /// </summary>
        public void SetMultiCellHighlight(int startIndex, int count, bool isCoreLayer, bool valid, bool isReplace = false)
        {
            var cells = isCoreLayer ? _coreCells : _prismCells;

            for (int i = startIndex; i < startIndex + count && i < cells.Length; i++)
            {
                if (cells[i] == null) continue;

                if (isReplace)
                    cells[i].SetReplaceHighlight();
                else
                    cells[i].SetHighlight(valid);
            }
        }

        /// <summary>
        /// Clear all drag highlights on every cell in all layers.
        /// </summary>
        public void ClearAllHighlights()
        {
            for (int i = 0; i < _coreCells.Length; i++)
                _coreCells[i]?.ClearHighlight();

            for (int i = 0; i < _prismCells.Length; i++)
                _prismCells[i]?.ClearHighlight();

            _sailCell?.ClearHighlight();

            for (int i = 0; i < _satCells.Length; i++)
                _satCells[i]?.ClearHighlight();
        }

        private void OnDestroy()
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;
        }
    }
}
