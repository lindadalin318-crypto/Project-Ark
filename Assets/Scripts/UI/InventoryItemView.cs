using System;
using System.Collections.Generic;
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
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _slotSizeLabel;
        [SerializeField] private Button _button;
        [SerializeField] private Image _equippedBadge;
        [SerializeField] private Image _selectionBorder;

        [Header("Theme Visuals")]
        [SerializeField] private Image _typeDot;          // top-left type color dot
        [SerializeField] private Image _equippedBorder;   // green border when equipped

        // Dynamically created shape preview cells
        private Image[] _shapePreviewCells;
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

        // Cached 1x1 white sprite used as placeholder when item has no icon.
        private static Sprite _whitePlaceholder;
        private static Sprite WhitePlaceholder
        {
            get
            {
                if (_whitePlaceholder == null)
                {
                    var tex = new Texture2D(4, 4);
                    var pixels = new Color[16];
                    for (int i = 0; i < 16; i++) pixels[i] = Color.white;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    _whitePlaceholder = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                }
                return _whitePlaceholder;
            }
        }

        /// <summary> Configure this view with an item and its equipped status. </summary>
        public void Setup(StarChartItemSO item, bool isEquipped)
        {
            Item = item;
            _isEquipped = isEquipped;

            if (_iconImage != null)
            {
                if (item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.color = Color.white;
                }
                else
                {
                    // Image component won't render color when sprite is null.
                    // Use a white placeholder sprite and tint it with the type color.
                    _iconImage.sprite = WhitePlaceholder;
                    _iconImage.color = StarChartTheme.GetTypeColor(item.ItemType);
                }
            }

            if (_nameLabel != null)
                _nameLabel.text = item.DisplayName;

            if (_slotSizeLabel != null)
                _slotSizeLabel.text = $"[{item.SlotSize}]";

            // Type dot: left-top corner colored circle
            if (_typeDot != null)
                _typeDot.color = StarChartTheme.GetTypeColor(item.ItemType);

            // C2/C3: Shape preview renders only active cells — equipped state baked in
            BuildShapePreview(item.Shape);

            // Equipped badge (checkmark icon)
            if (_equippedBadge != null)
                _equippedBadge.enabled = isEquipped;

            // C3: _equippedBorder is a stretch Image — keep it clear to avoid leaking into
            // empty cells of non-rectangular shapes. The equipped tint is handled per-cell
            // inside BuildShapePreview via the _isEquipped flag.
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
        /// Only active cells receive color; empty cells stay transparent.
        /// </summary>
        public void RefreshShapeColors()
        {
            if (Item == null || _shapePreviewCells == null) return;

            var cells = ItemShapeHelper.GetCells(Item.Shape);
            var bounds = ItemShapeHelper.GetBounds(Item.Shape);

            var activeCellSet = new HashSet<Vector2Int>();
            foreach (var c in cells) activeCellSet.Add(c);

            Color typeColor = StarChartTheme.GetTypeColor(Item.ItemType);
            Color activeColor;
            if (_isEquipped)
            {
                Color equippedGreen = StarChartTheme.EquippedGreen;
                Color blended = Color.Lerp(typeColor, equippedGreen, 0.4f);
                activeColor = new Color(blended.r, blended.g, blended.b, 0.45f);
            }
            else
            {
                activeColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.35f);
            }

            int idx = 0;
            for (int row = 0; row < bounds.y; row++)
            {
                for (int col = 0; col < bounds.x; col++)
                {
                    if (idx >= _shapePreviewCells.Length) break;
                    bool isActive = activeCellSet.Contains(new Vector2Int(col, row));
                    if (_shapePreviewCells[idx] != null)
                        _shapePreviewCells[idx].color = isActive ? activeColor : Color.clear;
                    idx++;
                }
            }
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
            DragDropManager.Instance.BeginDrag(payload, CachedCanvasGroup);
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

        // ========== Shape Preview ==========

        /// <summary>
        /// C2/C3: Builds the shape background layer for the inventory card.
        /// Renders ONLY the active cells (from ItemShapeHelper.GetCells) with type color.
        /// Empty cells in the bounding box remain Color.clear (true holes, not filled squares).
        /// This matches the HTML prototype's .inv-shape-grid: active cells colored, empty transparent.
        /// Applies to ALL shapes including 1x1 (since the card background Image is now Color.clear).
        /// </summary>
        private void BuildShapePreview(ItemShape shape)
        {
            // Destroy old preview cells
            if (_shapePreviewCells != null)
            {
                foreach (var img in _shapePreviewCells)
                    if (img != null) Destroy(img.gameObject);
                _shapePreviewCells = null;
            }

            var cells = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);

            // Build a lookup set for active cells (C1: single source of truth)
            var activeCellSet = new HashSet<Vector2Int>();
            foreach (var c in cells) activeCellSet.Add(c);

            // C3: active-cell color — type color at 0.35 opacity (matching HTML prototype)
            // Equipped items get a green-tinted mix to indicate equip status,
            // applied only to active cells (never to empty holes).
            Color typeColor = Item != null ? StarChartTheme.GetTypeColor(Item.ItemType) : Color.white;
            Color activeColor;
            if (_isEquipped)
            {
                // Blend type color toward equipped green for a subtle equipped indicator
                Color equippedGreen = StarChartTheme.EquippedGreen;
                Color blended = Color.Lerp(typeColor, equippedGreen, 0.4f);
                activeColor = new Color(blended.r, blended.g, blended.b, 0.45f);
            }
            else
            {
                activeColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.35f);
            }
            // C3: empty cells stay fully transparent — they are genuine holes
            Color emptyColor = Color.clear;

            // Create one cell image per bounding-box position.
            // Active cells get colored, empty cells stay transparent.
            int totalCells = bounds.x * bounds.y;
            _shapePreviewCells = new Image[totalCells];

            float cellW = 1f / bounds.x;
            float cellH = 1f / bounds.y;
            int idx = 0;

            for (int row = 0; row < bounds.y; row++)
            {
                for (int col = 0; col < bounds.x; col++)
                {
                    bool isActive = activeCellSet.Contains(new Vector2Int(col, row));

                    var cellGo = new GameObject($"ShapeCell_{col}_{row}", typeof(RectTransform), typeof(Image));
                    cellGo.transform.SetParent(transform, false);
                    cellGo.transform.SetAsFirstSibling(); // behind icon and labels

                    var cellRect = cellGo.GetComponent<RectTransform>();
                    float xMin = col * cellW;
                    float yMax = 1f - row * cellH;
                    cellRect.anchorMin = new Vector2(xMin, yMax - cellH);
                    cellRect.anchorMax = new Vector2(xMin + cellW, yMax);
                    // 1px inset padding for cell-gap visual separation
                    cellRect.offsetMin = new Vector2(1f, 1f);
                    cellRect.offsetMax = new Vector2(-1f, -1f);

                    var cellImg = cellGo.GetComponent<Image>();
                    // C3: only active cells are colored — empty cells are transparent holes
                    cellImg.color = isActive ? activeColor : emptyColor;
                    cellImg.raycastTarget = false;
                    _shapePreviewCells[idx++] = cellImg;
                }
            }
        }

        private void OnDestroy()
        {
            if (_shapePreviewCells != null)
            {
                foreach (var img in _shapePreviewCells)
                    if (img != null && img.gameObject != null)
                        Destroy(img.gameObject);
            }
        }
    }
}
