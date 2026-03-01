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

        /// <summary> Fired when a player clicks an item in the inventory. </summary>
        public event Action<StarChartItemSO> OnItemSelected;

        /// <summary> Fired when the pointer enters an inventory item card. </summary>
        public event Action<StarChartItemSO> OnItemPointerEntered;

        /// <summary> Fired when the pointer exits an inventory item card. </summary>
        public event Action OnItemPointerExited;

        private StarChartInventorySO _inventory;
        private StarChartItemType? _activeFilter;
        private readonly List<InventoryItemView> _itemViews = new();

        // 外部注入的装备检查函数
        private Func<StarChartItemSO, bool> _isEquippedCheck;

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

            int count = 0;
            foreach (var item in _inventory.GetByType(_activeFilter))
            {
                if (item == null) continue;

                var view = Instantiate(_itemPrefab, _contentParent);
                bool equipped = _isEquippedCheck?.Invoke(item) ?? false;
                view.Setup(item, equipped);
                view.OnClicked += HandleItemClicked;
                view.OnPointerEntered += HandleItemPointerEntered;
                view.OnPointerExited += HandleItemPointerExited;
                view.gameObject.SetActive(true);
                _itemViews.Add(view);
                count++;
            }
            // Force layout rebuild immediately — GridLayoutGroup + ContentSizeFitter
            // won't auto-update when timeScale == 0
            if (_contentParent is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                Canvas.ForceUpdateCanvases();
            }


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
            // Unsubscribe events from tracked views
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].OnClicked -= HandleItemClicked;
                    _itemViews[i].OnPointerEntered -= HandleItemPointerEntered;
                    _itemViews[i].OnPointerExited -= HandleItemPointerExited;
                }
            }
            _itemViews.Clear();

            // DestroyImmediate all children to avoid ghost objects
            // (Destroy is deferred and unreliable when timeScale == 0)
            if (_contentParent != null)
            {
                for (int i = _contentParent.childCount - 1; i >= 0; i--)
                    DestroyImmediate(_contentParent.GetChild(i).gameObject);
            }
        }

        private void OnDestroy()
        {
            ClearViews();
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
