using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Semi-transparent ghost that follows the mouse during a drag operation.
    /// Must live under the StarChart Canvas with <c>CanvasGroup.blocksRaycasts = false</c>
    /// so it doesn't intercept pointer events intended for drop targets.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DragGhostView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private float _ghostAlpha = 0.7f;
        [SerializeField] private Vector2 _ghostSize = new(64f, 64f);

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _parentCanvas;

        // Placeholder colors — mirrors InventoryItemView scheme
        private static readonly Color CORE_COLOR = new(0.9f, 0.55f, 0.1f, 1f);
        private static readonly Color PRISM_COLOR = new(0.4f, 0.6f, 0.9f, 1f);
        private static readonly Color DEFAULT_COLOR = new(0.5f, 0.5f, 0.5f, 1f);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();

            // Ensure this ghost never blocks raycasts
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            // Set fixed size
            if (_rectTransform != null)
                _rectTransform.sizeDelta = _ghostSize;

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
                    // Use a white placeholder sprite tinted by type color
                    _iconImage.sprite = null;
                    var baseColor = GetPlaceholderColor(item);
                    _iconImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, _ghostAlpha);
                }
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = _ghostAlpha;
        }

        /// <summary>
        /// Move the ghost to follow the pointer position.
        /// Uses ScreenPointToLocalPointInRectangle for Canvas-space accuracy.
        /// </summary>
        public void FollowPointer(PointerEventData eventData)
        {
            if (_rectTransform == null || _parentCanvas == null) return;

            // Get the root Canvas's RectTransform for coordinate conversion
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

        /// <summary> Hide the ghost and deactivate. </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static Color GetPlaceholderColor(StarChartItemSO item)
        {
            return item.ItemType switch
            {
                StarChartItemType.Core => CORE_COLOR,
                StarChartItemType.Prism => PRISM_COLOR,
                _ => DEFAULT_COLOR,
            };
        }
    }
}
