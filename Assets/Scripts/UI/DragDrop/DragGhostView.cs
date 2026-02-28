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
    /// Must live under the StarChart Canvas with <c>CanvasGroup.blocksRaycasts = false</c>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DragGhostView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private TMP_Text _replaceHintLabel;
        [SerializeField] private float _ghostAlpha = 0.7f;
        [SerializeField] private Vector2 _ghostSize = new(64f, 64f);

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;
        private Tween _scaleTween;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_rectTransform != null)
                _rectTransform.sizeDelta = _ghostSize;

            // Initialize border and hint
            if (_borderImage != null)
                _borderImage.color = Color.clear;

            if (_replaceHintLabel != null)
                _replaceHintLabel.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }

        /// <summary> Show the ghost with the item's icon or placeholder color. </summary>
        public void Show(StarChartItemSO item)
        {
            if (item == null) return;

            gameObject.SetActive(true);

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

            if (_canvasGroup != null)
                _canvasGroup.alpha = _ghostAlpha;

            // Reset drop state
            SetDropState(DropPreviewState.None);

            // Pop-in animation: scale 0.8 → 1.0
            _scaleTween.Stop();
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
                    _replaceHintLabel.text = "↺ REPLACE";
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
            if (!gameObject.activeSelf) return;

            _scaleTween.Stop();

            if (_rectTransform != null)
            {
                _scaleTween = Tween.Scale(_rectTransform, endValue: Vector3.zero,
                    duration: 0.08f, ease: Ease.InQuad, useUnscaledTime: true)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
