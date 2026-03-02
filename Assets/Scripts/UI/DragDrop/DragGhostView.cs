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

        // Dynamically created cell grid images
        private Image[] _cellImages;
        private int _currentSlotSize = 1;

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

            if (_replaceHintLabel != null)
                _replaceHintLabel.gameObject.SetActive(false);

            if (_nameLabel != null)
                _nameLabel.gameObject.SetActive(false);

            // NOTE: 绝不调用 gameObject.SetActive(false)！
            // Ghost GameObject 始终保持 active，由 CanvasGroup.alpha 控制可见性
        }

        /// <summary>
        /// Adjust the ghost shape to match the item's SlotSize.
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

        /// <summary> Show the ghost with the item's icon or placeholder color. </summary>
        public void Show(StarChartItemSO item)
        {
            if (item == null) return;

            // Stop any in-flight hide tween
            _scaleTween.Stop();

            // Reset to clean state before showing
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one;

            // Apply shape based on SlotSize
            SetShape(item.SlotSize);

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

            // Show name label
            if (_nameLabel != null)
            {
                _nameLabel.text = item.DisplayName;
                _nameLabel.gameObject.SetActive(true);
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
        /// </summary>
        public void SetDropState(DropPreviewState state)
        {
            Color borderColor = state switch
            {
                DropPreviewState.Valid   => StarChartTheme.HighlightValid,
                DropPreviewState.Replace => StarChartTheme.HighlightReplace,
                DropPreviewState.Invalid => StarChartTheme.HighlightInvalid,
                _                        => Color.clear
            };

            if (_borderImage != null)
                _borderImage.color = borderColor;

            if (_replaceHintLabel != null)
            {
                bool showHint = state == DropPreviewState.Replace;
                _replaceHintLabel.gameObject.SetActive(showHint);
                if (showHint)
                    _replaceHintLabel.text = $"↺ REPLACE {_currentSlotSize}";
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
                        // 用 alpha=0 隐藏，绝不调用 SetActive(false)
                        if (_canvasGroup != null)
                            _canvasGroup.alpha = 0f;
                        // 重置 scale，为下次 Show() 准备干净状态
                        if (_rectTransform != null)
                            _rectTransform.localScale = Vector3.one;
                        if (_nameLabel != null)
                            _nameLabel.gameObject.SetActive(false);
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
