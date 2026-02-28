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
        IPointerEnterHandler, IPointerExitHandler
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

            // Equipped badge (checkmark)
            if (_equippedBadge != null)
                _equippedBadge.enabled = isEquipped;

            // Equipped border: green tint when equipped
            if (_equippedBorder != null)
                _equippedBorder.color = isEquipped ? StarChartTheme.EquippedGreen : Color.clear;

            // Force background to be fully opaque so cards are visible
            var bgImage = GetComponent<Image>();
            if (bgImage != null)
            {
                var c = bgImage.color;
                c.a = 1f;
                bgImage.color = c;
            }
        }

        /// <summary> Visual selection state (highlighted border). </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
            {
                Color targetColor = selected ? StarChartTheme.SelectedCyan : Color.clear;
                Tween.Color(_selectionBorder, endValue: targetColor,
                    duration: 0.1f, ease: Ease.OutQuad, useUnscaledTime: true);
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
    }
}
