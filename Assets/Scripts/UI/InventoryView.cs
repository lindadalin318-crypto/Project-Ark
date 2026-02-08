using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Scrollable inventory grid with type filter tabs.
    /// Instantiates <see cref="InventoryItemView"/> cards from a
    /// <see cref="StarChartInventorySO"/> data source.
    /// </summary>
    public class InventoryView : MonoBehaviour
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
                return;

            foreach (var item in _inventory.GetByType(_activeFilter))
            {
                if (item == null) continue;

                var view = Instantiate(_itemPrefab, _contentParent);
                bool equipped = _isEquippedCheck?.Invoke(item) ?? false;
                view.Setup(item, equipped);
                view.OnClicked += HandleItemClicked;
                view.gameObject.SetActive(true);
                _itemViews.Add(view);
            }
        }

        private void HandleItemClicked(StarChartItemSO item)
        {
            OnItemSelected?.Invoke(item);
        }

        private void ClearViews()
        {
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].OnClicked -= HandleItemClicked;
                    Destroy(_itemViews[i].gameObject);
                }
            }
            _itemViews.Clear();
        }

        private void OnDestroy()
        {
            ClearViews();
        }
    }
}
