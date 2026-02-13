using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.Level
{
    /// <summary>
    /// Simple UI line connecting two rooms on the map.
    /// Uses a stretched Image to draw a line between two points.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MapConnectionLine : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        private static readonly Color COLOR_NORMAL = new(0.5f, 0.6f, 0.7f, 0.6f);
        private static readonly Color COLOR_LAYER = new(0.9f, 0.65f, 0.2f, 0.8f);
        private const float LINE_THICKNESS = 2f;
        private const float LAYER_LINE_THICKNESS = 3f;

        // ──────────────────── State ────────────────────

        private Image _lineImage;
        private RectTransform _rectTransform;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _lineImage = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _lineImage.raycastTarget = false;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Draw a line between two UI positions (local to the parent container).
        /// </summary>
        /// <param name="from">Start position in local coordinates.</param>
        /// <param name="to">End position in local coordinates.</param>
        /// <param name="isLayerTransition">If true, uses thicker/colored line for layer transitions.</param>
        public void Setup(Vector2 from, Vector2 to, bool isLayerTransition = false)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float thickness = isLayerTransition ? LAYER_LINE_THICKNESS : LINE_THICKNESS;

            _rectTransform.anchoredPosition = from;
            _rectTransform.sizeDelta = new Vector2(distance, thickness);
            _rectTransform.pivot = new Vector2(0f, 0.5f);
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            _lineImage.color = isLayerTransition ? COLOR_LAYER : COLOR_NORMAL;
        }
    }
}
