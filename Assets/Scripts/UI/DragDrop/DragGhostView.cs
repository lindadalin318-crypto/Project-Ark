using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Drop preview state for the drag ghost.
    /// </summary>
    public enum DropPreviewState
    {
        None,
        Valid,
        Replace,
        Invalid
    }

    /// <summary>
    /// Semi-transparent ghost that follows the mouse during a drag operation.
    /// Shows drop state via border color and replace hint label.
    /// Supports shape-aware rendering: resizes to the item's 2D ItemShape bounding box.
    /// Must live under the StarChart Canvas with <c>CanvasGroup.blocksRaycasts = false</c>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DragGhostView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private TMP_Text _replaceHintLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private float _ghostAlpha = 0.7f;
        [SerializeField] private Vector2 _ghostSize = new(64f, 64f);

        [Header("Shape Settings")]
        [Tooltip("Height of a single slot cell in pixels")]
        [SerializeField] private float _cellHeight = 56f;
        [Tooltip("Gap between cells in pixels")]
        [SerializeField] private float _cellGap = 4f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;
        private Tween _scaleTween;
        private Tween _borderColorTween;

        // CanvasGroup wrappers for label visibility (CLAUDE.md 第11条：禁止 SetActive)
        private CanvasGroup _replaceHintCg;
        private CanvasGroup _nameLabelCg;

        // Dynamically created cell grid images
        private Image[] _cellImages;
        private int _currentSlotSize = 1;
        private ItemShape _currentShape = ItemShape.Shape1x1;

        // Dynamically created icon image (Icon Layer, managed by ItemIconRenderer)
        private Image _iconImageDynamic;

        // Cell color is determined per-item by StarChartTheme.GetTypeColorDim(item.ItemType)
        // to match the exact color used in SlotCellView.SetItem() — no hardcoded color here.

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();

            if (_canvasGroup != null)
            {
                // CLAUDE.md 第11条：uGUI面板禁止用SetActive控制显隐
                // 始终保持 active，用 alpha=0 + blocksRaycasts=false 隐藏
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = _ghostSize;
                _rectTransform.localScale = Vector3.one;
            }

        // Initialize border and hint
        if (_borderImage != null)
            _borderImage.color = Color.clear;

        // Hide legacy _iconImage permanently — Icon is now built dynamically by ItemIconRenderer.
        // Use CanvasGroup (not color=clear) to guarantee it never renders regardless of sprite.
        if (_iconImage != null)
        {
            var iconCg = _iconImage.GetComponent<CanvasGroup>();
            if (iconCg == null) iconCg = _iconImage.gameObject.AddComponent<CanvasGroup>();
            iconCg.alpha = 0f;
            iconCg.blocksRaycasts = false;
            iconCg.interactable = false;
        }

            // CLAUDE.md 第11条：uGUI面板禁止用SetActive控制显隐，统一用CanvasGroup
            // NOTE: 必须用显式 if 判断，不能用 ?? 运算符。
            // Unity 的 UnityEngine.Object 重写了 == 运算符，但 C# 的 ?? 绕过了该重写，
            // 导致 GetComponent 返回"假null"时 ?? 不执行右侧 AddComponent，引发 LogError。
            if (_replaceHintLabel != null)
            {
                _replaceHintCg = _replaceHintLabel.GetComponent<CanvasGroup>();
                if (_replaceHintCg == null)
                    _replaceHintCg = _replaceHintLabel.gameObject.AddComponent<CanvasGroup>();
                if (_replaceHintCg != null)
                {
                    _replaceHintCg.alpha = 0f;
                    _replaceHintCg.blocksRaycasts = false;
                    _replaceHintCg.interactable = false;
                }
            }

            if (_nameLabel != null)
            {
                _nameLabelCg = _nameLabel.GetComponent<CanvasGroup>();
                if (_nameLabelCg == null)
                    _nameLabelCg = _nameLabel.gameObject.AddComponent<CanvasGroup>();
                if (_nameLabelCg != null)
                {
                    _nameLabelCg.alpha = 0f;
                    _nameLabelCg.blocksRaycasts = false;
                    _nameLabelCg.interactable = false;
                }
            }

            // NOTE: 绝不调用 gameObject.SetActive(false)！
            // Ghost GameObject 始终保持 active，由 CanvasGroup.alpha 控制可见性
        }

        /// <summary>
        /// Adjust the ghost shape to match the item's 2D ItemShape.
        /// Resizes the ghost to the shape's bounding box and rebuilds the cell grid.
        /// </summary>
        public void SetShape(ItemShape shape, StarChartItemType itemType = StarChartItemType.Core)
        {
            _currentShape = shape;
            var bounds = ItemShapeHelper.GetBounds(shape);
            int cols = bounds.x;
            int rows = bounds.y;
            _currentSlotSize = rows; // legacy compat

            float newWidth  = cols * _ghostSize.x + (cols - 1) * _cellGap;
            float newHeight = rows * _cellHeight  + (rows - 1) * _cellGap;

            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

            // Use type-specific dim color (alpha=0.22) to match SlotCellView.SetItem() exactly.
            // Ghost CanvasGroup.alpha handles overall transparency — cell color stays opaque.
            Color cellColor = new Color(
                StarChartTheme.GetTypeColor(itemType).r,
                StarChartTheme.GetTypeColor(itemType).g,
                StarChartTheme.GetTypeColor(itemType).b,
                0.22f);
            RebuildShapeGrid(shape, newWidth, newHeight, cellColor);
        }

        private void RebuildShapeGrid(ItemShape shape, float totalWidth, float totalHeight, Color cellColor)
        {
            // Destroy old cell images
            if (_cellImages != null)
            {
                foreach (var img in _cellImages)
                    if (img != null) Destroy(img.gameObject);
            }

            // Build Shape Layer via ItemIconRenderer using the item's type color
            _cellImages = ItemIconRenderer.BuildShapeCellsAbsolute(
                transform, shape, cellColor, _ghostSize.x, _cellGap);
        }

        /// <summary> Show the ghost with the item's icon or placeholder color. </summary>
        public void Show(StarChartItemSO item)
        {
            if (item == null) return;

            // Stop any in-flight hide tween
            _scaleTween.Stop();

            // Reset to clean state before showing
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one;

            // Apply shape based on ItemShape (2D), passing item type for correct cell color
            SetShape(item.Shape, item.ItemType);

            // Icon Layer via ItemIconRenderer — fixed-size, placed at shape centroid
            // in the same absolute pixel coordinate system as BuildShapeCellsAbsolute.
            // Must use BuildIconAbsolute (not BuildIconCentered) because shape cells
            // use anchorMin=zero + anchoredPosition, not normalized anchor fractions.
            // Using BuildIconCentered here would cause Icon and Shape to appear at
            // completely different positions (the "two objects" bug).
            if (_iconImageDynamic != null)
            {
                Destroy(_iconImageDynamic.gameObject);
                _iconImageDynamic = null;
            }
            _iconImageDynamic = ItemIconRenderer.BuildIconAbsolute(
                transform, item, item.Shape,
                cellSize:   _ghostSize.x,
                cellGap:    _cellGap,
                iconSizePx: _ghostSize.x * 0.55f,
                alpha:      _ghostAlpha);

            // _iconImage is permanently hidden via CanvasGroup in Awake — no action needed here.

            // Show name label via CanvasGroup (CLAUDE.md 第11条)
            if (_nameLabel != null && _nameLabelCg != null)
            {
                _nameLabel.text = item.DisplayName;
                _nameLabelCg.alpha = 1f;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _ghostAlpha;
                _canvasGroup.blocksRaycasts = false; // Ghost 不拦截射线
            }

            // Reset drop state
            SetDropState(DropPreviewState.None);

            // Pop-in animation: scale 0.8 → 1.0
            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one * 0.8f;
                _scaleTween = Tween.Scale(_rectTransform, endValue: Vector3.one,
                    duration: 0.08f, ease: Ease.OutBack, useUnscaledTime: true);
            }
        }

        /// <summary>
        /// Update the ghost's border color and replace hint based on drop preview state.
        /// Uses PrimeTween for smooth color transition (≤80ms).
        /// </summary>
        public void SetDropState(DropPreviewState state, int evictCount = 0)
        {
            // Ghost border color is intentionally kept transparent — drop state feedback
            // is shown exclusively on the Track grid cells (DragHighlightLayer), not on the ghost.
            if (_borderImage != null)
            {
                _borderColorTween.Stop();
                _borderImage.color = Color.clear;
            }

            // 用 CanvasGroup.alpha 控制 replaceHintLabel 显隐（CLAUDE.md 第11条）
            if (_replaceHintLabel != null && _replaceHintCg != null)
            {
                bool showHint = state == DropPreviewState.Replace;
                _replaceHintCg.alpha = showHint ? 1f : 0f;
                if (showHint)
                {
                    int n = evictCount > 0 ? evictCount : _currentSlotSize;
                    _replaceHintLabel.text = $"↺ 替换 {n} 个部件";
                }
            }
        }

        /// <summary>
        /// Compute the drag anchor offset.
        /// Returns Vector2.zero so the ghost center always follows the cursor.
        /// </summary>
        public Vector2 ComputeDragOffset(PointerEventData eventData, RectTransform sourceRect = null)
        {
            return Vector2.zero;
        }

        /// <summary>
        /// Move the ghost to follow the pointer position, applying the drag anchor offset.
        /// </summary>
        /// <param name="eventData">Current pointer event data.</param>
        /// <param name="dragOffset">Offset computed by ComputeDragOffset at drag start.</param>
        public void FollowPointer(PointerEventData eventData, Vector2 dragOffset = default)
        {
            if (_rectTransform == null || _parentCanvas == null) return;

            var canvasRect = _parentCanvas.transform as RectTransform;
            if (canvasRect == null) return;

            Camera cam = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _parentCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, cam, out var localPoint))
            {
                // BuildShapeCellsAbsolute uses anchorMin=anchorMax=zero (Ghost's bottom-left corner)
                // with pivot=(0,1), so cells expand right-downward from the Ghost's bottom-left.
                // Ghost pivot=(0.5,0.5) means localPosition places the Ghost center at localPoint.
                //
                // To center the visual content on the cursor:
                //   - Ghost center is at localPoint
                //   - Ghost bottom-left (anchor) = localPoint + (-size.x*0.5, -size.y*0.5)
                //   - Cell[0,0] top-left = Ghost bottom-left = localPoint + (-size.x*0.5, -size.y*0.5)
                //   - Visual center ≈ localPoint + (0, -size.y*0.5)  (cells go downward from anchor)
                //
                // So we need to shift Ghost UP by size.y to bring visual center to cursor:
                //   centeringOffset = (0, size.y * 1.0f)
                Vector2 size = _rectTransform.sizeDelta;
                Vector2 centeringOffset = new Vector2(0f, size.y * 1.0f);
                _rectTransform.localPosition = localPoint + centeringOffset + dragOffset;
            }
        }

        /// <summary> Hide the ghost with a shrink animation. </summary>
        public void Hide()
        {
            // 用 alpha 判断是否已经隐藏，避免重复触发
            if (_canvasGroup == null || _canvasGroup.alpha <= 0f) return;

            _scaleTween.Stop();

            if (_rectTransform != null)
            {
                _scaleTween = Tween.Scale(_rectTransform, endValue: Vector3.zero,
                    duration: 0.08f, ease: Ease.InQuad, useUnscaledTime: true)
                    .OnComplete(() =>
                    {
                        // 用 alpha=0 隐藏，绝不调用 SetActive(false)（CLAUDE.md 第11条）
                        if (_canvasGroup != null)
                            _canvasGroup.alpha = 0f;
                        // 重置 scale，为下次 Show() 准备干净状态
                        if (_rectTransform != null)
                            _rectTransform.localScale = Vector3.one;
                        // 隐藏 name label via CanvasGroup
                        if (_nameLabelCg != null)
                            _nameLabelCg.alpha = 0f;
                    });
            }
            else
            {
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_cellImages != null)
            {
                foreach (var img in _cellImages)
                    if (img != null && img.gameObject != null)
                        Destroy(img.gameObject);
            }
            if (_iconImageDynamic != null && _iconImageDynamic.gameObject != null)
                Destroy(_iconImageDynamic.gameObject);
        }
    }
}
