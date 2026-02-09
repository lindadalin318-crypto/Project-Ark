using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.UI
{
    /// <summary>
    /// Applies a subtle parallax offset to the star-chart panel root
    /// based on mouse position (or right stick on gamepad).
    /// Runs on <c>Time.unscaledDeltaTime</c> so it works during pause.
    /// Attach to the StarChartPanel root RectTransform.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIParallaxEffect : MonoBehaviour
    {
        [Tooltip("Maximum parallax offset in pixels.")]
        [SerializeField] private float _maxOffset = 15f;

        [Tooltip("Smoothing speed for the offset interpolation.")]
        [SerializeField] private float _smoothSpeed = 5f;

        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private Vector2 _currentOffset;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            // Snapshot the anchored position as the "origin" when the panel opens.
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _currentOffset = Vector2.zero;
        }

        private void OnDisable()
        {
            ResetOffset();
        }

        private void Update()
        {
            Vector2 normalizedInput = GetNormalizedInput();

            // Invert for parallax feel (UI moves opposite to pointer).
            Vector2 target = -normalizedInput * _maxOffset;

            _currentOffset = Vector2.Lerp(
                _currentOffset,
                target,
                _smoothSpeed * Time.unscaledDeltaTime);

            _rectTransform.anchoredPosition = _baseAnchoredPosition + _currentOffset;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Immediately snap the offset back to zero.
        /// Called automatically on <c>OnDisable</c>.
        /// </summary>
        public void ResetOffset()
        {
            _currentOffset = Vector2.zero;
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = _baseAnchoredPosition;
        }

        // ══════════════════════════════════════════════════════════════
        // Input helpers
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a (-1, 1) vector representing the pointer deviation
        /// from screen center. Gamepad right-stick takes priority when active.
        /// </summary>
        private Vector2 GetNormalizedInput()
        {
            // Prefer gamepad right stick if a gamepad is connected and active.
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f)
                    return Vector2.ClampMagnitude(stick, 1f);
            }

            // Fall back to mouse position relative to screen center.
            if (Mouse.current == null) return Vector2.zero;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float nx = (mousePos.x / Screen.width - 0.5f) * 2f;   // -1 .. +1
            float ny = (mousePos.y / Screen.height - 0.5f) * 2f;
            return Vector2.ClampMagnitude(new Vector2(nx, ny), 1f);
        }
    }
}
