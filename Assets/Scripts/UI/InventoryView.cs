using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Scrollable inventory grid with type filter tabs.
    /// Instantiates <see cref="InventoryItemView"/> cards from a
    /// <see cref="StarChartInventorySO"/> data source.
    /// Uses a custom row-priority packing layout that supports shaped items
    /// spanning multiple grid cells (similar to the HTML prototype's CSS Grid approach).
    /// Also serves as a drop target for unequipping items dragged from slots.
    /// </summary>
    public class InventoryView : MonoBehaviour, IDropHandler
    {
        [Header("UI References")]
        [SerializeField] private Transform _contentParent;
        [SerializeField] private InventoryItemView _itemPrefab;

        [Header("Filter Buttons")]
        [SerializeField] private Button _filterAll;
        [SerializeField] private Button _filterCores;
        [SerializeField] private Button _filterPrisms;
        [SerializeField] private Button _filterSails;
        [SerializeField] private Button _filterSatellites;

        [Header("Grid Layout")]
        [Tooltip("Number of columns in the inventory grid.")]
        [SerializeField] private int _gridColumns = 8;
        [Tooltip("Size of each grid cell in pixels.")]
        [SerializeField] private float _cellSize = 80f;
        [Tooltip("Gap between cells in pixels.")]
        [SerializeField] private float _cellGap = 2f;
        [Tooltip("Padding around the grid in pixels.")]
        [SerializeField] private float _gridPadding = 6f;

        /// <summary> Fired when a player clicks an item in the inventory. </summary>
        public event Action<StarChartItemSO> OnItemSelected;

        /// <summary> Fired when the pointer enters an inventory item card. </summary>
        public event Action<StarChartItemSO> OnItemPointerEntered;

        /// <summary> Fired when the pointer exits an inventory item card. </summary>
        public event Action OnItemPointerExited;

        private StarChartInventorySO _inventory;
        private StarChartItemType? _activeFilter;
        private readonly List<InventoryItemView> _itemViews = new();
        private readonly List<InventoryItemView> _pooledViews = new();

        // 外部注入的装备检查函数
        private Func<StarChartItemSO, bool> _isEquippedCheck;

        // Row-priority packing occupancy grid (dynamically grows rows)
        private bool[,] _occupancy;
        private int _occupancyRows;

        private void Awake()
        {
            if (_filterAll != null)
                _filterAll.onClick.AddListener(() => SetFilter(null));
            if (_filterCores != null)
                _filterCores.onClick.AddListener(() => SetFilter(StarChartItemType.Core));
            if (_filterPrisms != null)
                _filterPrisms.onClick.AddListener(() => SetFilter(StarChartItemType.Prism));
            if (_filterSails != null)
                _filterSails.onClick.AddListener(() => SetFilter(StarChartItemType.LightSail));
            if (_filterSatellites != null)
                _filterSatellites.onClick.AddListener(() => SetFilter(StarChartItemType.Satellite));
        }

        /// <summary> Bind data source and equip-check function. </summary>
        public void Bind(StarChartInventorySO inventory, Func<StarChartItemSO, bool> isEquippedCheck)
        {
            _inventory = inventory;
            _isEquippedCheck = isEquippedCheck;
            UpdateFilterButtonStyles();
        }

        /// <summary> Set active filter. Null = show all. Triggers refresh. </summary>
        public void SetFilter(StarChartItemType? type)
        {
            _activeFilter = type;
            UpdateFilterButtonStyles();
            Refresh();
        }

        /// <summary> Update filter button visual states based on active filter. </summary>
        private void UpdateFilterButtonStyles()
        {
            SetFilterButtonStyle(_filterAll,        null,                          _activeFilter == null);
            SetFilterButtonStyle(_filterCores,      StarChartItemType.Core,        _activeFilter == StarChartItemType.Core);
            SetFilterButtonStyle(_filterPrisms,     StarChartItemType.Prism,       _activeFilter == StarChartItemType.Prism);
            SetFilterButtonStyle(_filterSails,      StarChartItemType.LightSail,   _activeFilter == StarChartItemType.LightSail);
            SetFilterButtonStyle(_filterSatellites, StarChartItemType.Satellite,   _activeFilter == StarChartItemType.Satellite);
        }

        private static void SetFilterButtonStyle(Button btn, StarChartItemType? type, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;

            if (active)
            {
                // Active: use type color (or cyan for ALL)
                Color activeColor = type.HasValue
                    ? StarChartTheme.GetTypeColor(type.Value)
                    : StarChartTheme.Cyan;
                img.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0.35f);
            }
            else
            {
                // Inactive: dim background
                img.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            }
        }

        /// <summary> Rebuild the inventory grid from current data and filter. </summary>
        public void Refresh()
        {
            ClearViews();

            if (_inventory == null || _itemPrefab == null || _contentParent == null)
                return;

            // Disable GridLayoutGroup and ContentSizeFitter if present — we do our own layout
            var gridLayout = _contentParent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null) gridLayout.enabled = false;
            var sizeFitter = _contentParent.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null) sizeFitter.enabled = false;

            // Collect items
            var items = new List<StarChartItemSO>();
            foreach (var item in _inventory.GetByType(_activeFilter))
            {
                if (item != null)
                    items.Add(item);
            }

            // Initialize occupancy grid (C1: shape-cell based, not bounding-box based)
            _occupancyRows = 4;
            _occupancy = new bool[_gridColumns, _occupancyRows];

            // Instantiate and place items
            foreach (var item in items)
            {
                var view = GetOrCreateView();
                bool equipped = _isEquippedCheck?.Invoke(item) ?? false;
                view.Setup(item, equipped);
                view.OnClicked += HandleItemClicked;
                view.OnPointerEntered += HandleItemPointerEntered;
                view.OnPointerExited += HandleItemPointerExited;
                view.gameObject.SetActive(true);
                _itemViews.Add(view);

                // C1: Find anchor using shape cells (not bounding box) for packing
                if (TryFindAnchorForShape(item.Shape, out int placeCol, out int placeRow))
                {
                    // C1: Mark only the actual shape cells as occupied (not the full bounding box)
                    MarkOccupiedByShape(item.Shape, placeCol, placeRow);

                    // Visual sizing: bounding box so card is easy to interact with,
                    // but the card background is transparent — only active cells are colored.
                    var bounds = ItemShapeHelper.GetBounds(item.Shape);
                    var rt = view.GetComponent<RectTransform>();
                    float width  = bounds.x * _cellSize + (bounds.x - 1) * _cellGap;
                    float height = bounds.y * _cellSize + (bounds.y - 1) * _cellGap;
                    rt.sizeDelta = new Vector2(width, height);

                    // Position: top-left anchoring within content parent
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    float x = _gridPadding + placeCol * (_cellSize + _cellGap);
                    float y = -(_gridPadding + placeRow * (_cellSize + _cellGap));
                    rt.anchoredPosition = new Vector2(x, y);
                }
            }

            // Update content height for ScrollRect
            if (_contentParent is RectTransform contentRect)
            {
                int usedRows = GetMaxOccupiedRow() + 1;
                float totalHeight = _gridPadding * 2 + usedRows * _cellSize + Mathf.Max(0, usedRows - 1) * _cellGap;
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
            }

            // Force layout rebuild for ScrollRect
            if (_contentParent is RectTransform rt2)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt2);
                Canvas.ForceUpdateCanvases();
            }
        }

        /// <summary>
        /// C1: Find the first anchor position where the given shape fits in the occupancy grid.
        /// Only the shape's actual cells are checked — not the full bounding box.
        /// Scans row-by-row, column-by-column (top-left priority, matching HTML prototype behavior).
        /// </summary>
        private bool TryFindAnchorForShape(ItemShape shape, out int col, out int row)
        {
            var bounds = ItemShapeHelper.GetBounds(shape);

            for (int r = 0; r < 100; r++) // safety limit
            {
                // Ensure occupancy grid has enough rows for the bounding box
                EnsureOccupancyRows(r + bounds.y);

                // Limit col scan so shape bounding box doesn't exceed grid width
                for (int c = 0; c <= _gridColumns - bounds.x; c++)
                {
                    if (CanPlaceShape(shape, c, r))
                    {
                        col = c;
                        row = r;
                        return true;
                    }
                }
            }

            col = 0;
            row = 0;
            return false;
        }

        /// <summary>
        /// C1: Check if all shape cells, placed at the given anchor, are unoccupied.
        /// Uses GetCells() — the single source of truth — not the bounding box.
        /// </summary>
        private bool CanPlaceShape(ItemShape shape, int anchorCol, int anchorRow)
        {
            foreach (var offset in ItemShapeHelper.GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                // Bounds check
                if (c < 0 || c >= _gridColumns || r < 0 || r >= _occupancyRows)
                    return false;
                if (_occupancy[c, r])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// C1: Mark only the actual shape cells as occupied (not the bounding box).
        /// This is what allows non-rectangular shapes to pack efficiently.
        /// </summary>
        private void MarkOccupiedByShape(ItemShape shape, int anchorCol, int anchorRow)
        {
            foreach (var offset in ItemShapeHelper.GetCells(shape))
            {
                int c = anchorCol + offset.x;
                int r = anchorRow + offset.y;
                if (c >= 0 && c < _gridColumns && r >= 0 && r < _occupancyRows)
                    _occupancy[c, r] = true;
            }
        }

        /// <summary> Grow the occupancy grid if needed. </summary>
        private void EnsureOccupancyRows(int minRows)
        {
            if (minRows <= _occupancyRows) return;

            int newRows = Mathf.Max(minRows, _occupancyRows * 2);
            var newGrid = new bool[_gridColumns, newRows];
            for (int c = 0; c < _gridColumns; c++)
            {
                for (int r = 0; r < _occupancyRows; r++)
                    newGrid[c, r] = _occupancy[c, r];
            }
            _occupancy = newGrid;
            _occupancyRows = newRows;
        }

        /// <summary> Get the highest row index that has any occupied cell. </summary>
        private int GetMaxOccupiedRow()
        {
            int maxRow = 0;
            for (int r = 0; r < _occupancyRows; r++)
            {
                for (int c = 0; c < _gridColumns; c++)
                {
                    if (_occupancy[c, r])
                    {
                        maxRow = r;
                        break;
                    }
                }
            }
            return maxRow;
        }

        private void HandleItemClicked(StarChartItemSO item)
        {
            OnItemSelected?.Invoke(item);
        }

        private void HandleItemPointerEntered(StarChartItemSO item)
        {
            OnItemPointerEntered?.Invoke(item);
        }

        private void HandleItemPointerExited()
        {
            OnItemPointerExited?.Invoke();
        }

        private void ClearViews()
        {
            // Return active views to pool
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].OnClicked -= HandleItemClicked;
                    _itemViews[i].OnPointerEntered -= HandleItemPointerEntered;
                    _itemViews[i].OnPointerExited -= HandleItemPointerExited;
                    _itemViews[i].transform.localScale = Vector3.one;
                    _itemViews[i].gameObject.SetActive(false);
                    _pooledViews.Add(_itemViews[i]);
                }
            }
            _itemViews.Clear();
        }

        private void OnDestroy()
        {
            ClearViews();
            for (int i = 0; i < _pooledViews.Count; i++)
            {
                if (_pooledViews[i] != null)
                    Destroy(_pooledViews[i].gameObject);
            }
            _pooledViews.Clear();
        }

        private InventoryItemView GetOrCreateView()
        {
            InventoryItemView view = null;

            if (_pooledViews.Count > 0)
            {
                int last = _pooledViews.Count - 1;
                view = _pooledViews[last];
                _pooledViews.RemoveAt(last);
                if (view != null)
                {
                    view.transform.SetParent(_contentParent, false);
                    view.transform.SetAsLastSibling();
                    view.transform.localScale = Vector3.one;
                    view.gameObject.SetActive(true);
                }
            }

            if (view == null)
                view = Instantiate(_itemPrefab, _contentParent);

            return view;
        }

        // ========== Drop Target for Unequip (Slot → Inventory) ==========

        /// <summary>
        /// When an equipped item is dragged from a slot onto the inventory area,
        /// trigger the unequip operation via DragDropManager.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr == null || !mgr.IsDragging) return;

            if (mgr.CurrentPayload?.Source == DragSource.Slot)
            {
                mgr.ExecuteUnequipDrop();
                mgr.EndDrag(false); // false because ExecuteUnequipDrop already handled it
            }
        }
    }
}
