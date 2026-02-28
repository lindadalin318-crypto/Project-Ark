using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Single grid cell in a weapon track's slot layer.
    /// Displays item icon, empty state, or spanned-by indicator.
    /// Acts as a drop target for drag-and-drop equip, and a drag source for unequip.
    /// </summary>
    public class SlotCellView : MonoBehaviour,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;

        [Header("Placeholder Label")]
        [SerializeField] private TMP_Text _placeholderLabel;

        // Current type color set by the owning TrackView (used for occupied background & hover border)
        private Color _typeColor = Color.white;

        /// <summary> Fired when this cell is clicked. </summary>
        public event Action OnClicked;

        /// <summary> Fired when the pointer enters this cell and an item is displayed. </summary>
        public event Action<StarChartItemSO> OnPointerEntered;

        /// <summary> Fired when the pointer exits this cell. </summary>
        public event Action OnPointerExited;

        /// <summary> The item currently displayed in this cell (null if empty/spanned). </summary>
        public StarChartItemSO DisplayedItem { get; private set; }

        // ========== Drag-and-Drop Properties (set by TrackView on init) ==========

        /// <summary> True if this cell belongs to the Core layer (false = Prism layer). </summary>
        public bool IsCoreCell { get; set; }

        /// <summary> Index of this cell within its layer (0, 1, or 2). </summary>
        public int CellIndex { get; set; }

        /// <summary> The TrackView that owns this cell. </summary>
        public TrackView OwnerTrack { get; set; }

        // Tracks whether this cell is currently showing a valid drag highlight
        private bool _isHighlightedValid;
        private bool _isDragSource;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        /// <summary>
        /// Set the type theme color for this cell.
        /// Called by TrackView.Refresh() so occupied cells use the layer's type color.
        /// </summary>
        public void SetThemeColor(Color typeColor)
        {
            _typeColor = typeColor;
        }

        /// <summary> Show an item's icon in this cell (primary cell for multi-size items). </summary>
        public void SetItem(StarChartItemSO item)
        {
            DisplayedItem = item;

            // Background: dim type color
            if (_backgroundImage != null)
                _backgroundImage.color = new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.22f);

            // Hide placeholder label
            if (_placeholderLabel != null)
                _placeholderLabel.enabled = false;

            if (_iconImage != null)
            {
                _iconImage.enabled = true;

                if (item != null && item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.color = Color.white;
                }
                else if (item != null)
                {
                    // No icon: show type-colored placeholder square
                    _iconImage.sprite = null;
                    _iconImage.color = StarChartTheme.GetTypeColor(item.ItemType);
                    Debug.Log($"[SlotCellView] Item '{item.DisplayName}' has no icon — showing placeholder.");
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = Color.clear;
                }
            }
        }

        /// <summary> Show empty state (no item, available for equip). </summary>
        public void SetEmpty()
        {
            DisplayedItem = null;

            if (_backgroundImage != null)
                _backgroundImage.color = StarChartTheme.SlotEmpty;

            if (_iconImage != null)
                _iconImage.enabled = false;

            // Show '+' placeholder
            if (_placeholderLabel != null)
            {
                _placeholderLabel.text = "+";
                _placeholderLabel.color = new Color(1f, 1f, 1f, 0.25f);
                _placeholderLabel.enabled = true;
            }
        }

        /// <summary> Show as occupied by a multi-size item (no icon, tinted background). </summary>
        public void SetSpannedBy(StarChartItemSO item)
        {
            DisplayedItem = null;

            if (_backgroundImage != null)
                _backgroundImage.color = StarChartTheme.SlotSpanned;

            if (_iconImage != null)
                _iconImage.enabled = false;

            if (_placeholderLabel != null)
                _placeholderLabel.enabled = false;
        }

        /// <summary> Highlight the cell (green for valid target, orange for replace, red for invalid). </summary>
        public void SetHighlight(bool valid)
        {
            if (_backgroundImage != null)
                _backgroundImage.color = valid ? StarChartTheme.HighlightValid : StarChartTheme.HighlightInvalid;
        }

        /// <summary> Highlight the cell as a replace target (orange). </summary>
        public void SetReplaceHighlight()
        {
            if (_backgroundImage != null)
                _backgroundImage.color = StarChartTheme.HighlightReplace;
        }

        /// <summary> Remove any highlight, restore to current state. </summary>
        public void ClearHighlight()
        {
            _isHighlightedValid = false;

            if (DisplayedItem != null)
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.22f);
            }
            else
            {
                if (_backgroundImage != null)
                    _backgroundImage.color = StarChartTheme.SlotEmpty;
            }
        }

        // ========== Drop Target Implementation ==========

        public void OnPointerEnter(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging)
            {
                var payload = mgr.CurrentPayload;
                if (payload == null) return;

                // Type matching: Core items → core cells, Prism items → prism cells
                bool typeMatch = (payload.Item.ItemType == StarChartItemType.Core && IsCoreCell)
                              || (payload.Item.ItemType == StarChartItemType.Prism && !IsCoreCell);

                // Space check via the owning track
                bool hasSpace = false;
                if (typeMatch && OwnerTrack != null)
                {
                    hasSpace = OwnerTrack.HasSpaceForItem(payload.Item, IsCoreCell);
                }

                bool valid = typeMatch && hasSpace;
                _isHighlightedValid = valid;

                // Store drop target info in the manager
                mgr.DropTargetTrack = OwnerTrack?.Track;
                mgr.DropTargetIsCoreLayer = IsCoreCell;
                mgr.DropTargetValid = valid;

                // Highlight this cell and adjacent cells for SlotSize > 1
                if (OwnerTrack != null)
                    OwnerTrack.SetMultiCellHighlight(CellIndex, payload.Item.SlotSize, IsCoreCell, valid);
                else
                    SetHighlight(valid);
            }
            else
            {
                // Not dragging - show tooltip if there's an item
                if (DisplayedItem != null)
                    OnPointerEntered?.Invoke(DisplayedItem);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr != null && mgr.IsDragging)
            {
                // Clear highlights
                if (OwnerTrack != null)
                    OwnerTrack.ClearAllHighlights();
                else
                    ClearHighlight();

                _isHighlightedValid = false;

                // Clear drop target in manager
                if (mgr.DropTargetTrack == OwnerTrack?.Track)
                {
                    mgr.DropTargetTrack = null;
                    mgr.DropTargetValid = false;
                }
            }
            else
            {
                // Not dragging - hide tooltip
                OnPointerExited?.Invoke();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var mgr = DragDropManager.Instance;
            if (mgr == null || !mgr.IsDragging) return;

            if (_isHighlightedValid)
            {
                mgr.EndDrag(true);
            }
            // If invalid, do nothing — OnEndDrag on the source will cancel
        }

        // ========== Drag Source Implementation (equipped item drag-out) ==========

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only start drag if this cell has an equipped item
            if (DisplayedItem == null)
            {
                _isDragSource = false;
                return;
            }

            // Only Core and Prism can be dragged
            if (DisplayedItem.ItemType != StarChartItemType.Core
                && DisplayedItem.ItemType != StarChartItemType.Prism)
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
            var payload = new DragPayload(DisplayedItem, DragSource.Slot, OwnerTrack?.Track);
            mgr.BeginDrag(payload);
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
    }
}
