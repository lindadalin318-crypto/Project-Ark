using UnityEngine;
using PrimeTween;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Visual juice effects for the ship: movement tilt and squash/stretch.
    /// Operates on a visual child transform so physics and aiming are unaffected.
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    public class ShipVisualJuice : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visual child transform (SpriteRenderer). Must be a direct child.")]
        [SerializeField] private Transform _visualChild;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached Components
        // ══════════════════════════════════════════════════════════════

        private ShipMotor _motor;
        private ShipDash _dash;
        private ShipAiming _aiming;

        // ══════════════════════════════════════════════════════════════
        // State
        // ══════════════════════════════════════════════════════════════

        private float _currentTiltAngle;
        private float _previousNormalizedSpeed;
        private Tween _squashStretchTween;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
            _dash = GetComponent<ShipDash>();
            _aiming = GetComponent<ShipAiming>();

            if (_visualChild == null)
            {
                Debug.LogWarning("[ShipVisualJuice] No visual child assigned. Disabling.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_motor != null) _motor.OnSpeedChanged += OnSpeedChanged;
            if (_dash != null) _dash.OnDashStarted += OnDashStarted;
        }

        private void OnDisable()
        {
            if (_motor != null) _motor.OnSpeedChanged -= OnSpeedChanged;
            if (_dash != null) _dash.OnDashStarted -= OnDashStarted;

            // Reset visual state
            if (_visualChild != null)
            {
                _visualChild.localRotation = Quaternion.identity;
                _visualChild.localScale = Vector3.one;
            }
        }

        private void LateUpdate()
        {
            if (_visualChild == null || _juiceSettings == null) return;

            UpdateMovementTilt();
        }

        // ══════════════════════════════════════════════════════════════
        // Movement Tilt
        // ══════════════════════════════════════════════════════════════

        private void UpdateMovementTilt()
        {
            Vector2 velocity = _motor.CurrentVelocity;

            if (velocity.sqrMagnitude < 0.01f)
            {
                // Smoothly return to neutral
                _currentTiltAngle = Mathf.Lerp(_currentTiltAngle, 0f, _juiceSettings.TiltSmoothSpeed * Time.deltaTime);
                _visualChild.localRotation = Quaternion.Euler(0f, 0f, _currentTiltAngle);
                return;
            }

            // Calculate lateral component relative to the ship's facing direction
            Vector2 facingDir = _aiming != null ? _aiming.FacingDirection : (Vector2)transform.up;
            Vector2 rightDir = new Vector2(facingDir.y, -facingDir.x); // Perpendicular (right)

            // Dot product: positive = moving right, negative = moving left
            float lateralComponent = Vector2.Dot(velocity.normalized, rightDir);

            // Map to tilt angle (moving right → tilt left, i.e., negative angle for visual leaning)
            float targetTilt = -lateralComponent * _juiceSettings.MoveTiltMaxAngle;

            _currentTiltAngle = Mathf.Lerp(_currentTiltAngle, targetTilt, _juiceSettings.TiltSmoothSpeed * Time.deltaTime);
            _visualChild.localRotation = Quaternion.Euler(0f, 0f, _currentTiltAngle);
        }

        // ══════════════════════════════════════════════════════════════
        // Squash & Stretch
        // ══════════════════════════════════════════════════════════════

        private void OnSpeedChanged(float normalizedSpeed)
        {
            if (_juiceSettings == null || _visualChild == null) return;

            float delta = normalizedSpeed - _previousNormalizedSpeed;
            _previousNormalizedSpeed = normalizedSpeed;

            // Only trigger on significant speed changes
            float threshold = 0.15f;
            if (Mathf.Abs(delta) < threshold) return;

            float intensity = _juiceSettings.SquashStretchIntensity;
            float duration = _juiceSettings.SquashStretchDuration;

            Vector3 targetScale;
            if (delta > 0f)
            {
                // Accelerating → stretch along Y, squash X
                targetScale = new Vector3(1f - intensity, 1f + intensity, 1f);
            }
            else
            {
                // Decelerating → squash along Y, stretch X
                targetScale = new Vector3(1f + intensity, 1f - intensity, 1f);
            }

            // Cancel previous tween if active
            if (_squashStretchTween.isAlive) _squashStretchTween.Stop();

            // Animate to squash/stretch then back to normal
            _squashStretchTween = Tween.Scale(_visualChild, targetScale, duration * 0.5f, Ease.OutQuad,
                cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        private void OnDashStarted(Vector2 dashDirection)
        {
            if (_juiceSettings == null || _visualChild == null) return;

            float intensity = _juiceSettings.SquashStretchIntensity * 2f; // Stronger for dash
            float duration = _juiceSettings.SquashStretchDuration;

            // Stretch along movement axis
            Vector3 stretchScale = new Vector3(1f - intensity, 1f + intensity, 1f);

            if (_squashStretchTween.isAlive) _squashStretchTween.Stop();

            _squashStretchTween = Tween.Scale(_visualChild, stretchScale, duration * 0.5f, Ease.OutQuad,
                cycles: 2, cycleMode: CycleMode.Yoyo);
        }
    }
}
