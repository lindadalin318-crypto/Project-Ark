using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Root controller for the Star Chart editing panel.
    /// Orchestrates track views, inventory, detail panel, and equip/unequip logic.
    /// </summary>
    public class StarChartPanel : MonoBehaviour
    {
        [Header("Track Views")]
        [SerializeField] private TrackView _primaryTrackView;
        [SerializeField] private TrackView _secondaryTrackView;

        [Header("Inventory & Detail")]
        [SerializeField] private InventoryView _inventoryView;
        [SerializeField] private ItemDetailView _itemDetailView;

        [Header("Drag & Drop")]
        [SerializeField] private DragDropManager _dragDropManager;

        [Header("Status Bar")]
        [SerializeField] private StatusBarView _statusBar;

        [Header("Panel Animation")]
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        private StarChartController _controller;
        private StarChartInventorySO _inventory;
        private WeaponTrack _selectedTrack;

        /// <summary> Exposes the status bar for DragDropManager to show messages. </summary>
        public StatusBarView StatusBar => _statusBar;

        /// <summary> Exposes the controller for DragDropManager SAIL/SAT operations. </summary>
        public StarChartController Controller => _controller;

        // 当前在详情面板中选中的物品
        private StarChartItemSO _selectedItem;

        /// <summary> Fired when panel opens. </summary>
        public event Action OnOpened;

        /// <summary> Fired when panel closes. </summary>
        public event Action OnClosed;

        /// <summary> Bind to game systems. Call once during initialization. </summary>
        public void Bind(StarChartController controller, StarChartInventorySO inventory)
        {
            _controller = controller;
            _inventory = inventory;

            // 绑定轨道视图
            if (_primaryTrackView != null)
            {
                _primaryTrackView.Bind(controller.PrimaryTrack, controller);
                _primaryTrackView.OnTrackSelected += HandleTrackSelected;
                _primaryTrackView.OnCellClicked += HandleCellClicked;
            }

            if (_secondaryTrackView != null)
            {
                _secondaryTrackView.Bind(controller.SecondaryTrack, controller);
                _secondaryTrackView.OnTrackSelected += HandleTrackSelected;
                _secondaryTrackView.OnCellClicked += HandleCellClicked;
            }

            // 绑定库存
            if (_inventoryView != null)
            {
                _inventoryView.Bind(inventory, IsItemEquipped);
                _inventoryView.OnItemSelected += HandleInventoryItemSelected;
            }

            // 绑定详情
            if (_itemDetailView != null)
            {
                _itemDetailView.OnActionClicked += HandleActionClicked;
            }

            // 默认选中主轨道
            _selectedTrack = controller.PrimaryTrack;
            UpdateTrackSelection();

            // Initialize drag-and-drop manager
            if (_dragDropManager != null)
                _dragDropManager.Bind(this, controller);
        }

        /// <summary> Open the panel and refresh all views. </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            _selectedItem = null;
            RefreshAll();
            OnOpened?.Invoke();

            // Panel open animation: scale 0.95 → 1.0 + alpha 0 → 1 (200ms OutQuad)
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.alpha = 0f;
                var rt = GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one * 0.95f;

                Sequence.Create()
                    .Group(Tween.Alpha(_panelCanvasGroup, endValue: 1f,
                        duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true))
                    .Group(rt != null
                        ? Tween.Scale(rt, endValue: Vector3.one,
                            duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true)
                        : Tween.Delay(0f))
                    .ChainCallback(() => { if (_panelCanvasGroup != null) _panelCanvasGroup.interactable = true; });
            }
        }

        /// <summary> Close the panel. </summary>
        public void Close()
        {
            // Cancel any in-progress drag operation
            if (_dragDropManager != null)
                _dragDropManager.CancelDrag();
            else if (DragDropManager.Instance != null)
                DragDropManager.Instance.CancelDrag();

            // Panel close animation: scale 1.0 → 0.95 + alpha 1 → 0 (150ms InQuad)
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.interactable = false;
                var rt = GetComponent<RectTransform>();

                Sequence.Create()
                    .Group(Tween.Alpha(_panelCanvasGroup, endValue: 0f,
                        duration: 0.15f, ease: Ease.InQuad, useUnscaledTime: true))
                    .Group(rt != null
                        ? Tween.Scale(rt, endValue: Vector3.one * 0.95f,
                            duration: 0.15f, ease: Ease.InQuad, useUnscaledTime: true)
                        : Tween.Delay(0f))
                    .ChainCallback(() =>
                    {
                        gameObject.SetActive(false);
                        OnClosed?.Invoke();
                    });
            }
            else
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
            }
        }

        /// <summary> Is the panel currently visible? </summary>
        public bool IsOpen => gameObject.activeSelf;

        private void RefreshAll()
        {
            _primaryTrackView?.Refresh();
            _secondaryTrackView?.Refresh();
            _inventoryView?.Refresh();

            if (_itemDetailView != null)
            {
                if (_selectedItem != null)
                    _itemDetailView.ShowItem(_selectedItem, IsItemEquipped(_selectedItem));
                else
                    _itemDetailView.Clear();
            }
        }

        /// <summary>
        /// Public entry point for refreshing all views. Called by DragDropManager after equip/unequip.
        /// </summary>
        public void RefreshAllViews()
        {
            RefreshAll();
        }

        /// <summary>
        /// Select an item and show it in the detail panel. Called by DragDropManager after successful drop.
        /// </summary>
        public void SelectAndShowItem(StarChartItemSO item)
        {
            if (item == null) return;
            _selectedItem = item;
            bool equipped = IsItemEquipped(item);
            _itemDetailView?.ShowItem(item, equipped);
        }

        private void HandleTrackSelected(TrackView trackView)
        {
            _selectedTrack = trackView.Track;
            UpdateTrackSelection();
        }

        private void UpdateTrackSelection()
        {
            bool primarySelected = _selectedTrack == _controller?.PrimaryTrack;
            _primaryTrackView?.SetSelected(primarySelected);
            _secondaryTrackView?.SetSelected(!primarySelected);
        }

        private void HandleInventoryItemSelected(StarChartItemSO item)
        {
            _selectedItem = item;
            bool equipped = IsItemEquipped(item);
            _itemDetailView?.ShowItem(item, equipped);
        }

        private void HandleCellClicked(StarChartItemSO item)
        {
            // 点击已装备的格子 → 在详情面板显示（已装备状态）
            _selectedItem = item;
            _itemDetailView?.ShowItem(item, true);
        }

        private void HandleActionClicked(StarChartItemSO item, bool isEquip)
        {
            if (_controller == null) return;

            if (isEquip)
                EquipItem(item);
            else
                UnequipItem(item);

            RefreshAll();
        }

        private void EquipItem(StarChartItemSO item)
        {
            if (_selectedTrack == null) return;

            switch (item)
            {
                case StarCoreSO core:
                    if (!_selectedTrack.EquipCore(core))
                    {
                        Debug.LogWarning($"[StarChartPanel] 无法装备核心 '{core.DisplayName}'：空间不足");
                        _statusBar?.ShowMessage($"NO SPACE: {core.DisplayName}", StarChartTheme.StatusError, 3f);
                    }
                    else
                    {
                        _selectedTrack.InitializePools();
                        _statusBar?.ShowMessage($"EQUIPPED: {core.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    }
                    break;

                case PrismSO prism:
                    if (!_selectedTrack.EquipPrism(prism))
                    {
                        Debug.LogWarning($"[StarChartPanel] 无法装备棱镜 '{prism.DisplayName}'：空间不足");
                        _statusBar?.ShowMessage($"NO SPACE: {prism.DisplayName}", StarChartTheme.StatusError, 3f);
                    }
                    else
                    {
                        _statusBar?.ShowMessage($"EQUIPPED: {prism.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    }
                    break;

                case LightSailSO sail:
                    _controller.EquipLightSail(sail);
                    _statusBar?.ShowMessage($"EQUIPPED: {sail.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;

                case SatelliteSO sat:
                    _controller.EquipSatellite(sat);
                    _statusBar?.ShowMessage($"EQUIPPED: {sat.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;
            }
        }

        private void UnequipItem(StarChartItemSO item)
        {
            switch (item)
            {
                case StarCoreSO core:
                    if (!_controller.PrimaryTrack.UnequipCore(core))
                        _controller.SecondaryTrack.UnequipCore(core);
                    _statusBar?.ShowMessage($"UNEQUIPPED: {core.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;

                case PrismSO prism:
                    if (!_controller.PrimaryTrack.UnequipPrism(prism))
                        _controller.SecondaryTrack.UnequipPrism(prism);
                    _statusBar?.ShowMessage($"UNEQUIPPED: {prism.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;

                case LightSailSO:
                    _controller.UnequipLightSail();
                    _statusBar?.ShowMessage($"UNEQUIPPED: {item.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;

                case SatelliteSO sat:
                    _controller.UnequipSatellite(sat);
                    _statusBar?.ShowMessage($"UNEQUIPPED: {sat.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;
            }
        }

        /// <summary> Check if an item is currently equipped in any track/slot. </summary>
        private bool IsItemEquipped(StarChartItemSO item)
        {
            if (_controller == null) return false;

            switch (item)
            {
                case StarCoreSO core:
                    return ListContains(_controller.PrimaryTrack.CoreLayer.Items, core)
                        || ListContains(_controller.SecondaryTrack.CoreLayer.Items, core);

                case PrismSO prism:
                    return ListContains(_controller.PrimaryTrack.PrismLayer.Items, prism)
                        || ListContains(_controller.SecondaryTrack.PrismLayer.Items, prism);

                case LightSailSO sail:
                    return _controller.GetEquippedLightSail() == sail;

                case SatelliteSO sat:
                    var satellites = _controller.GetEquippedSatellites();
                    if (satellites != null)
                    {
                        for (int i = 0; i < satellites.Count; i++)
                        {
                            if (satellites[i] == sat) return true;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary> Helper: IReadOnlyList does not have Contains, so we iterate manually. </summary>
        private static bool ListContains<T>(System.Collections.Generic.IReadOnlyList<T> list, T target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], target)) return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (_primaryTrackView != null)
            {
                _primaryTrackView.OnTrackSelected -= HandleTrackSelected;
                _primaryTrackView.OnCellClicked -= HandleCellClicked;
            }

            if (_secondaryTrackView != null)
            {
                _secondaryTrackView.OnTrackSelected -= HandleTrackSelected;
                _secondaryTrackView.OnCellClicked -= HandleCellClicked;
            }

            if (_inventoryView != null)
                _inventoryView.OnItemSelected -= HandleInventoryItemSelected;

            if (_itemDetailView != null)
                _itemDetailView.OnActionClicked -= HandleActionClicked;
        }
    }
}
