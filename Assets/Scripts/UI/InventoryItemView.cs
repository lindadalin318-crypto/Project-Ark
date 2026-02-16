using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
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

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new(0.25f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color _selectedColor = new(0.3f, 0.6f, 0.8f, 1f);

        // Placeholder colors when item has no icon sprite
        private static readonly Color CORE_COLOR = new(0.9f, 0.55f, 0.1f, 1f);     // orange
        private static readonly Color PRISM_COLOR = new(0.4f, 0.6f, 0.9f, 1f);      // blue
        private static readonly Color SAIL_COLOR = new(0.3f, 0.8f, 0.5f, 1f);       // green
        private static readonly Color SATELLITE_COLOR = new(0.7f, 0.4f, 0.8f, 1f);  // purple
        private static readonly Color DEFAULT_COLOR = new(0.5f, 0.5f, 0.5f, 1f);    // grey

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
                    _iconImage.color = GetPlaceholderColor(item);
                }
                Debug.Log($"[InventoryItemView] Setup '{item.DisplayName}': iconSprite={_iconImage.sprite?.name ?? "NULL"}, iconColor={_iconImage.color}, iconEnabled={_iconImage.enabled}, iconGO.active={_iconImage.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning($"[InventoryItemView] Setup '{item.DisplayName}': _iconImage is NULL!");
            }

            if (_nameLabel != null)
                _nameLabel.text = item.DisplayName;

            if (_slotSizeLabel != null)
                _slotSizeLabel.text = $"[{item.SlotSize}]";

            if (_equippedBadge != null)
                _equippedBadge.enabled = isEquipped;

            // Force background to be fully opaque so cards are visible
            var bgImage = GetComponent<Image>();
            if (bgImage != null)
            {
                var c = bgImage.color;
                c.a = 1f;
                bgImage.color = c;
                Debug.Log($"[InventoryItemView] '{item.DisplayName}' bg color={bgImage.color}, rectSize={((RectTransform)transform).sizeDelta}");
            }
        }

        /// <summary> Visual selection state (highlighted border). </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
                _selectionBorder.color = selected ? _selectedColor : _normalColor;
        }

        /// <summary> Returns a type-based placeholder color when an item has no icon. </summary>
        public static Color GetPlaceholderColor(StarChartItemSO item)
        {
            return item.ItemType switch
            {
                StarChartItemType.Core      => CORE_COLOR,
                StarChartItemType.Prism     => PRISM_COLOR,
                StarChartItemType.LightSail => SAIL_COLOR,
                StarChartItemType.Satellite => SATELLITE_COLOR,
                _                           => DEFAULT_COLOR,
            };
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
            // Only Core and Prism support drag-to-equip
            if (Item == null) return;
            if (Item.ItemType != StarChartItemType.Core && Item.ItemType != StarChartItemType.Prism)
            {
                _isDragging = false;
                return;
            }

            if (DragDropManager.Instance == null || DragDropManager.Instance.IsDragging)
            {
                _isDragging = false;
                return;
            }

            _isDragging = true;
            var payload = new DragPayload(Item, DragSource.Inventory);
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

            // If the drop wasn't consumed by a valid target, cancel
            if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
                DragDropManager.Instance.CancelDrag();
        }

        // ========== Pointer Hover Implementation ==========

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;
            OnPointerEntered?.Invoke(Item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;
            OnPointerExited?.Invoke();
        }
    }
}
