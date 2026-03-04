using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Slot type enum — distinguishes the four column types in a TrackView.
    /// </summary>
    public enum SlotType
    {
        Core,
        Prism,
        LightSail,
        Satellite
    }

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

        // Current type color set by the owning TrackView
        private Color _typeColor = Color.white;

        /// <summary> Fired when this cell is clicked. </summary>
        public event Action OnClicked;

        /// <summary> Fired when the pointer enters this cell and an item is displayed. </summary>
        public event Action<StarChartItemSO> OnPointerEntered;

        /// <summary> Fired when the pointer exits this cell. </summary>
        public event Action OnPointerExited;

        /// <summary> The item currently displayed in this cell (null if empty/spanned). </summary>
        public StarChartItemSO DisplayedItem { get; private set; }

        // ========== Slot Type & Identity ==========

        /// <summary> The type of this slot (Core / Prism / LightSail / Satellite). </summary>
        public SlotType SlotType { get; set; }

        /// <summary> Index of this cell within its layer (0, 1, or 2). </summary>
        public int CellIndex { get; set; }

        /// <summary> The TrackView that owns this cell. </summary>
        public TrackView OwnerTrack { get; set; }

        /// <summary> The TypeColumn that owns this cell. Used for column-level drop preview. </summary>
        public TypeColumn OwnerColumn { get; set; }

        /// <summary> True when this cell is displaying an item (not empty/spanned). </summary>
        public bool IsOccupied => DisplayedItem != null;

        /// <summary>
        /// Injected by TrackView on init. Returns true if there is space for the given item
        /// in this cell's layer. Used for SAIL/SAT types that don't use SlotLayer.
        /// </summary>
        public Func<StarChartItemSO, bool> HasSpaceForItem { get; set; }

        // Tracks whether this cell is currently showing a valid drag highlight
        private bool _isHighlightedValid;
        private bool _isDragSource;

        // PrimeTween handle for flash animation
        private Sequence _flashSequence;

        // PrimeTween handle for hover pulse animation
        private Tween _pulseTween;

        // Whether this cell is currently showing an overlay (multi-cell item)
        private bool _isOverlayCell;

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

        /// <summary>
        /// Set this cell as part of an Item Overlay (multi-cell item).
        /// The primary cell (index 0) shows the icon; secondary cells show a tinted background only.
        /// </summary>
        /// <param name="item">The item occupying this cell.</param>
        /// <param name="isPrimary">True if this is the first (icon-bearing) cell of the overlay.</param>
        public void SetOverlay(StarChartItemSO item, bool isPrimary)
        {
            _isOverlayCell = true;
            DisplayedItem = item;

            Color targetBg = new Color(_typeColor.r, _typeColor.g, _typeColor.b, isPrimary ? 0.35f : 0.22f);

            _flashSequence.Stop();
            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.white;
                _flashSequence = Sequence.Create(useUnscaledTime: true)
                    .Chain(Tween.Color(_backgroundImage, endValue: targetBg, duration: 0.15f,
                        ease: Ease.OutQuad, useUnscaledTime: true));
            }

            if (_placeholderLabel != null)
                _placeholderLabel.enabled = false;

            if (_iconImage != null)
            {
                if (isPrimary)
                {
                    _iconImage.enabled = true;
                    if (item != null && item.Icon != null)
                    {
                        _iconImage.sprite = item.Icon;
                        _iconImage.color = Color.white;
                    }
                    else if (item != null)
                    {
                        _iconImage.sprite = null;
                        _iconImage.color = StarChartTheme.GetTypeColor(item.ItemType);
                    }
                }
                else
                {
                    // Secondary overlay cell: no icon, just tinted background
                    _iconImage.enabled = false;
                }
            }
        }

        /// <summary> Show an item's icon in this cell (primary cell for multi-size items). </summary>
        public void SetItem(StarChartItemSO item)
        {
            DisplayedItem = item;

            // Background: dim type color
            Color targetBg = new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.22f);

            // Phase C: flash animation (white → type color)
            _flashSequence.Stop();
            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.white;
                _flashSequence = Sequence.Create(useUnscaledTime: true)
                    .Chain(Tween.Color(_backgroundImage, endValue: targetBg, duration: 0.15f,
                        ease: Ease.OutQuad, useUnscaledTime: true));
            }

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
            _isOverlayCell = false;
            DisplayedItem = null;

            // Phase C: fade to empty color
            _flashSequence.Stop();
            if (_backgroundImage != null)
            {
                Tween.Color(_backgroundImage, endValue: StarChartTheme.SlotEmpty,
                    duration: 0.1f, ease: Ease.OutQuad, useUnscaledTime: true);
            }

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
            _isOverlayCell = false;
            DisplayedItem = null;

            if (_backgroundImage != null)
                _backgroundImage.color = StarChartTheme.SlotSpanned;

            if (_iconImage != null)
                _iconImage.enabled = false;

            if (_placeholderLabel != null)
                _placeholderLabel.enabled = false;
        }

        /// <summary> Highlight the cell (green for valid target, red for invalid). </summary>
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
                // Drag hover: play scale pulse animation
                _pulseTween.Stop();
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                    _pulseTween = Tween.Scale(rt, startValue: Vector3.one,
                        endValue: Vector3.one * 1.05f, duration: 0.1f,
                        ease: Ease.OutQuad, useUnscaledTime: true);
                var payload = mgr.CurrentPayload;
                if (payload == null) return;

                // Type matching: check if dragged item type matches this slot type
                bool typeMatch = IsTypeMatch(payload.Item);

                // Space check
                bool hasSpace = false;
                if (typeMatch)
                {
                    if (HasSpaceForItem != null)
                    {
                        // SAIL/SAT: use injected delegate
                        hasSpace = HasSpaceForItem(payload.Item);
                    }
                    else if (OwnerTrack != null)
                    {
                        // Core/Prism: use TrackView.HasSpaceForItem
                        hasSpace = OwnerTrack.HasSpaceForItem(payload.Item, SlotType == SlotType.Core);
                    }
                }

                // Determine highlight state
                bool occupied = IsOccupied;
                bool valid = typeMatch && hasSpace;
                bool isReplace = typeMatch && !hasSpace && occupied;

                _isHighlightedValid = valid || isReplace;

                // Store drop target info in the manager
                mgr.DropTargetTrack = OwnerTrack?.Track;
                mgr.DropTargetIsCoreLayer = SlotType == SlotType.Core;
                mgr.DropTargetSlotType = SlotType;
                mgr.DropTargetValid = valid || isReplace; // both valid and replace are "accept"
                mgr.DropTargetIsReplace = isReplace;

                // Determine preview state
                DropPreviewState previewState;
                if (!typeMatch)
                    previewState = DropPreviewState.Invalid;
                else if (isReplace)
                    previewState = DropPreviewState.Replace;
                else if (valid)
                    previewState = DropPreviewState.Valid;
                else
                    previewState = DropPreviewState.Invalid;

                // Update ghost drop state
                mgr.UpdateGhostDropState(previewState, isReplace ? 1 : 0);

                // Update column-level drop preview highlight
                OwnerColumn?.SetDropPreview(previewState);

                // Highlight cells matching the item's 2D shape
                if (OwnerTrack != null)
                {
                    if (SlotType == SlotType.Core || SlotType == SlotType.Prism)
                    {
                        // Convert linear CellIndex to (col, row) in the 2×2 grid
                        // Layout: [0]=top-left, [1]=top-right, [2]=bottom-left, [3]=bottom-right
                        // i.e. col = CellIndex % GRID_COLS, row = CellIndex / GRID_COLS
                        const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS;
                        int anchorCol = CellIndex % gridCols;
                        int anchorRow = CellIndex / gridCols;

                        DropPreviewState previewStateForShape = isReplace
                            ? DropPreviewState.Replace
                            : (valid ? DropPreviewState.Valid : DropPreviewState.Invalid);

                        OwnerTrack.SetShapeHighlight(anchorCol, anchorRow, payload.Item.Shape,
                            previewStateForShape, SlotType == SlotType.Core);
                    }
                    else
                        ApplySingleHighlight(valid, isReplace);
                }
                else
                {
                    ApplySingleHighlight(valid, isReplace);
                }
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
                // Restore scale
                _pulseTween.Stop();
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                    _pulseTween = Tween.Scale(rt, endValue: Vector3.one,
                        duration: 0.1f, ease: Ease.OutQuad, useUnscaledTime: true);

                // Clear highlights immediately
                if (OwnerTrack != null)
                    OwnerTrack.ClearAllHighlights();
                else
                    ClearHighlight();

                _isHighlightedValid = false;

                // Delay one frame before clearing DropTargetTrack:
                // if the pointer moved to another cell in the same TrackView,
                // OnPointerEnter on the new cell will re-set DropTargetTrack before
                // the coroutine runs, so we skip the clear and avoid Ghost border flicker.
                StartCoroutine(ClearDropTargetNextFrame(mgr, OwnerTrack?.Track));
            }
            else
            {
                OnPointerExited?.Invoke();
            }
        }

        private IEnumerator ClearDropTargetNextFrame(DragDropManager mgr, WeaponTrack trackAtExit)
        {
            yield return null; // wait one frame

            // Only clear if DropTargetTrack hasn't been re-set by a new OnPointerEnter
            if (mgr != null && mgr.IsDragging && mgr.DropTargetTrack == trackAtExit)
            {
                mgr.DropTargetTrack = null;
                mgr.DropTargetValid = false;
                mgr.DropTargetIsReplace = false;
                mgr.UpdateGhostDropState(DropPreviewState.None);

                // Restore column preview to None (candidate pulse will resume)
                OwnerColumn?.SetDropPreview(DropPreviewState.None);
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
        }

        // ========== Drag Source Implementation (equipped item drag-out) ==========

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (DisplayedItem == null)
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

        // ========== Helpers ==========

        private bool IsTypeMatch(StarChartItemSO item)
        {
            return SlotType switch
            {
                SlotType.Core      => item.ItemType == StarChartItemType.Core,
                SlotType.Prism     => item.ItemType == StarChartItemType.Prism,
                SlotType.LightSail => item.ItemType == StarChartItemType.LightSail,
                SlotType.Satellite => item.ItemType == StarChartItemType.Satellite,
                _                  => false
            };
        }

        private void ApplySingleHighlight(bool valid, bool isReplace)
        {
            if (isReplace)
                SetReplaceHighlight();
            else
                SetHighlight(valid);
        }

        private void OnDestroy()
        {
            _flashSequence.Stop();
            _pulseTween.Stop();
        }
    }
}
