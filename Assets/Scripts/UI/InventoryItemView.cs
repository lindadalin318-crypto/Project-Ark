using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Single item card in the inventory grid.
    /// Shows icon, name, slot size, and an equipped badge.
    /// Supports drag-and-drop to equip items onto weapon track slots.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryItemView : MonoBehaviour, 
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        // _iconImage is intentionally removed: Icon is now built dynamically by ItemIconRenderer
        // (fixed-size, centered on shape centroid — not stretched to bounding box).
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _slotSizeLabel;
        [SerializeField] private Button _button;
        [SerializeField] private Image _equippedBadge;
        [SerializeField] private Image _selectionBorder;

        [Header("Theme Visuals")]
        [SerializeField] private Image _typeDot;          // top-left type color dot
        [SerializeField] private Image _equippedBorder;   // green border when equipped

        // Dynamically created shape preview cells (managed by ItemIconRenderer)
        private Image[] _shapePreviewCells;
        // Dynamically created icon image (managed by ItemIconRenderer)
        private Image _iconImageDynamic;
        private bool _isEquipped;

        /// <summary> Fired when this item card is clicked. </summary>
        public event Action<StarChartItemSO> OnClicked;

        /// <summary> Fired when the pointer enters this item. </summary>
        public event Action<StarChartItemSO> OnPointerEntered;

        /// <summary> Fired when the pointer exits this item. </summary>
        public event Action OnPointerExited;

        /// <summary> The item this view represents. </summary>
        public StarChartItemSO Item { get; private set; }

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke(Item));
        }

        /// <summary> Configure this view with an item and its equipped status. </summary>
        public void Setup(StarChartItemSO item, bool isEquipped)
        {
            Item = item;
            _isEquipped = isEquipped;

            if (_nameLabel != null)
                _nameLabel.text = item.DisplayName;

            if (_slotSizeLabel != null)
                _slotSizeLabel.text = $"[{ItemShapeHelper.GetCells(item.Shape).Count}]";

            // Type dot: left-top corner colored circle
            if (_typeDot != null)
                _typeDot.color = StarChartTheme.GetTypeColor(item.ItemType);

            // C2/C3: Shape preview renders only active cells — equipped state baked in
            BuildShapePreview(item.Shape);

            // Equipped badge (checkmark icon)
            if (_equippedBadge != null)
                _equippedBadge.enabled = isEquipped;

            // C3: _equippedBorder is a stretch Image — keep it clear to avoid leaking into
            // empty cells of non-rectangular shapes.
            if (_equippedBorder != null)
                _equippedBorder.color = Color.clear;

            // C3: Card root Image must stay transparent — all visual color is per shape-cell.
            var bgImage = GetComponent<Image>();
            if (bgImage != null)
                bgImage.color = Color.clear;
        }

        /// <summary>
        /// C3: Visual selection state — affects only the border overlay, never fills empty cells.
        /// The selection border must be a child Image sized to the bounding box,
        /// not the root Image component (which stays Color.clear for shape transparency).
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
            {
                Color targetColor = selected ? StarChartTheme.SelectedCyan : Color.clear;
                Tween.Color(_selectionBorder, endValue: targetColor,
                    duration: 0.1f, ease: Ease.OutQuad, useUnscaledTime: true);
            }
        }

        /// <summary>
        /// C3: Refresh the active cell colors — e.g., when equip state changes.
        /// Delegates to ItemIconRenderer for consistent Icon/Shape decoupled rendering.
        /// </summary>
        public void RefreshShapeColors()
        {
            if (Item == null || _shapePreviewCells == null) return;
            ItemIconRenderer.RefreshShapeCellColors(_shapePreviewCells, Item.Shape, ComputeActiveColor());
            // Also refresh icon alpha
            if (_iconImageDynamic != null)
                ItemIconRenderer.SetIconSprite(_iconImageDynamic, Item);
        }

        /// <summary> Returns a type-based placeholder color when an item has no icon. </summary>
        public static Color GetPlaceholderColor(StarChartItemSO item)
        {
            return StarChartTheme.GetTypeColor(item.ItemType);
        }

        // ========== Drag Source Implementation ==========

        private CanvasGroup _canvasGroup;
        private bool _isDragging;

        /// <summary> Lazy-get CanvasGroup for drag alpha control. </summary>
        private CanvasGroup CachedCanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // All item types support drag-to-equip
            if (Item == null) return;

            if (DragDropManager.Instance == null || DragDropManager.Instance.IsDragging)
            {
                _isDragging = false;
                return;
            }

            _isDragging = true;
            var payload = new DragPayload(Item, DragSource.Inventory);
            // Dim source card with animation
            Tween.Alpha(CachedCanvasGroup, endValue: 0.4f, duration: 0.08f,
                ease: Ease.OutQuad, useUnscaledTime: true);
            DragDropManager.Instance.BeginDrag(payload, eventData, CachedCanvasGroup, GetComponent<RectTransform>());
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            DragDropManager.Instance?.UpdateGhostPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Restore alpha with animation
            Tween.Alpha(CachedCanvasGroup, endValue: 1f, duration: 0.08f,
                ease: Ease.OutQuad, useUnscaledTime: true);

            // If the drop wasn't consumed by a valid target, cancel
            if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
                DragDropManager.Instance.CancelDrag();
        }

        // ========== Pointer Hover Implementation ==========

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;
            // Scale up on hover
            Tween.Scale(transform, endValue: Vector3.one * 1.06f, duration: 0.12f, ease: Ease.OutQuad, useUnscaledTime: true);
            OnPointerEntered?.Invoke(Item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;
            // Restore scale
            Tween.Scale(transform, endValue: Vector3.one, duration: 0.12f, ease: Ease.OutQuad, useUnscaledTime: true);
            OnPointerExited?.Invoke();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_isDragging) return;
            DragDropManager.Instance?.TooltipView?.UpdatePosition();
        }

        // ========== Shape Preview (Icon/Shape decoupled via ItemIconRenderer) ==========

        /// <summary>
        /// C2/C3: Builds the shape background layer + centered icon for the inventory card.
        /// Delegates to ItemIconRenderer — Shape cells and Icon are two independent layers.
        /// Shape cells: only active cells colored, empty cells Color.clear (true holes).
        /// Icon: fixed-size image centered on shape centroid, NOT stretched to bounding box.
        /// </summary>
        private void BuildShapePreview(ItemShape shape)
        {
            // Destroy old shape cells
            if (_shapePreviewCells != null)
            {
                foreach (var img in _shapePreviewCells)
                    if (img != null) Destroy(img.gameObject);
                _shapePreviewCells = null;
            }

            // Destroy old dynamic icon
            if (_iconImageDynamic != null)
            {
                Destroy(_iconImageDynamic.gameObject);
                _iconImageDynamic = null;
            }

            // Build Shape Layer via ItemIconRenderer
            _shapePreviewCells = ItemIconRenderer.BuildShapeCells(
                transform, shape, ComputeActiveColor(), cellPaddingPx: 1f);

            // Build Icon Layer via ItemIconRenderer — fixed size, centered on shape centroid
            if (Item != null)
                _iconImageDynamic = ItemIconRenderer.BuildIconCentered(
                    transform, Item, iconSizePx: 32f);
        }

        /// <summary> Compute the active-cell color based on item type and equip state. </summary>
        private Color ComputeActiveColor()
        {
            Color typeColor = Item != null ? StarChartTheme.GetTypeColor(Item.ItemType) : Color.white;
            if (_isEquipped)
            {
                Color blended = Color.Lerp(typeColor, StarChartTheme.EquippedGreen, 0.4f);
                return new Color(blended.r, blended.g, blended.b, 0.45f);
            }
            return new Color(typeColor.r, typeColor.g, typeColor.b, 0.35f);
        }

        private void OnDestroy()
        {
            if (_shapePreviewCells != null)
            {
                foreach (var img in _shapePreviewCells)
                    if (img != null && img.gameObject != null)
                        Destroy(img.gameObject);
            }
            if (_iconImageDynamic != null && _iconImageDynamic.gameObject != null)
                Destroy(_iconImageDynamic.gameObject);
        }
    }
}
