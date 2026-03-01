using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Singleton manager that orchestrates all drag-and-drop operations
    /// within the Star Chart panel. Attach to the StarChart Canvas.
    /// All logic is timeScale-independent (compatible with paused state).
    /// </summary>
    public class DragDropManager : MonoBehaviour
    {
        public static DragDropManager Instance { get; private set; }

        [SerializeField] private DragGhostView _ghostView;
        [SerializeField] private Canvas _rootCanvas;

        /// <summary> True while a drag operation is in progress. </summary>
        public bool IsDragging { get; private set; }

        /// <summary> The current drag context, null when not dragging. </summary>
        public DragPayload CurrentPayload { get; private set; }

        // --- Drop target info set by SlotCellView on hover ---
        /// <summary> The track that owns the cell currently hovered. </summary>
        public WeaponTrack DropTargetTrack { get; set; }

        /// <summary> True if the hovered cell belongs to the core layer. </summary>
        public bool DropTargetIsCoreLayer { get; set; }

        /// <summary> The slot type of the current drop target. </summary>
        public SlotType DropTargetSlotType { get; set; }

        /// <summary> Whether the current hover target is a valid drop (including replace). </summary>
        public bool DropTargetValid { get; set; }

        /// <summary> Whether the current drop would trigger a forced replace. </summary>
        public bool DropTargetIsReplace { get; set; }

        /// <summary>
        /// Items evicted during the last forced replace operation.
        /// Populated by EvictBlockingItems, consumed by FlyBackAnimator.
        /// </summary>
        public List<(StarChartItemSO item, WeaponTrack track)> EvictedItems { get; } = new();

        private StarChartPanel _panel;
        private StarChartController _controller;
        private RectTransform _inventoryRect; // target for fly-back animations

        // Source InventoryItemView — used to restore alpha on cancel/end
        private CanvasGroup _sourceCanvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Bind to the StarChartPanel and controller. Call once during panel initialization.
        /// </summary>
        public void Bind(StarChartPanel panel, StarChartController controller)
        {
            _panel = panel;
            _controller = controller;

            if (_ghostView != null)
            {
                _ghostView.Hide();
            }

            // Cache inventory rect for fly-back target
            if (_panel != null)
            {
                var invView = _panel.GetComponentInChildren<InventoryView>(true);
                if (invView != null)
                    _inventoryRect = invView.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Start a new drag operation.
        /// </summary>
        public void BeginDrag(DragPayload payload, CanvasGroup sourceCanvasGroup = null)
        {
            if (IsDragging) return;

            // Skip all in-flight fly-back animations
            FlyBackAnimator.SkipAll();

            IsDragging = true;
            CurrentPayload = payload;
            _sourceCanvasGroup = sourceCanvasGroup;
            DropTargetTrack = null;
            DropTargetValid = false;
            DropTargetIsReplace = false;
            EvictedItems.Clear();

            // Dim source card
            if (_sourceCanvasGroup != null)
                _sourceCanvasGroup.alpha = 0.4f;

            // Show ghost
            if (_ghostView != null)
                _ghostView.Show(payload.Item);

            // Highlight all TypeColumns that match the dragged item's type
            HighlightMatchingColumns(payload.Item, true);
        }

        /// <summary>
        /// Move the ghost to follow the pointer. Call from IDragHandler.OnDrag.
        /// </summary>
        public void UpdateGhostPosition(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_ghostView != null && IsDragging)
                _ghostView.FollowPointer(eventData);
        }

        /// <summary>
        /// Update the ghost's drop preview state (border color + replace hint).
        /// Called by SlotCellView.OnPointerEnter / OnPointerExit.
        /// </summary>
        public void UpdateGhostDropState(DropPreviewState state)
        {
            _ghostView?.SetDropState(state);
        }

        /// <summary>
        /// Complete the drag as a successful drop. Called by the drop target.
        /// </summary>
        public void EndDrag(bool success)
        {
            if (!IsDragging) return;

            if (success && DropTargetValid)
            {
                ExecuteDrop();
            }
            else if (success && !DropTargetValid && CurrentPayload?.Source == DragSource.Slot)
            {
                // Slot dragged to empty area — give user feedback
                _panel?.StatusBar?.ShowMessage(
                    "CANNOT PLACE HERE",
                    StarChartTheme.HighlightInvalid,
                    2f);
            }

            CleanUp();
        }

        /// <summary>
        /// Cancel the current drag without performing any action.
        /// </summary>
        public void CancelDrag()
        {
            if (!IsDragging) return;
            CleanUp();
        }

        /// <summary>
        /// Execute the actual equip/unequip/move operation based on payload and drop target.
        /// </summary>
        private void ExecuteDrop()
        {
            if (CurrentPayload == null || DropTargetTrack == null || _panel == null) return;

            var item = CurrentPayload.Item;
            var source = CurrentPayload.Source;

            if (source == DragSource.Inventory)
            {
                EquipToTrack(item, DropTargetTrack);
            }
            else if (source == DragSource.Slot)
            {
                var sourceTrack = CurrentPayload.SourceTrack;

                if (DropTargetTrack == null)
                {
                    UnequipFromTrack(item, sourceTrack);
                }
                else if (sourceTrack != null && sourceTrack != DropTargetTrack)
                {
                    UnequipFromTrack(item, sourceTrack);
                    EquipToTrack(item, DropTargetTrack);
                }
                else if (sourceTrack != null && sourceTrack == DropTargetTrack)
                {
                    // Same track — no-op
                }
            }

            _panel.RefreshAllViews();
            _panel.SelectAndShowItem(item);

            // Trigger fly-back animations for evicted items
            TriggerFlyBackAnimations();
        }

        /// <summary>
        /// Execute an unequip-to-inventory drop. Called when item is dropped on InventoryView.
        /// </summary>
        public void ExecuteUnequipDrop()
        {
            if (CurrentPayload == null || CurrentPayload.Source != DragSource.Slot) return;

            var item = CurrentPayload.Item;
            var sourceTrack = CurrentPayload.SourceTrack;

            if (sourceTrack != null)
            {
                UnequipFromTrack(item, sourceTrack);
                _panel?.RefreshAllViews();
                _panel?.SelectAndShowItem(item);
            }
        }

        private void EquipToTrack(StarChartItemSO item, WeaponTrack track)
        {
            switch (item)
            {
                case StarCoreSO core:
                    if (!track.EquipCore(core))
                    {
                        // Force replace: evict blocking items, then retry
                        EvictBlockingItems(item, track, isCoreLayer: true);
                        if (track.EquipCore(core))
                        {
                            track.InitializePools();
                            ShowReplaceMessage(item);
                        }
                        else
                        {
                            Debug.LogWarning($"[DragDropManager] Still failed to equip core '{core.DisplayName}' after eviction");
                        }
                    }
                    else
                    {
                        track.InitializePools();
                    }
                    break;

                case PrismSO prism:
                    if (!track.EquipPrism(prism))
                    {
                        EvictBlockingItems(item, track, isCoreLayer: false);
                        if (!track.EquipPrism(prism))
                        {
                            Debug.LogWarning($"[DragDropManager] Still failed to equip prism '{prism.DisplayName}' after eviction");
                        }
                        else
                        {
                            ShowReplaceMessage(item);
                        }
                    }
                    break;

                case LightSailSO sail:
                    // Evict existing sail if present
                    var existingSail = _controller?.GetEquippedLightSail();
                    if (existingSail != null)
                    {
                        EvictedItems.Add((existingSail, track));
                        _controller.UnequipLightSail();
                        ShowReplaceMessage(item);
                    }
                    _controller?.EquipLightSail(sail);
                    break;

                case SatelliteSO sat:
                    var sats = _controller?.GetEquippedSatellites();
                    int maxSats = 2;
                    if (sats != null && sats.Count >= maxSats)
                    {
                        // Evict the oldest satellite
                        var evicted = sats[0];
                        EvictedItems.Add((evicted, track));
                        _controller.UnequipSatellite(evicted);
                        ShowReplaceMessage(item);
                    }
                    _controller?.EquipSatellite(sat);
                    break;
            }
        }

        private void UnequipFromTrack(StarChartItemSO item, WeaponTrack track)
        {
            switch (item)
            {
                case StarCoreSO core:
                    track.UnequipCore(core);
                    break;

                case PrismSO prism:
                    track.UnequipPrism(prism);
                    break;

                case LightSailSO:
                    _controller?.UnequipLightSail();
                    break;

                case SatelliteSO sat:
                    _controller?.UnequipSatellite(sat);
                    break;
            }
        }

        /// <summary>
        /// Evict all items blocking placement of <paramref name="newItem"/> in the given layer.
        /// Evicted items are added to <see cref="EvictedItems"/> for fly-back animation.
        /// </summary>
        private void EvictBlockingItems(StarChartItemSO newItem, WeaponTrack track, bool isCoreLayer)
        {
            if (track == null) return;

            if (isCoreLayer)
            {
                var layer = track.CoreLayer;
                // Evict items until there's enough free space
                while (layer.FreeSpace < newItem.SlotSize && layer.Items.Count > 0)
                {
                    var victim = layer.Items[layer.Items.Count - 1];
                    EvictedItems.Add((victim, track));
                    track.UnequipCore(victim as StarCoreSO);
                }
            }
            else
            {
                var layer = track.PrismLayer;
                while (layer.FreeSpace < newItem.SlotSize && layer.Items.Count > 0)
                {
                    var victim = layer.Items[layer.Items.Count - 1];
                    EvictedItems.Add((victim, track));
                    track.UnequipPrism(victim as PrismSO);
                }
            }
        }

        /// <summary>
        /// Trigger fly-back animations for all items evicted during the last forced replace.
        /// </summary>
        private void TriggerFlyBackAnimations()
        {
            if (EvictedItems.Count == 0) return;
            if (_inventoryRect == null || _rootCanvas == null) return;

            // Find the source slot RectTransform from the current payload
            // Use the ghost's last position as the "from" point
            var ghostRect = _ghostView?.GetComponent<RectTransform>();

            foreach (var (evictedItem, _) in EvictedItems)
            {
                // Use ghost position as source (it's at the drop point)
                var fromRect = ghostRect ?? _inventoryRect;
                FlyBackAnimator.FlyTo(fromRect, _inventoryRect, evictedItem, _rootCanvas);
            }
        }

        private void ShowReplaceMessage(StarChartItemSO newItem)
        {
            if (EvictedItems.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < EvictedItems.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(EvictedItems[i].item.DisplayName);
            }
            _panel?.StatusBar?.ShowMessage(
                $"REPLACED: {sb} → {newItem.DisplayName}",
                StarChartTheme.HighlightReplace,
                3f);
        }

        private void CleanUp()
        {
            if (_sourceCanvasGroup != null)
            {
                // Only directly restore alpha if the source is NOT an InventoryItemView.
                // InventoryItemView.OnEndDrag handles its own alpha restore via PrimeTween.
                if (CurrentPayload?.Source != DragSource.Inventory)
                    _sourceCanvasGroup.alpha = 1f;
                _sourceCanvasGroup = null;
            }

            if (_ghostView != null)
                _ghostView.Hide();

            // Clear all TypeColumn highlights
            if (CurrentPayload != null)
                HighlightMatchingColumns(CurrentPayload.Item, false);

            IsDragging = false;
            CurrentPayload = null;
            DropTargetTrack = null;
            DropTargetValid = false;
            DropTargetIsReplace = false;
            DropTargetIsCoreLayer = false;
        }

        /// <summary>
        /// Highlight (or clear) all TypeColumns in both TrackViews that match the given item's type.
        /// Called on drag begin (highlight=true) and drag end/cancel (highlight=false).
        /// </summary>
        private void HighlightMatchingColumns(StarChartItemSO item, bool highlight)
        {
            if (_panel == null || item == null) return;

            // Determine the matching SlotType
            SlotType matchType = item.ItemType switch
            {
                StarChartItemType.Core      => SlotType.Core,
                StarChartItemType.Prism     => SlotType.Prism,
                StarChartItemType.LightSail => SlotType.LightSail,
                StarChartItemType.Satellite => SlotType.Satellite,
                _                           => SlotType.Core
            };

            // Find all TrackViews in the panel and highlight/clear the matching column
            var trackViews = _panel.GetComponentsInChildren<TrackView>(true);
            foreach (var tv in trackViews)
            {
                var col = tv.GetColumn(matchType);
                if (col == null) continue;

                if (highlight)
                    col.SetDropHighlight(true);
                else
                    col.ClearDropHighlight();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
