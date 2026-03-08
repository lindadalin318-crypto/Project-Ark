using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Root controller for the Star Chart editing panel.
    /// Orchestrates Loadout card (PRIMARY + SECONDARY tracks), inventory, detail panel,
    /// Loadout switcher, and equip/unequip logic.
    /// </summary>
    public class StarChartPanel : MonoBehaviour
    {
        [Header("Track Views")]
        [SerializeField] private TrackView _primaryTrackView;
        [SerializeField] private TrackView _secondaryTrackView;

        [Header("Shared SAIL Column")]
        [SerializeField] private TypeColumn _sharedSailColumn;
        [SerializeField] private DragHighlightLayer _sailHighlightLayer;
        [Tooltip("Override SAIL layer column count for debugging. 0 = use save data. Range 1-4.")]
        [SerializeField] [Range(0, 4)] private int _debugSailCols = 0;

        [Header("Loadout")]
        [SerializeField] private LoadoutSwitcher _loadoutSwitcher;
        [SerializeField] private RectTransform _loadoutCard;

        [Header("Inventory & Detail")]
        [SerializeField] private InventoryView _inventoryView;
        [SerializeField] private ItemDetailView _itemDetailView;

        [Header("Drag & Drop")]
        [SerializeField] private DragDropManager _dragDropManager;

        [Header("Tooltip")]
        [SerializeField] private ItemTooltipView _tooltipView;

        [Header("Status Bar")]
        [SerializeField] private StatusBarView _statusBar;

        [Header("Panel Animation")]
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        private StarChartController _controller;
        private StarChartInventorySO _inventory;
        private WeaponTrack _selectedTrack;

        // Tracks whether the panel is logically open (replaces gameObject.activeSelf check)
        private bool _isOpen = false;

        /// <summary> Exposes the status bar for DragDropManager to show messages. </summary>
        public StatusBarView StatusBar => _statusBar;

        /// <summary> Exposes the controller for DragDropManager SAIL/SAT operations. </summary>
        public StarChartController Controller => _controller;

        // =====================================================================
        // Tooltip API
        // =====================================================================

        /// <summary>
        /// Show tooltip for the given item. Called by InventoryItemView and SlotCellView.
        /// </summary>
        public void ShowTooltip(StarChartItemSO item, bool isEquipped, string equippedLocation = "")
        {
            _tooltipView?.ShowTooltip(item, isEquipped, equippedLocation);
        }

        /// <summary> Hide the tooltip immediately. </summary>
        public void HideTooltip()
        {
            _tooltipView?.HideTooltip();
        }

        /// <summary>
        /// Build the equipped location string for a given item.
        /// e.g. "PRIMARY · CORE" or "SECONDARY · PRISM".
        /// </summary>
        public string GetEquippedLocation(StarChartItemSO item)
        {
            if (_controller == null) return string.Empty;

            string trackName = string.Empty;
            string typeName = string.Empty;

            switch (item)
            {
                case StarCoreSO core:
                    typeName = "CORE";
                    if (ListContains(_controller.PrimaryTrack.CoreLayer.Items, core))
                        trackName = "PRIMARY";
                    else if (ListContains(_controller.SecondaryTrack.CoreLayer.Items, core))
                        trackName = "SECONDARY";
                    break;

                case PrismSO prism:
                    typeName = "PRISM";
                    if (ListContains(_controller.PrimaryTrack.PrismLayer.Items, prism))
                        trackName = "PRIMARY";
                    else if (ListContains(_controller.SecondaryTrack.PrismLayer.Items, prism))
                        trackName = "SECONDARY";
                    break;

                case LightSailSO:
                    typeName = "SAIL";
                    trackName = "SHARED";
                    break;

                case SatelliteSO:
                    typeName = "SAT";
                    // SAT is now Per-Track: check both tracks
                    if (ListContains(_controller.PrimaryTrack.EquippedSatelliteSOs, item as SatelliteSO))
                        trackName = "PRIMARY";
                    else if (ListContains(_controller.SecondaryTrack.EquippedSatelliteSOs, item as SatelliteSO))
                        trackName = "SECONDARY";
                    break;
            }

            if (string.IsNullOrEmpty(trackName)) return string.Empty;
            return $"{trackName} · {typeName}";
        }

        // Currently selected item in the detail panel
        private StarChartItemSO _selectedItem;

        /// <summary> Fired when panel opens. </summary>
        public event Action OnOpened;

        /// <summary> Fired when panel closes. </summary>
        public event Action OnClosed;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            // Ensure panel starts fully hidden and non-interactive via CanvasGroup.
            // Do NOT call SetActive(false) here — that would prevent Awake from running
            // on first Play Mode entry and break the C-key toggle.
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.blocksRaycasts = false;
            }
            _isOpen = false;
        }

        // =====================================================================
        // Initialization
        // =====================================================================

        /// <summary> Bind to game systems. Call once during initialization. </summary>
        public void Bind(StarChartController controller, StarChartInventorySO inventory)
        {
            _controller = controller;
            _inventory = inventory;

            // Bind track views
            if (_primaryTrackView != null)
            {
                _primaryTrackView.Bind(controller.PrimaryTrack, controller);
                _primaryTrackView.OnTrackSelected += HandleTrackSelected;
                _primaryTrackView.OnCellClicked += HandleCellClicked;
                _primaryTrackView.OnCellPointerEntered += HandleCellPointerEntered;
                _primaryTrackView.OnCellPointerExited += HandleCellPointerExited;
            }

            if (_secondaryTrackView != null)
            {
                _secondaryTrackView.Bind(controller.SecondaryTrack, controller);
                _secondaryTrackView.OnTrackSelected += HandleTrackSelected;
                _secondaryTrackView.OnCellClicked += HandleCellClicked;
                _secondaryTrackView.OnCellPointerEntered += HandleCellPointerEntered;
                _secondaryTrackView.OnCellPointerExited += HandleCellPointerExited;
            }

            // Bind Loadout switcher
            if (_loadoutSwitcher != null)
            {
                _loadoutSwitcher.SetLoadoutCount(3); // 3 independent loadout slots
                _loadoutSwitcher.OnLoadoutChanged += HandleLoadoutChanged;
            }

            // Bind inventory
            if (_inventoryView != null)
            {
                _inventoryView.Bind(inventory, IsItemEquipped);
                _inventoryView.OnItemSelected += HandleInventoryItemSelected;
            }

            // Bind detail panel
            if (_itemDetailView != null)
                _itemDetailView.OnActionClicked += HandleActionClicked;

            // Default: select primary track
            _selectedTrack = controller.PrimaryTrack;
            UpdateTrackSelection();

            // Bind drag-and-drop manager
            if (_dragDropManager != null)
                _dragDropManager.Bind(this, controller);

            // Initialize the shared SAIL column so each cell gets SlotType.LightSail and OwnerTrack=null.
            // This is required for IsTypeMatch and DropTargetIsSailColumn to work correctly.
            if (_sharedSailColumn != null)
            {
                _sharedSailColumn.Initialize(SlotType.LightSail, StarChartTheme.SailColor, null);

                // Inject HasSpaceForItem delegate for shared SAIL column cells
                // (cells have no OwnerTrack, so they need a direct delegate for drop validity)
                foreach (var cell in _sharedSailColumn.Cells)
                {
                    if (cell != null)
                    {
                        cell.HasSpaceForItem = (item) =>
                        {
                            var sailLayer = _controller?.SailLayer;
                            if (sailLayer == null) return false;
                            // If the dragged SAIL is already in the layer, exclude it from the
                            // free-space check so it can be moved to another slot within the column.
                            var mgr = DragDropManager.Instance;
                            if (mgr != null && mgr.CurrentPayload?.Source == DragSource.Slot
                                && item is LightSailSO draggedSail
                                && sailLayer.Items.Contains(draggedSail))
                                return sailLayer.FreeSpace + draggedSail.SlotSize > 0;
                            return sailLayer.FreeSpace > 0;
                        };
                    }
                }

                // Initialize the SAIL highlight layer — must be done AFTER Initialize() so
                // GridContainer and Cells are wired up. Without this, _container is null and
                // ShowHighlightAtCellIndex() returns immediately (no green/red highlight).
                if (_sailHighlightLayer != null)
                    _sailHighlightLayer.Initialize(
                        _sharedSailColumn.GridContainer,
                        _sharedSailColumn.Cells,
                        2);

                // Subscribe to Sail cell hover events for tooltip.
                // Sail cells have OwnerTrack=null so they bypass TrackView.InitColumn();
                // we must wire them up here directly.
                foreach (var cell in _sharedSailColumn.Cells)
                {
                    if (cell == null) continue;
                    cell.OnPointerEntered += HandleSailCellPointerEntered;
                    cell.OnPointerExited  += HandleCellPointerExited;
                }
            }

            // Apply debug SAIL column count override (only when > 0)
            if (_debugSailCols > 0 && _controller != null)
                _controller.SetSailLayerCols(_debugSailCols);

            // Bind inventory item hover events to tooltip
            if (_inventoryView != null)
            {
                _inventoryView.OnItemPointerEntered += HandleInventoryItemHover;
                _inventoryView.OnItemPointerExited += HandleInventoryItemHoverExit;
            }
        }

        // =====================================================================
        // Open / Close
        // =====================================================================

        /// <summary> Open the panel and refresh all views. </summary>
        public void Open()
        {
            _isOpen = true;
            _selectedItem = null;
            RefreshAll();
            OnOpened?.Invoke();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.blocksRaycasts = false;
                _panelCanvasGroup.alpha = 0f;
                var rt = GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one * 0.95f;

                var seq = Sequence.Create(useUnscaledTime: true)
                    .Group(Tween.Alpha(_panelCanvasGroup, endValue: 1f,
                        duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true));

                if (rt != null)
                    seq.Group(Tween.Scale(rt, endValue: Vector3.one,
                        duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true));

                // Loadout card slide-in from below
                if (_loadoutCard != null)
                {
                    _loadoutCard.anchoredPosition = new Vector2(
                        _loadoutCard.anchoredPosition.x, -40f);
                    seq.Group(Tween.UIAnchoredPositionY(_loadoutCard, endValue: 0f,
                        duration: 0.25f, ease: Ease.OutCubic, useUnscaledTime: true));
                }

                seq.ChainCallback(() =>
                {
                    if (_panelCanvasGroup != null)
                    {
                        _panelCanvasGroup.interactable = true;
                        _panelCanvasGroup.blocksRaycasts = true;
                    }
                });
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
                // Immediately block raycasts so invisible panel can't intercept input
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.blocksRaycasts = false;
                var rt = GetComponent<RectTransform>();

                var seq = Sequence.Create(useUnscaledTime: true)
                    .Group(Tween.Alpha(_panelCanvasGroup, endValue: 0f,
                        duration: 0.15f, ease: Ease.InQuad, useUnscaledTime: true));

                if (rt != null)
                    seq.Group(Tween.Scale(rt, endValue: Vector3.one * 0.95f,
                        duration: 0.15f, ease: Ease.InQuad, useUnscaledTime: true));

                // Loadout card slide-out downward
                if (_loadoutCard != null)
                    seq.Group(Tween.UIAnchoredPositionY(_loadoutCard, endValue: -30f,
                        duration: 0.15f, ease: Ease.InQuad, useUnscaledTime: true));

                seq.ChainCallback(() =>
                {
                    _isOpen = false;
                    OnClosed?.Invoke();
                });
            }
            else
            {
                _isOpen = false;
                OnClosed?.Invoke();
            }
        }

        /// <summary> Is the panel currently visible? </summary>
        public bool IsOpen => _isOpen;

        // =====================================================================
        // Refresh
        // =====================================================================

        private void RefreshAll()
        {
            // Refresh both tracks via the current Loadout
            _primaryTrackView?.Refresh();
            _secondaryTrackView?.Refresh();
            RefreshSharedSailColumn();
            _inventoryView?.Refresh();

            // Update status bar equipped count
            UpdateStatusBar();

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
        /// Refresh the shared SAIL column from the current loadout.
        /// Mirrors RefreshColumn() in TrackView: reads SailLayer.Rows * SailLayer.Cols
        /// to determine unlocked cell count; hides the rest via CanvasGroup.
        /// </summary>
        public void RefreshSharedSailColumn()
        {
            if (_sharedSailColumn == null) return;
            var cells = _sharedSailColumn.Cells;
            if (cells == null || cells.Length == 0) return;

            // Read unlocked count from SailLayer (consistent with TrackView.RefreshColumn)
            var sailLayer = _controller?.SailLayer;
            int unlockedCount = sailLayer != null ? sailLayer.Rows * sailLayer.Cols : 0;

            // Show unlocked cells, hide locked cells via CanvasGroup
            // (CLAUDE.md: 禁止 SetActive, 用 CanvasGroup)
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;
                bool unlocked = (i < unlockedCount);
                var cg = cells[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = cells[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha          = unlocked ? 1f : 0f;
                cg.interactable   = unlocked;
                cg.blocksRaycasts = unlocked;
            }

            if (sailLayer == null) return;

            // Reset all unlocked cells to empty first
            for (int i = 0; i < unlockedCount && i < cells.Length; i++)
                cells[i]?.SetEmpty();

            // Fill cells with equipped sails from SailLayer
            for (int r = 0; r < sailLayer.Rows; r++)
            {
                for (int c = 0; c < sailLayer.Cols; c++)
                {
                    var sail = sailLayer.GetAt(c, r);
                    if (sail == null) continue;
                    int cellIndex = r * sailLayer.Cols + c;
                    if (cellIndex < cells.Length && cells[cellIndex] != null)
                        cells[cellIndex].SetItem(sail);
                }
            }
        }

        /// <summary>
        /// Exposes the shared SAIL highlight layer for DragDropManager.
        /// </summary>
        public DragHighlightLayer SailHighlightLayer => _sailHighlightLayer;

        /// <summary>
        /// Exposes the shared SAIL TypeColumn for DragDropManager drop-target detection.
        /// </summary>
        public TypeColumn SharedSailColumn => _sharedSailColumn;

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

        private void UpdateStatusBar()
        {
            if (_statusBar == null || _controller == null) return;

            int equipped = CountEquipped();
            int inventoryCount = _inventory != null ? _inventory.OwnedItems.Count : 0;
            // Use ShowIdle-style instant update (no animation) to avoid
            // PrimeTween "duration <= 0" warning that was caused by duration: 0f.
            _statusBar.SetText(
                $"EQUIPPED {equipped}/10  ·  INVENTORY {inventoryCount} ITEMS  ·  DRAG TO EQUIP  ·  CLICK TO INSPECT",
                StarChartTheme.StatusNormal);
        }

        private int CountEquipped()
        {
            int count = 0;
            if (_controller == null) return count;

            count += _controller.PrimaryTrack.CoreLayer.Items.Count;
            count += _controller.PrimaryTrack.PrismLayer.Items.Count;
            count += _controller.SecondaryTrack.CoreLayer.Items.Count;
            count += _controller.SecondaryTrack.PrismLayer.Items.Count;
            if (_controller.SailLayer != null) count += _controller.SailLayer.Items.Count;
            count += _controller.PrimaryTrack.EquippedSatelliteSOs.Count;
            count += _controller.SecondaryTrack.EquippedSatelliteSOs.Count;
            return count;
        }

        // =====================================================================
        // Event handlers
        // =====================================================================

        private void HandleCellPointerEntered(StarChartItemSO item, string location)
        {
            if (item == null) return;
            ShowTooltip(item, true, location);
        }

        private void HandleSailCellPointerEntered(StarChartItemSO item)
        {
            if (item == null) return;
            bool equipped = IsItemEquipped(item);
            string location = equipped ? GetEquippedLocation(item) : string.Empty;
            ShowTooltip(item, equipped, location);
        }

        private void HandleCellPointerExited()
        {
            HideTooltip();
        }

        private void HandleInventoryItemHover(StarChartItemSO item)
        {
            if (item == null) return;
            bool equipped = IsItemEquipped(item);
            string location = equipped ? GetEquippedLocation(item) : string.Empty;
            ShowTooltip(item, equipped, location);
        }

        private void HandleInventoryItemHoverExit()
        {
            HideTooltip();
        }

        private void HandleLoadoutChanged(int index)
        {
            if (_controller == null) return;

            // Switch the active loadout slot in the controller
            _controller.SwitchLoadout(index);

            // Re-bind track views to the new slot's tracks
            if (_primaryTrackView != null)
                _primaryTrackView.Bind(_controller.PrimaryTrack, _controller);

            if (_secondaryTrackView != null)
                _secondaryTrackView.Bind(_controller.SecondaryTrack, _controller);

            // Update selected track reference to the new primary track
            _selectedTrack = _controller.PrimaryTrack;
            UpdateTrackSelection();

            // Refresh inventory equipped marks and status bar
            _inventoryView?.Refresh();
            UpdateStatusBar();
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

        // =====================================================================
        // Equip / Unequip
        // =====================================================================

        private void EquipItem(StarChartItemSO item)
        {
            if (_selectedTrack == null) return;

            switch (item)
            {
                case StarCoreSO core:
                    if (!_selectedTrack.EquipCore(core))
                    {
                        Debug.LogWarning($"[StarChartPanel] Cannot equip core '{core.DisplayName}': no space");
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
                        Debug.LogWarning($"[StarChartPanel] Cannot equip prism '{prism.DisplayName}': no space");
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
                    // Equip to the currently selected track
                    _controller.EquipSatellite(sat, _selectedTrack?.Id ?? WeaponTrack.TrackId.Primary);
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

                case LightSailSO sail:
                    _controller.UnequipLightSail(sail);
                    _statusBar?.ShowMessage($"UNEQUIPPED: {item.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;

                case SatelliteSO sat:
                    // Unequip from whichever track owns this satellite
                    if (ListContains(_controller.PrimaryTrack.EquippedSatelliteSOs, sat))
                        _controller.UnequipSatellite(sat, WeaponTrack.TrackId.Primary);
                    else
                        _controller.UnequipSatellite(sat, WeaponTrack.TrackId.Secondary);
                    _statusBar?.ShowMessage($"UNEQUIPPED: {sat.DisplayName}", StarChartTheme.StatusNormal, 3f);
                    break;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary> Check if an item is currently equipped in the active loadout slot. </summary>
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
                    return _controller.SailLayer != null && ListContains(_controller.SailLayer.Items, sail);

                case SatelliteSO sat:
                    return ListContains(_controller.PrimaryTrack.EquippedSatelliteSOs, sat)
                        || ListContains(_controller.SecondaryTrack.EquippedSatelliteSOs, sat);

                default:
                    return false;
            }
        }

        private static bool ListContains<T>(System.Collections.Generic.IReadOnlyList<T> list, T target)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], target)) return true;
            return false;
        }

        private void OnDestroy()
        {
            if (_primaryTrackView != null)
            {
                _primaryTrackView.OnTrackSelected -= HandleTrackSelected;
                _primaryTrackView.OnCellClicked -= HandleCellClicked;
                _primaryTrackView.OnCellPointerEntered -= HandleCellPointerEntered;
                _primaryTrackView.OnCellPointerExited -= HandleCellPointerExited;
            }

            if (_secondaryTrackView != null)
            {
                _secondaryTrackView.OnTrackSelected -= HandleTrackSelected;
                _secondaryTrackView.OnCellClicked -= HandleCellClicked;
                _secondaryTrackView.OnCellPointerEntered -= HandleCellPointerEntered;
                _secondaryTrackView.OnCellPointerExited -= HandleCellPointerExited;
            }

            if (_inventoryView != null)
            {
                _inventoryView.OnItemSelected -= HandleInventoryItemSelected;
                _inventoryView.OnItemPointerEntered -= HandleInventoryItemHover;
                _inventoryView.OnItemPointerExited -= HandleInventoryItemHoverExit;
            }

            if (_itemDetailView != null)
                _itemDetailView.OnActionClicked -= HandleActionClicked;

            if (_loadoutSwitcher != null)
                _loadoutSwitcher.OnLoadoutChanged -= HandleLoadoutChanged;

            // Unsubscribe Sail cell hover events
            if (_sharedSailColumn != null)
            {
                foreach (var cell in _sharedSailColumn.Cells)
                {
                    if (cell == null) continue;
                    cell.OnPointerEntered -= HandleSailCellPointerEntered;
                    cell.OnPointerExited  -= HandleCellPointerExited;
                }
            }
        }
    }
}
