using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Single item card in the inventory grid.
    /// Shows icon, name, slot size, and an equipped badge.
    /// </summary>
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _slotSizeLabel;
        [SerializeField] private Button _button;
        [SerializeField] private Image _equippedBadge;
        [SerializeField] private Image _selectionBorder;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new(0.2f, 0.2f, 0.25f, 0.9f);
        [SerializeField] private Color _selectedColor = new(0.3f, 0.6f, 0.8f, 1f);

        /// <summary> Fired when this item card is clicked. </summary>
        public event Action<StarChartItemSO> OnClicked;

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

            if (_iconImage != null)
            {
                _iconImage.sprite = item.Icon;
                _iconImage.color = item.Icon != null ? Color.white : Color.clear;
            }

            if (_nameLabel != null)
                _nameLabel.text = item.DisplayName;

            if (_slotSizeLabel != null)
                _slotSizeLabel.text = $"[{item.SlotSize}]";

            if (_equippedBadge != null)
                _equippedBadge.enabled = isEquipped;
        }

        /// <summary> Visual selection state (highlighted border). </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionBorder != null)
                _selectionBorder.color = selected ? _selectedColor : _normalColor;
        }
    }
}
