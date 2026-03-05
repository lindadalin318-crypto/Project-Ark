using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Renders a complete shaped-item overlay on a Track TypeColumn grid.
    /// Positioned absolutely over the bounding box of the item's shape,
    /// with per-cell fill matching the actual occupied cells (non-rectangular shapes
    /// show transparent cells for unoccupied positions in the bounding box).
    ///
    /// Lifecycle: created by TrackView.RefreshColumn, destroyed on next Refresh.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ItemOverlayView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler
    {
        // =====================================================================
        // Serialized fields (set programmatically — no Inspector wiring needed)
        // =====================================================================

        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _iconImage;

        // =====================================================================
        // Runtime state
        // =====================================================================

        private StarChartItemSO _item;
        private SlotType _slotType;
        private TrackView _ownerTrack;

        // Dynamically created cell images inside the overlay
        private Image[] _cellImages;

        // PrimeTween handles
        private Tween _hoverScaleTween;
        private Tween _borderGlowTween;

        // Drag source state
        private bool _isDragSource;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Initialize and render the overlay for the given item.
        /// Must be called immediately after instantiation.
        /// </summary>
        /// <param name="item">The equipped item to display.</param>
        /// <param name="slotType">The column type (determines theme color).</param>
        /// <param name="ownerTrack">The TrackView that owns this overlay.</param>
        /// <param name="anchorCol">Anchor column in the grid (0-based).</param>
        /// <param name="anchorRow">Anchor row in the grid (0-based).</param>
        /// <param name="cellSize">Size of a single grid cell in pixels.</param>
        /// <param name="cellGap">Gap between cells in pixels.</param>
        public void Setup(
            StarChartItemSO item,
            SlotType slotType,
            TrackView ownerTrack,
            int anchorCol,
            int anchorRow,
            float cellSize,
            float cellGap)
        {
            _item = item;
            _slotType = slotType;
            _ownerTrack = ownerTrack;

            // Dynamically create border and icon child objects
            // (ItemOverlayView is instantiated via code, not from a prefab)
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(transform, false);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;
            _borderImage = borderGo.GetComponent<Image>();
            _borderImage.raycastTarget = false;

            Color typeColor = StarChartTheme.GetTypeColor(item.ItemType);
            var shape = item.Shape;
            var bounds = ItemShapeHelper.GetBounds(shape);

            // ── Position & size the overlay RectTransform ──────────────────
            var rt = GetComponent<RectTransform>();
            float overlayW = bounds.x * cellSize + (bounds.x - 1) * cellGap;
            float overlayH = bounds.y * cellSize + (bounds.y - 1) * cellGap;

            // Anchor to top-left of the grid container
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f); // top-left pivot

            float xPos =  anchorCol * (cellSize + cellGap);
            float yPos = -anchorRow * (cellSize + cellGap);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(overlayW, overlayH);

            // ── Build per-cell images via ItemIconRenderer (Shape Layer) ──
            Color activeColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.55f);
            _cellImages = ItemIconRenderer.BuildShapeCellsAbsolute(
                transform, shape, activeColor, cellSize, cellGap);

            // ── Border / glow image ────────────────────────────────────────
            if (_borderImage != null)
            {
                // Subtle background tint; the per-cell images provide the main visual
                _borderImage.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.12f);
                _borderImage.raycastTarget = false;
            }

            // ── Icon image via ItemIconRenderer (Icon Layer) ───────────────
            // Fixed-size icon centered on anchor cell — NOT stretched to bounding box.
            // This is the Backpack Monsters approach: Icon = identity, Shape = occupancy.
            _iconImage = ItemIconRenderer.BuildIconOnAnchorCell(
                transform, item, cellSize, iconSizePx: cellSize * 0.65f);

            // ── Snap-in entrance animation ─────────────────────────────────
            rt.localScale = Vector3.one * 0.85f;
            Tween.Scale(rt, endValue: Vector3.one,
                duration: 0.15f, ease: Ease.OutBack, useUnscaledTime: true);
        }

        // =====================================================================
        // Hover / Click events (forwarded to TrackView)
        // =====================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging)
            {
                // During drag: forward to the underlying cell's OnPointerEnter
                // by finding the cell at the anchor position.
                // The overlay's raycastTarget=true means EventSystem hits us first;
                // we need to forward the event to the correct SlotCellView.
                ForwardDragEventToCell(eventData);
                return;
            }

            // Not dragging: show tooltip
            if (_item != null)
                _ownerTrack?.RaiseOverlayCellPointerEntered(_item, _slotType);

            // Hover scale animation
            _hoverScaleTween.Stop();
            var rt = GetComponent<RectTransform>();
            _hoverScaleTween = Tween.Scale(rt, endValue: Vector3.one * 1.04f,
                duration: 0.08f, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging) return;

            _ownerTrack?.RaiseOverlayCellPointerExited();

            // Restore scale
            _hoverScaleTween.Stop();
            var rt = GetComponent<RectTransform>();
            _hoverScaleTween = Tween.Scale(rt, endValue: Vector3.one,
                duration: 0.08f, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging) return;

            if (_item != null)
                _ownerTrack?.RaiseOverlayCellClicked(_item);
        }

        // =====================================================================
        // Drag source (drag equipped item out of track)
        // =====================================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_item == null)
            {
                _isDragSource = false;
                return;
            }

            var mgr = DragDropManager.Instance;
            if (mgr == null || mgr.IsDragging)
            {
                _isDragSource = false;
                return;
            }

            _isDragSource = true;
            var payload = new DragPayload(_item, DragSource.Slot, _ownerTrack?.Track);
            mgr.BeginDrag(payload, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragSource) return;
            DragDropManager.Instance?.UpdateGhostPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragSource) return;
            _isDragSource = false;

            if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
                DragDropManager.Instance.CancelDrag();
        }

        // =====================================================================
        // Drop target (when dragging over an overlay, accept the drop)
        // =====================================================================

        public void OnDrop(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr == null || !mgr.IsDragging) return;

            // Forward to the underlying cell's OnDrop
            if (_ownerTrack == null) return;
            foreach (var cell in _ownerTrack.GetAllCells())
            {
                if (cell == null) continue;
                var cellRt = cell.GetComponent<RectTransform>();
                if (cellRt == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    cellRt, eventData.position, eventData.enterEventCamera))
                {
                    cell.OnDrop(eventData);
                    return;
                }
            }

            // Fallback: if DropTargetValid, end drag directly
            if (mgr.DropTargetValid)
                mgr.EndDrag(true);
        }

        // =====================================================================
        // Drop target forwarding (when dragging over an overlay)
        // =====================================================================

        /// <summary>
        /// Forward drag hover to the SlotCellView underneath this overlay.
        /// Since the overlay sits on top of the cells, we need to manually
        /// find the cell at the pointer position and invoke its OnPointerEnter.
        /// </summary>
        private void ForwardDragEventToCell(PointerEventData eventData)
        {
            if (_ownerTrack == null) return;

            // Use RectTransformUtility to find which cell the pointer is over
            foreach (var cell in _ownerTrack.GetAllCells())
            {
                if (cell == null) continue;
                var cellRt = cell.GetComponent<RectTransform>();
                if (cellRt == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(
                    cellRt, eventData.position, eventData.enterEventCamera))
                {
                    // Found the cell — invoke its pointer enter
                    cell.OnPointerEnter(eventData);
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            _hoverScaleTween.Stop();
            _borderGlowTween.Stop();
        }
    }
}
