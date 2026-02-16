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
    /// Displays the sandwich layout: 3 prism cells (upper) + 3 core cells (lower).
    /// Subscribes to <see cref="WeaponTrack.OnLoadoutChanged"/> for reactive refresh.
    /// Auto-selects this track when a dragged item enters its area.
    /// </summary>
    public class TrackView : MonoBehaviour, IPointerEnterHandler
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _trackLabel;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _selectionBorder;

        [Header("Slot Cells")]
        [SerializeField] private SlotCellView[] _prismCells = new SlotCellView[3];
        [SerializeField] private SlotCellView[] _coreCells = new SlotCellView[3];

        [Header("Colors")]
        [SerializeField] private Color _selectedBorderColor = new(0.4f, 0.8f, 1f, 1f);
        [SerializeField] private Color _unselectedBorderColor = new(0.3f, 0.3f, 0.4f, 0.5f);

        /// <summary> Fired when a cell with an equipped item is clicked (for unequip). </summary>
        public event Action<StarChartItemSO> OnCellClicked;

        /// <summary> Fired when the track select button is clicked. </summary>
        public event Action<TrackView> OnTrackSelected;

        /// <summary> Fired when the pointer enters a cell with an item. </summary>
        public event Action<StarChartItemSO> OnCellPointerEntered;

        /// <summary> Fired when the pointer exits a cell. </summary>
        public event Action OnCellPointerExited;

        private WeaponTrack _track;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            // Initialize cell properties for drag-and-drop and register click events
            for (int i = 0; i < _prismCells.Length; i++)
            {
                int index = i;
                if (_prismCells[i] != null)
                {
                    _prismCells[i].IsCoreCell = false;
                    _prismCells[i].CellIndex = i;
                    _prismCells[i].OwnerTrack = this;
                    _prismCells[i].OnClicked += () => HandleCellClick(_prismCells[index]);
                    _prismCells[i].OnPointerEntered += (item) => OnCellPointerEntered?.Invoke(item);
                    _prismCells[i].OnPointerExited += () => OnCellPointerExited?.Invoke();
                }
            }

            for (int i = 0; i < _coreCells.Length; i++)
            {
                int index = i;
                if (_coreCells[i] != null)
                {
                    _coreCells[i].IsCoreCell = true;
                    _coreCells[i].CellIndex = i;
                    _coreCells[i].OwnerTrack = this;
                    _coreCells[i].OnClicked += () => HandleCellClick(_coreCells[index]);
                    _coreCells[i].OnPointerEntered += (item) => OnCellPointerEntered?.Invoke(item);
                    _coreCells[i].OnPointerExited += () => OnCellPointerExited?.Invoke();
                }
            }
        }

        /// <summary> Bind to a WeaponTrack and subscribe to loadout changes. </summary>
        public void Bind(WeaponTrack track)
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;

            _track = track;

            if (_track != null)
            {
                _track.OnLoadoutChanged += Refresh;

                if (_trackLabel != null)
                    _trackLabel.text = _track.Id == WeaponTrack.TrackId.Primary
                        ? "PRIMARY" : "SECONDARY";
            }

            Refresh();
        }

        /// <summary> The bound WeaponTrack. </summary>
        public WeaponTrack Track => _track;

        /// <summary> Set visual selection state (border highlight). </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
                _selectionBorder.color = selected ? _selectedBorderColor : _unselectedBorderColor;
        }

        /// <summary>
        /// Refresh all cells from current track loadout.
        /// Maps items to cells based on SlotSize.
        /// </summary>
        public void Refresh()
        {
            RefreshLayer(_coreCells, _track?.CoreLayer);
            RefreshLayer(_prismCells, _track?.PrismLayer);
        }

        private void RefreshLayer<T>(SlotCellView[] cells, SlotLayer<T> layer) where T : StarChartItemSO
        {
            // Check for null/empty cell array
            if (cells == null || cells.Length == 0)
            {
                Debug.LogWarning($"[TrackView] RefreshLayer: cells array is null or empty on '{gameObject.name}'");
                return;
            }

            // 先全部清空
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetEmpty();
                else
                    Debug.LogWarning($"[TrackView] Cell[{i}] is null on '{gameObject.name}' — check Inspector references");
            }

            if (layer == null)
            {
                Debug.LogWarning($"[TrackView] RefreshLayer: layer is null on '{gameObject.name}'");
                return;
            }

            // 遍历装备，根据 SlotSize 映射到格子
            int cellIndex = 0;
            var items = layer.Items;

            Debug.Log($"[TrackView] RefreshLayer on '{gameObject.name}': {items.Count} item(s) equipped");

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int size = item.SlotSize;

                Debug.Log($"[TrackView]   → item[{i}]: '{item.DisplayName}' (size={size}, icon={item.Icon != null}), placing at cell {cellIndex}");

                // 第一个格子显示图标
                if (cellIndex < cells.Length && cells[cellIndex] != null)
                    cells[cellIndex].SetItem(item);

                // 后续格子标记为被占用
                for (int s = 1; s < size; s++)
                {
                    int spanIndex = cellIndex + s;
                    if (spanIndex < cells.Length && cells[spanIndex] != null)
                        cells[spanIndex].SetSpannedBy(item);
                }

                cellIndex += size;
            }
        }

        private void HandleCellClick(SlotCellView cell)
        {
            if (cell.DisplayedItem != null)
                OnCellClicked?.Invoke(cell.DisplayedItem);
        }

        // ========== Drag-and-Drop Support ==========

        /// <summary>
        /// Auto-select this track when a dragged item enters its area.
        /// </summary>
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
        /// </summary>
        public bool HasSpaceForItem(StarChartItemSO item, bool isCoreLayer)
        {
            if (_track == null) return false;

            if (isCoreLayer)
                return _track.CoreLayer.FreeSpace >= item.SlotSize;
            else
                return _track.PrismLayer.FreeSpace >= item.SlotSize;
        }

        /// <summary>
        /// Highlight multiple consecutive cells for a SlotSize > 1 item preview.
        /// </summary>
        public void SetMultiCellHighlight(int startIndex, int count, bool isCoreLayer, bool valid)
        {
            var cells = isCoreLayer ? _coreCells : _prismCells;

            for (int i = startIndex; i < startIndex + count && i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetHighlight(valid);
            }
        }

        /// <summary>
        /// Clear all drag highlights on every cell in both layers.
        /// </summary>
        public void ClearAllHighlights()
        {
            for (int i = 0; i < _coreCells.Length; i++)
            {
                if (_coreCells[i] != null)
                    _coreCells[i].ClearHighlight();
            }

            for (int i = 0; i < _prismCells.Length; i++)
            {
                if (_prismCells[i] != null)
                    _prismCells[i].ClearHighlight();
            }
        }

        private void OnDestroy()
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;
        }
    }
}
