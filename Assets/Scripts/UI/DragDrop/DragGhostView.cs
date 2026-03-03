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
    /// Supports shape-aware rendering: adjusts height based on SlotSize (1-3).
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

        // Cyan cell color matching SlotCellView style
        private static readonly Color CellActiveColor = new Color(0f, 0.85f, 1f, 0.18f);

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

            // CLAUDE.md 第11条：uGUI面板禁止用SetActive控制显隐，统一用CanvasGroup
            if (_replaceHintLabel != null)
            {
                _replaceHintCg = _replaceHintLabel.GetComponent<CanvasGroup>()
                    ?? _replaceHintLabel.gameObject.AddComponent<CanvasGroup>();
                if (_replaceHintCg != null)
                {
                    _replaceHintCg.alpha = 0f;
                    _replaceHintCg.blocksRaycasts = false;
                    _replaceHintCg.interactable = false;
                }
                else
                {
                    Debug.LogError($"[DragGhostView] Failed to get/add CanvasGroup on ReplaceHint '{_replaceHintLabel.gameObject.name}'", _replaceHintLabel.gameObject);
                }
            }

            if (_nameLabel != null)
            {
                _nameLabelCg = _nameLabel.GetComponent<CanvasGroup>()
                    ?? _nameLabel.gameObject.AddComponent<CanvasGroup>();
                _nameLabelCg.alpha = 0f;
                _nameLabelCg.blocksRaycasts = false;
                _nameLabelCg.interactable = false;
            }

            // NOTE: 绝不调用 gameObject.SetActive(false)！
            // Ghost GameObject 始终保持 active，由 CanvasGroup.alpha 控制可见性
        }

        /// <summary>
        /// Adjust the ghost shape to match the item's SlotSize (legacy 1D).
        /// Dynamically creates/updates cell grid images.
        /// </summary>
        public void SetShape(int slotSize)
        {
            slotSize = Mathf.Clamp(slotSize, 1, 3);
            _currentSlotSize = slotSize;

            // Calculate new height: N cells + (N-1) gaps
            float newHeight = slotSize * _cellHeight + (slotSize - 1) * _cellGap;
            float width = _ghostSize.x;

            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(width, newHeight);

            // Rebuild cell grid images
            RebuildCellGrid(slotSize, width, newHeight);
        }

        /// <summary>
        /// Adjust the ghost shape to match the item's 2D ItemShape.
        /// Resizes the ghost to the shape's bounding box and rebuilds the cell grid.
        /// </summary>
        public void SetShape(ItemShape shape)
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

            RebuildShapeGrid(shape, newWidth, newHeight);
        }

        private void RebuildCellGrid(int slotSize, float width, float totalHeight)
        {
            // Destroy old cell images
            if (_cellImages != null)
            {
                foreach (var img in _cellImages)
                    if (img != null) Destroy(img.gameObject);
            }

            _cellImages = new Image[slotSize];

            for (int i = 0; i < slotSize; i++)
            {
                var cellGo = new GameObject($"GhostCell_{i}", typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(transform, false);

                // Place cell behind icon/border (sibling index 0)
                cellGo.transform.SetAsFirstSibling();

                var cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(1f, 1f);
                cellRect.pivot = new Vector2(0.5f, 1f);

                // Position from top: each cell offset by (cellHeight + gap) * i
                float yOffset = -(i * (_cellHeight + _cellGap));
                cellRect.anchoredPosition = new Vector2(0f, yOffset);
                cellRect.sizeDelta = new Vector2(0f, _cellHeight);

                var cellImg = cellGo.GetComponent<Image>();
                cellImg.color = CellActiveColor;
                _cellImages[i] = cellImg;
            }
        }

        private void RebuildShapeGrid(ItemShape shape, float totalWidth, float totalHeight)
        {
            // Destroy old cell images
            if (_cellImages != null)
            {
                foreach (var img in _cellImages)
                    if (img != null) Destroy(img.gameObject);
            }

            var cells = ItemShapeHelper.GetCells(shape);
            var bounds = ItemShapeHelper.GetBounds(shape);
            _cellImages = new Image[cells.Count];

            float cellW = (totalWidth  - (bounds.x - 1) * _cellGap) / bounds.x;
            float cellH = (totalHeight - (bounds.y - 1) * _cellGap) / bounds.y;

            for (int i = 0; i < cells.Count; i++)
            {
                var offset = cells[i];
                var cellGo = new GameObject($"GhostCell_{offset.x}_{offset.y}", typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(transform, false);
                cellGo.transform.SetAsFirstSibling();

                var cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.anchorMin = Vector2.zero;
                cellRect.anchorMax = Vector2.zero;
                cellRect.pivot = new Vector2(0f, 1f);

                float xPos =  offset.x * (cellW + _cellGap);
                float yPos = -offset.y * (cellH + _cellGap);
                cellRect.anchoredPosition = new Vector2(xPos, yPos);
                cellRect.sizeDelta = new Vector2(cellW, cellH);

                var cellImg = cellGo.GetComponent<Image>();
                cellImg.color = CellActiveColor;
                _cellImages[i] = cellImg;
            }
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

            // Apply shape based on ItemShape (2D)
            SetShape(item.Shape);

            if (_iconImage != null)
            {
                if (item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.color = new Color(1f, 1f, 1f, _ghostAlpha);
                }
                else
                {
                    _iconImage.sprite = null;
                    var baseColor = StarChartTheme.GetTypeColor(item.ItemType);
                    _iconImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, _ghostAlpha);
                }
            }

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
            Color borderColor = state switch
            {
                DropPreviewState.Valid   => StarChartTheme.HighlightValid,
                DropPreviewState.Replace => StarChartTheme.HighlightReplace,
                DropPreviewState.Invalid => StarChartTheme.HighlightInvalid,
                _                        => Color.clear
            };

            if (_borderImage != null)
            {
                _borderColorTween.Stop();
                _borderColorTween = Tween.Color(_borderImage, endValue: borderColor,
                    duration: 0.08f, ease: Ease.OutQuad, useUnscaledTime: true);
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
        /// Move the ghost to follow the pointer position.
        /// </summary>
        public void FollowPointer(PointerEventData eventData)
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
                _rectTransform.localPosition = localPoint;
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
        }
    }
}
