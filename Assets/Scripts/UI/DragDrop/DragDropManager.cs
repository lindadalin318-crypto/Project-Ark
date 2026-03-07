using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
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
        /// True when the current drop target is the shared SAIL column (not a per-track cell).
        /// In this case DropTargetTrack may be null; SAIL equip goes directly through the controller.
        /// </summary>
        public bool DropTargetIsSailColumn { get; set; }

        /// <summary>
        /// The resolved anchor column for the current drop target (computed by FindBestAnchor).
        /// Valid only when DropTargetValid is true and DropTargetSlotType is Core or Prism.
        /// </summary>
        public int DropTargetAnchorCol { get; set; }

        /// <summary>
        /// The resolved anchor row for the current drop target (computed by FindBestAnchor).
        /// Valid only when DropTargetValid is true and DropTargetSlotType is Core or Prism.
        /// </summary>
        public int DropTargetAnchorRow { get; set; }

        /// <summary>
        /// Items evicted during the last forced replace operation.
        /// Populated by EvictBlockingItems, consumed by FlyBackAnimator.
        /// </summary>
        public List<(StarChartItemSO item, WeaponTrack track)> EvictedItems { get; } = new();
        public ItemTooltipView TooltipView { get; private set; }

        /// <summary> Exposes the shared SAIL highlight layer for SlotCellView hover feedback. </summary>
        public DragHighlightLayer SailHighlightLayer => _panel?.SailHighlightLayer;

        /// <summary> Exposes the SAIL slot layer for SlotCellView anchor computation. </summary>
        public SlotLayer<LightSailSO> SailLayer => _panel?.Controller?.SailLayer;

        private StarChartPanel _panel;
        private StarChartController _controller;
        private RectTransform _inventoryRect; // target for fly-back animations
        private readonly List<TypeColumn> _registeredColumns = new();

        // Source InventoryItemView — used to restore alpha on cancel/end
        private CanvasGroup _sourceCanvasGroup;

        // Drag anchor offset: mouse position relative to Ghost center in canvas local space.
        // Computed once in BeginDrag so the ghost stays "under" the exact click point.
        private Vector2 _dragOffset;

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
                _ghostView.Hide();

            // Cache inventory rect for fly-back target
            if (_panel != null)
            {
                var invView = _panel.GetComponentInChildren<InventoryView>(true);
                if (invView != null)
                    _inventoryRect = invView.GetComponent<RectTransform>();
                TooltipView = _panel.GetComponentInChildren<ItemTooltipView>(true);

                // Fallback registration pass in case some columns were enabled before manager Awake.
                var columns = _panel.GetComponentsInChildren<TypeColumn>(true);
                foreach (var col in columns)
                    RegisterColumn(col);
            }
        }

        /// <summary>
        /// Register a type column to receive drag begin/end broadcasts.
        /// </summary>
        public void RegisterColumn(TypeColumn column)
        {
            if (column == null) return;
            if (_registeredColumns.Contains(column)) return;
            _registeredColumns.Add(column);
        }

        /// <summary>
        /// Unregister a type column from drag broadcasts.
        /// </summary>
        public void UnregisterColumn(TypeColumn column)
        {
            if (column == null) return;
            _registeredColumns.Remove(column);
        }

        /// <summary>
        /// Start a new drag operation.
        /// </summary>
        /// <param name="payload">Drag payload describing the item and source.</param>
        /// <param name="eventData">Pointer event data from OnBeginDrag — used to anchor the ghost under the click point.</param>
        /// <param name="sourceCanvasGroup">Optional source card CanvasGroup to dim during drag.</param>
        /// <param name="sourceRect">Optional source card RectTransform — used to pin the ghost under the exact click point.</param>
        public void BeginDrag(DragPayload payload, PointerEventData eventData = null, CanvasGroup sourceCanvasGroup = null, RectTransform sourceRect = null)
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
            DropTargetIsSailColumn = false;
            EvictedItems.Clear();

            // Dim source card
            if (_sourceCanvasGroup != null)
                _sourceCanvasGroup.alpha = 0.4f;

            // Show ghost
            if (_ghostView != null)
            {
                _ghostView.Show(payload.Item);

                // Compute drag anchor offset so the ghost stays pinned under the click point.
                // After Show(), the ghost has a known size. We convert the pointer screen position
                // to canvas local space, then store the delta from ghost center (which FollowPointer
                // would place at the pointer). This offset is subtracted every frame in FollowPointer.
                if (eventData != null)
                    _dragOffset = _ghostView.ComputeDragOffset(eventData, sourceRect);
                else
                    _dragOffset = Vector2.zero;
            }

            // Suppress tooltip during drag
            _panel?.GetComponentInChildren<ItemTooltipView>(true)?.SetDragSuppressed(true);

            // Show unequip hint when dragging from an equipped slot
            if (payload.Source == DragSource.Slot)
            {
                _panel?.StatusBar?.ShowPersistent("DRAG TO INVENTORY TO UNEQUIP", StarChartTheme.HighlightReplace);
                // Refresh track views to remove the ItemOverlayView of the dragged item,
                // preventing Ghost + overlay double-render (Problem D).
                //
                // IMPORTANT: Do NOT call RefreshAllViews() synchronously here!
                // If the drag was initiated from an ItemOverlayView.OnBeginDrag, calling
                // RefreshAllViews() immediately destroys the overlay's GameObject in the
                // same frame — before OnDrag/OnEndDrag can fire. This breaks drag entirely
                // for PRISM/CORE items (SAIL/SAT use SlotCellView which survives the refresh).
                //
                // Solution: defer one frame via UniTask so the current drag event chain
                // (OnBeginDrag → OnDrag → ...) completes before the overlay is destroyed.
                DeferredRefreshAsync().Forget();
            }

            BroadcastDragBegin(payload.Item);
        }

        /// <summary>
        /// Move the ghost to follow the pointer. Call from IDragHandler.OnDrag.
        /// </summary>
        public void UpdateGhostPosition(PointerEventData eventData)
        {
            if (_ghostView != null && IsDragging)
                _ghostView.FollowPointer(eventData, _dragOffset);
        }

        /// <summary>
        /// Update the ghost's drop preview state (border color + replace hint).
        /// Called by SlotCellView.OnPointerEnter / OnPointerExit.
        /// </summary>
        /// <param name="state">The new drop preview state.</param>
        /// <param name="evictCount">Number of items that would be evicted (shown in replace hint).</param>
        public void UpdateGhostDropState(DropPreviewState state, int evictCount = 0)
        {
            _ghostView?.SetDropState(state, evictCount);
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
            if (CurrentPayload == null || _panel == null) return;

            // Shared SAIL column: DropTargetTrack may be null, but DropTargetIsSailColumn is set.
            // SAIL equip/unequip goes directly through the controller, no track needed.
            if (DropTargetIsSailColumn)
            {
                var sailItem = CurrentPayload.Item;

                // Detect SAIL column-internal move (already equipped → moving to another slot).
                bool isSailInternalMove = CurrentPayload.Source == DragSource.Slot
                    && sailItem is LightSailSO movingSail
                    && _controller?.SailLayer != null
                    && System.Linq.Enumerable.Contains(_controller.SailLayer.Items, movingSail);

                if (isSailInternalMove && sailItem is LightSailSO internalSail)
                {
                    // No-op check: if the target anchor is the same as the current anchor, do nothing.
                    var currentAnchor = _controller.SailLayer.GetAnchor(internalSail);
                    bool isSameSlot = currentAnchor.x == DropTargetAnchorCol
                                   && currentAnchor.y == DropTargetAnchorRow;
                    if (!isSameSlot)
                    {
                        // Move within SAIL column: unequip from old position first, then equip at new position.
                        _controller.UnequipLightSail(internalSail);
                        EquipToTrack(sailItem, null);
                    }
                    // else: dropped on the exact same slot — true no-op, nothing to do.
                }
                else
                {
                    // From Inventory or other source: equip normally.
                    EquipToTrack(sailItem, null);
                }

                _panel.RefreshAllViews();
                _panel.SelectAndShowItem(sailItem);
                PlaySnapInAnimation(sailItem);
                TriggerFlyBackAnimations();
                return;
            }

            if (DropTargetTrack == null) return;

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
                    // Same track — move to a different slot.
                    // Resolve the item's current anchor position to detect a no-op (drop on same slot).
                    Vector2Int currentAnchor = new Vector2Int(-1, -1);
                    if (item is StarCoreSO coreItem && sourceTrack.CoreLayer != null)
                        currentAnchor = sourceTrack.CoreLayer.GetAnchor(coreItem);
                    else if (item is PrismSO prismItem && sourceTrack.PrismLayer != null)
                        currentAnchor = sourceTrack.PrismLayer.GetAnchor(prismItem);
                    else if (item is SatelliteSO satItem && sourceTrack.SatLayer != null)
                        currentAnchor = sourceTrack.SatLayer.GetAnchor(satItem);

                    bool isSameSlot = currentAnchor.x == DropTargetAnchorCol
                                   && currentAnchor.y == DropTargetAnchorRow;
                    if (!isSameSlot)
                    {
                        // Move within the same track: unequip from old position, equip at new position.
                        UnequipFromTrack(item, sourceTrack);
                        EquipToTrack(item, DropTargetTrack);
                    }
                    // else: dropped on the exact same slot — true no-op, nothing to do.
                }
            }

            _panel.RefreshAllViews();
            _panel.SelectAndShowItem(item);

            // Play snap-in animation on the newly placed item's cell
            PlaySnapInAnimation(item);

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
            else if (item is LightSailSO sail)
            {
                // Shared SAIL column: SourceTrack is null (no owning track).
                // Unequip the specific sail directly through the controller.
                _controller?.UnequipLightSail(sail);
                _panel?.RefreshAllViews();
                _panel?.SelectAndShowItem(item);
            }
        }

        private void EquipToTrack(StarChartItemSO item, WeaponTrack track)
        {
            // Use the anchor resolved by FindBestAnchor (set in SlotCellView.OnPointerEnter)
            int anchorCol = DropTargetAnchorCol;
            int anchorRow = DropTargetAnchorRow;

            switch (item)
            {
                case StarCoreSO core:
                    if (!track.EquipCore(core, anchorCol, anchorRow))
                    {
                        // Force replace: evict blocking items, then retry
                        EvictBlockingItems(item, track, isCoreLayer: true);
                        if (track.EquipCore(core, anchorCol, anchorRow))
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
                    if (!track.EquipPrism(prism, anchorCol, anchorRow))
                    {
                        EvictBlockingItems(item, track, isCoreLayer: false);
                        if (!track.EquipPrism(prism, anchorCol, anchorRow))
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
                    // Shared SAIL column: equip at the resolved anchor position.
                    var sailLayer = _controller?.SailLayer;
                    // Defensive guard: if this sail is already in the layer (column-internal move
                    // that wasn't pre-unequipped by ExecuteDrop), unequip it first to prevent duplicates.
                    bool sailAlreadyEquipped = sailLayer != null && System.Linq.Enumerable.Contains(sailLayer.Items, sail);
                    if (sailAlreadyEquipped)
                    {
                        _controller.UnequipLightSail(sail);
                    }
                    else
                    {
                        // Only run eviction logic when equipping from outside the SAIL column.
                        var sailOccupant = sailLayer?.GetAt(anchorCol, anchorRow);
                        if (sailOccupant != null && !ReferenceEquals(sailOccupant, sail))
                        {
                            EvictedItems.Add((sailOccupant, track));
                            _controller.UnequipLightSail(sailOccupant);
                            ShowReplaceMessage(sail);
                        }
                        else if (sailLayer != null && sailLayer.FreeSpace <= 0 && sailOccupant == null)
                        {
                            // No free space and target cell is empty — evict the first sail
                            var firstSail = sailLayer.Items.Count > 0 ? sailLayer.Items[0] : null;
                            if (firstSail != null)
                            {
                                EvictedItems.Add((firstSail, track));
                                _controller.UnequipLightSail(firstSail);
                                ShowReplaceMessage(sail);
                            }
                        }
                    }
                    _controller?.EquipLightSail(sail, anchorCol, anchorRow);
                    break;

                case SatelliteSO sat:
                    // SAT now uses SlotLayer<SatelliteSO> — place at the resolved anchor position.
                    // anchorCol/anchorRow are already declared at the top of EquipToTrack.
                    var satOccupant = track.SatLayer?.GetAt(anchorCol, anchorRow);
                    if (satOccupant != null && !ReferenceEquals(satOccupant, sat))
                    {
                        // Evict the occupant at the target cell first
                        EvictedItems.Add((satOccupant, track));
                        _controller.UnequipSatellite(satOccupant, track.Id);
                        ShowReplaceMessage(sat);
                    }
                    _controller.EquipSatellite(sat, track.Id, anchorCol, anchorRow);
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

                case LightSailSO sail:
                    _controller?.UnequipLightSail(sail);
                    break;

                case SatelliteSO sat:
                    // Unequip from the track that owns this satellite
                    _controller?.UnequipSatellite(sat, track.Id);
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
        /// Play snap-in animation (scale 1.18 → 0.96 → 1.0) on the cell that now holds the dropped item.
        /// Called after RefreshAllViews so the cell already shows the new item.
        /// </summary>
        private void PlaySnapInAnimation(StarChartItemSO item)
        {
            if (_panel == null || item == null) return;

            // Find the SlotCellView that now displays this item
            var trackViews = _panel.GetComponentsInChildren<TrackView>(true);
            foreach (var tv in trackViews)
            {
                foreach (var cell in tv.GetAllCells())
                {
                    if (cell.DisplayedItem == item)
                    {
                        var rt = cell.GetComponent<RectTransform>();
                        if (rt == null) continue;

                        // snap-in: scale 1.18 → 0.96 → 1.0
                        PrimeTween.Tween.Scale(rt, endValue: Vector3.one * 1.18f,
                            duration: 0.06f, ease: PrimeTween.Ease.OutQuad, useUnscaledTime: true)
                            .OnComplete(() =>
                                PrimeTween.Tween.Scale(rt, endValue: Vector3.one * 0.96f,
                                    duration: 0.05f, ease: PrimeTween.Ease.InQuad, useUnscaledTime: true)
                                    .OnComplete(() =>
                                        PrimeTween.Tween.Scale(rt, endValue: Vector3.one,
                                            duration: 0.05f, ease: PrimeTween.Ease.OutBack, useUnscaledTime: true)));
                        return; // Only animate the primary cell
                    }
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

            // Clear all shape highlight tiles across every TrackView
            if (_panel != null)
            {
                var trackViews = _panel.GetComponentsInChildren<TrackView>(true);
                foreach (var tv in trackViews)
                    tv.ClearAllHighlightTiles();

                // Also clear shared SAIL highlight layer
                _panel.SailHighlightLayer?.ClearHighlight();
            }

            BroadcastDragEnd();

            // Restore status bar (clear persistent drag hint)
            _panel?.StatusBar?.RestoreDefault();

            // Restore tooltip suppression
            _panel?.GetComponentInChildren<ItemTooltipView>(true)?.SetDragSuppressed(false);

            IsDragging = false;
            CurrentPayload = null;
            DropTargetTrack = null;
            DropTargetValid = false;
            DropTargetIsReplace = false;
            DropTargetIsCoreLayer = false;
            DropTargetIsSailColumn = false;
            DropTargetAnchorCol = 0;
            DropTargetAnchorRow = 0;
        }

        /// <summary>
        /// Broadcast drag begin to all registered columns so each column can self-evaluate.
        /// </summary>
        /// <summary>
        /// Refresh all track views one frame after BeginDrag, so the current drag event
        /// chain (OnBeginDrag → OnDrag) completes before any overlay GameObjects are destroyed.
        /// </summary>
        private async UniTaskVoid DeferredRefreshAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, destroyCancellationToken);
            _panel?.RefreshAllViews();
        }

        private void BroadcastDragBegin(StarChartItemSO item)
        {
            if (item == null) return;
            foreach (var column in _registeredColumns)
                column?.OnDragBeginBroadcast(item);
        }

        /// <summary>
        /// Broadcast drag end to clear candidate highlights on all registered columns.
        /// </summary>
        private void BroadcastDragEnd()
        {
            foreach (var column in _registeredColumns)
                column?.OnDragEndBroadcast();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
