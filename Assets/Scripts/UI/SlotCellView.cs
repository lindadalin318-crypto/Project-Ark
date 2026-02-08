using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Single grid cell in a weapon track's slot layer.
    /// Displays item icon, empty state, or spanned-by indicator.
    /// </summary>
    public class SlotCellView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new(0.15f, 0.15f, 0.2f, 0.8f);
        [SerializeField] private Color _occupiedColor = new(0.2f, 0.25f, 0.35f, 0.9f);
        [SerializeField] private Color _spannedColor = new(0.18f, 0.22f, 0.3f, 0.7f);
        [SerializeField] private Color _selectedHighlight = new(0.3f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color _invalidHighlight = new(0.8f, 0.2f, 0.2f, 1f);

        /// <summary> Fired when this cell is clicked. </summary>
        public event Action OnClicked;

        /// <summary> The item currently displayed in this cell (null if empty/spanned). </summary>
        public StarChartItemSO DisplayedItem { get; private set; }

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        /// <summary> Show an item's icon in this cell (primary cell for multi-size items). </summary>
        public void SetItem(StarChartItemSO item)
        {
            DisplayedItem = item;

            if (_backgroundImage != null)
                _backgroundImage.color = _occupiedColor;

            if (_iconImage != null)
            {
                _iconImage.enabled = true;
                _iconImage.sprite = item != null ? item.Icon : null;
                _iconImage.color = item != null && item.Icon != null ? Color.white : Color.clear;
            }
        }

        /// <summary> Show empty state (no item, available for equip). </summary>
        public void SetEmpty()
        {
            DisplayedItem = null;

            if (_backgroundImage != null)
                _backgroundImage.color = _emptyColor;

            if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }
        }

        /// <summary> Show as occupied by a multi-size item (no icon, tinted background). </summary>
        public void SetSpannedBy(StarChartItemSO item)
        {
            DisplayedItem = null;

            if (_backgroundImage != null)
                _backgroundImage.color = _spannedColor;

            if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }
        }

        /// <summary> Highlight the cell (green for valid target, red for invalid). </summary>
        public void SetHighlight(bool valid)
        {
            if (_backgroundImage != null)
                _backgroundImage.color = valid ? _selectedHighlight : _invalidHighlight;
        }

        /// <summary> Remove any highlight, restore to current state. </summary>
        public void ClearHighlight()
        {
            if (DisplayedItem != null)
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = _occupiedColor;
            }
            else
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = _emptyColor;
            }
        }
    }
}
