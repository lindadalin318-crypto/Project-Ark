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

        /// <summary> Fired when the pointer enters an item in the inventory. </summary>
        public event Action<StarChartItemSO> OnItemPointerEntered;

        /// <summary> Fired when the pointer exits an item in the inventory. </summary>
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
            Debug.Log($"[InventoryView] Bind called. inventory={inventory?.name ?? "NULL"}, ownedItems={inventory?.OwnedItems?.Count ?? -1}");
        }

        /// <summary> Set active filter. Null = show all. Triggers refresh. </summary>
        public void SetFilter(StarChartItemType? type)
        {
            _activeFilter = type;
            Refresh();
        }

        /// <summary> Rebuild the inventory grid from current data and filter. </summary>
        public void Refresh()
        {
            ClearViews();

            if (_inventory == null || _itemPrefab == null || _contentParent == null)
            {
                Debug.LogWarning($"[InventoryView] Refresh aborted: inventory={(_inventory != null ? "OK" : "NULL")}, itemPrefab={(_itemPrefab != null ? "OK" : "NULL")}, contentParent={(_contentParent != null ? "OK" : "NULL")}");
                return;
            }

            int count = 0;
            foreach (var item in _inventory.GetByType(_activeFilter))
            {
                if (item == null) { Debug.LogWarning("[InventoryView] Skipping null item in inventory"); continue; }

                var view = Instantiate(_itemPrefab, _contentParent);
                bool equipped = _isEquippedCheck?.Invoke(item) ?? false;
                view.Setup(item, equipped);
                view.OnClicked += HandleItemClicked;
                view.OnPointerEntered += (i) => OnItemPointerEntered?.Invoke(i);
                view.OnPointerExited += () => OnItemPointerExited?.Invoke();
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

            Debug.Log($"[InventoryView] Refresh done. Filter={_activeFilter?.ToString() ?? "ALL"}, instantiated {count} items. contentParent.childCount={_contentParent.childCount}");
        }

        private void HandleItemClicked(StarChartItemSO item)
        {
            OnItemSelected?.Invoke(item);
        }

        private void ClearViews()
        {
            // Unsubscribe events from tracked views
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                    _itemViews[i].OnClicked -= HandleItemClicked;
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
