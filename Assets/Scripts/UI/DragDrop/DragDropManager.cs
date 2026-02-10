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

        /// <summary> True while a drag operation is in progress. </summary>
        public bool IsDragging { get; private set; }

        /// <summary> The current drag context, null when not dragging. </summary>
        public DragPayload CurrentPayload { get; private set; }

        // --- Drop target info set by SlotCellView on hover ---
        /// <summary> The track that owns the cell currently hovered. </summary>
        public WeaponTrack DropTargetTrack { get; set; }

        /// <summary> True if the hovered cell belongs to the core layer. </summary>
        public bool DropTargetIsCoreLayer { get; set; }

        /// <summary> Whether the current hover target is a valid drop. </summary>
        public bool DropTargetValid { get; set; }

        private StarChartPanel _panel;
        private StarChartController _controller;
        private DragGhostView _ghost;

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

            // Directly reference scene instance — no Instantiate needed
            if (_ghostView != null)
            {
                _ghost = _ghostView;
                _ghost.Hide();
            }
        }

        /// <summary>
        /// Start a new drag operation.
        /// </summary>
        /// <param name="payload">Data describing the dragged item and source.</param>
        /// <param name="sourceCanvasGroup">Optional: the source view's CanvasGroup to dim.</param>
        public void BeginDrag(DragPayload payload, CanvasGroup sourceCanvasGroup = null)
        {
            if (IsDragging) return; // only one drag at a time

            IsDragging = true;
            CurrentPayload = payload;
            _sourceCanvasGroup = sourceCanvasGroup;
            DropTargetTrack = null;
            DropTargetValid = false;

            // Dim source card
            if (_sourceCanvasGroup != null)
                _sourceCanvasGroup.alpha = 0.4f;

            // Show ghost
            if (_ghost != null)
                _ghost.Show(payload.Item);
        }

        /// <summary>
        /// Move the ghost to follow the pointer. Call from IDragHandler.OnDrag.
        /// </summary>
        public void UpdateGhostPosition(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_ghost != null && IsDragging)
                _ghost.FollowPointer(eventData);
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

            CleanUp();
        }

        /// <summary>
        /// Cancel the current drag without performing any action.
        /// Safe to call when panel closes mid-drag.
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
                // Inventory → Slot: equip
                EquipToTrack(item, DropTargetTrack);
            }
            else if (source == DragSource.Slot)
            {
                var sourceTrack = CurrentPayload.SourceTrack;

                if (DropTargetTrack == null)
                {
                    // Slot → Inventory area: unequip (handled by InventoryView.OnDrop)
                    UnequipFromTrack(item, sourceTrack);
                }
                else if (sourceTrack != null && sourceTrack != DropTargetTrack)
                {
                    // Cross-track move: unequip from source, equip to target
                    UnequipFromTrack(item, sourceTrack);
                    EquipToTrack(item, DropTargetTrack);
                }
                else if (sourceTrack != null && sourceTrack == DropTargetTrack)
                {
                    // Same track — no-op (item already there)
                }
            }

            // Refresh all views and select the item in detail panel
            _panel.RefreshAllViews();
            _panel.SelectAndShowItem(item);
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
                    if (track.EquipCore(core))
                        track.InitializePools();
                    else
                        Debug.LogWarning($"[DragDropManager] Failed to equip core '{core.DisplayName}': no space");
                    break;

                case PrismSO prism:
                    if (!track.EquipPrism(prism))
                        Debug.LogWarning($"[DragDropManager] Failed to equip prism '{prism.DisplayName}': no space");
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
            }
        }

        private void CleanUp()
        {
            // Restore source alpha
            if (_sourceCanvasGroup != null)
            {
                _sourceCanvasGroup.alpha = 1f;
                _sourceCanvasGroup = null;
            }

            // Hide ghost
            if (_ghost != null)
                _ghost.Hide();

            IsDragging = false;
            CurrentPayload = null;
            DropTargetTrack = null;
            DropTargetValid = false;
            DropTargetIsCoreLayer = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
