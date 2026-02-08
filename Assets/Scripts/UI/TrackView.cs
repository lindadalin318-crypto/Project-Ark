using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Visual representation of one <see cref="WeaponTrack"/> (Primary or Secondary).
    /// Displays the sandwich layout: 3 prism cells (upper) + 3 core cells (lower).
    /// Subscribes to <see cref="WeaponTrack.OnLoadoutChanged"/> for reactive refresh.
    /// </summary>
    public class TrackView : MonoBehaviour
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

        private WeaponTrack _track;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => OnTrackSelected?.Invoke(this));

            // 给每个 cell 注册点击事件
            for (int i = 0; i < _prismCells.Length; i++)
            {
                int index = i;
                if (_prismCells[i] != null)
                    _prismCells[i].OnClicked += () => HandleCellClick(_prismCells[index]);
            }

            for (int i = 0; i < _coreCells.Length; i++)
            {
                int index = i;
                if (_coreCells[i] != null)
                    _coreCells[i].OnClicked += () => HandleCellClick(_coreCells[index]);
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
            // 先全部清空
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetEmpty();
            }

            if (layer == null) return;

            // 遍历装备，根据 SlotSize 映射到格子
            int cellIndex = 0;
            var items = layer.Items;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int size = item.SlotSize;

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

        private void OnDestroy()
        {
            if (_track != null)
                _track.OnLoadoutChanged -= Refresh;
        }
    }
}
