using UnityEngine;
using PrimeTween;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Visual juice effects for the ship: movement tilt and squash/stretch.
    /// Operates on a visual child transform so physics and aiming are unaffected.
    ///
    /// Driven by ShipView (Worker pattern):
    ///   - Initialize() passes component references at startup
    ///   - OnSpeedChanged() routed from ShipMotor.OnSpeedChanged via ShipView
    ///   - OnDashStarted() routed from ShipStateController.OnStateChanged via ShipView
    ///   - LateUpdate() runs movement tilt internally (reads cached motor velocity)
    ///
    /// Does NOT subscribe to events directly — ShipView routes all signals.
    /// </summary>
    public class ShipVisualJuice : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visual child transform (SpriteRenderer). Must be a direct child.")]
        [SerializeField] private Transform _visualChild;

        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        [Header("Enable Toggles")]
        [Tooltip("Master switch — when OFF, all juice visuals are silently skipped.")]
        [SerializeField] private bool _enableAll = true;

        [Tooltip("Movement tilt (bank when strafing).")]
        [SerializeField] private bool _enableMoveTilt = true;

        [Tooltip("Squash/stretch on speed change.")]
        [SerializeField] private bool _enableSquashStretch = true;

        [Tooltip("Runtime body sprite swap for left/right lean readability.")]
        [SerializeField] private bool _enableLeanSpriteSwap = true;

        // ══════════════════════════════════════════════════════════════
        // Cached Components (injected by ShipView via Initialize)
        // ══════════════════════════════════════════════════════════════

        private ShipMotor _motor;
        private ShipDash _dash;
        private ShipBoost _boost;
        private ShipAiming _aiming;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _highlightRenderer;
        private Sprite _normalBodySprite;
        private Color _highlightBaseColor;

        // ══════════════════════════════════════════════════════════════
        // State
        // ══════════════════════════════════════════════════════════════

        private float _currentTiltAngle;
        private float _previousNormalizedSpeed;
        private Tween _squashStretchTween;
        private bool _initialized;

        // ══════════════════════════════════════════════════════════════
        // Initialization (called by ShipView after Awake)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called by ShipView after Awake to inject component references.
        /// </summary>
        public void Initialize(ShipMotor motor, ShipDash dash, ShipBoost boost, ShipAiming aiming)
        {
            _motor = motor;
            _dash = dash;
            _boost = boost;
            _aiming = aiming;
            _initialized = true;

            if (_visualChild == null)
            {
                Debug.LogError("[ShipVisualJuice] No visual child assigned. Juice effects will not work.", this);
            }
        }

        /// <summary>
        /// Called by ShipView after Awake to inject component references and the body renderer used for runtime sprite readability.
        /// </summary>
        public void Initialize(ShipMotor motor, ShipDash dash, ShipBoost boost, ShipAiming aiming, SpriteRenderer bodyRenderer)
        {
            Initialize(motor, dash, boost, aiming, bodyRenderer, null);
        }

        /// <summary>
        /// Called by ShipView after Awake to inject component references and ship renderers used for runtime sprite readability.
        /// </summary>
        public void Initialize(ShipMotor motor, ShipDash dash, ShipBoost boost, ShipAiming aiming, SpriteRenderer bodyRenderer, SpriteRenderer highlightRenderer)
        {
            Initialize(motor, dash, boost, aiming);
            _bodyRenderer = bodyRenderer;
            _highlightRenderer = highlightRenderer;
            _highlightBaseColor = _highlightRenderer != null ? _highlightRenderer.color : Color.white;
            _normalBodySprite = _juiceSettings != null && _juiceSettings.NormalBodySprite != null
                ? _juiceSettings.NormalBodySprite
                : _bodyRenderer != null ? _bodyRenderer.sprite : null;
        }

        // ══════════════════════════════════════════════════════════════
        // LateUpdate — movement tilt (runs every frame)
        // ══════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (!_initialized || !_enableAll || !_enableMoveTilt) return;
            if (_visualChild == null || _juiceSettings == null || _motor == null) return;

            UpdateMovementTilt();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — called by ShipView
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Routed from ShipMotor.OnSpeedChanged via ShipView.
        /// Triggers squash/stretch on significant speed delta.
        /// </summary>
        public void OnSpeedChanged(float normalizedSpeed)
        {
            if (!_enableAll || !_enableSquashStretch) return;
            if (_juiceSettings == null || _visualChild == null) return;

            float delta = normalizedSpeed - _previousNormalizedSpeed;
            _previousNormalizedSpeed = normalizedSpeed;

            // Dash/Boost 链路下不要对整船视觉根做 squash/stretch，避免船体被压窄
            if ((_dash != null && _dash.IsDashing) || (_boost != null && _boost.IsBoosting))
            {
                if (_squashStretchTween.isAlive)
                    _squashStretchTween.Stop();

                _visualChild.localScale = Vector3.one;
                return;
            }

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

        /// <summary>
        /// Routed from ShipStateController.OnStateChanged (Dash) via ShipView.
        /// Cancels squash/stretch and resets scale.
        /// </summary>
        public void OnDashStarted()
        {
            if (_visualChild == null) return;

            if (_squashStretchTween.isAlive)
                _squashStretchTween.Stop();

            _visualChild.localScale = Vector3.one;
        }

        /// <summary>
        /// Re-applies the movement lean sprite after another worker temporarily owns the body sprite.
        /// </summary>
        public void RefreshLeanSprite()
        {
            if (!_initialized || !_enableAll || !_enableLeanSpriteSwap) return;
            if (_bodyRenderer == null || _juiceSettings == null || _motor == null) return;
            if (_dash != null && _dash.IsDashing) return;

            ApplyLeanSprite();
        }

        /// <summary>
        /// Resets all juice state to defaults.
        /// Called by ShipView.ResetVFX() for object pool return.
        /// </summary>
        public void ResetState()
        {
            _previousNormalizedSpeed = 0f;
            _currentTiltAngle = 0f;

            if (_squashStretchTween.isAlive)
                _squashStretchTween.Stop();

            if (_visualChild != null)
            {
                _visualChild.localRotation = Quaternion.identity;
                _visualChild.localScale = Vector3.one;
            }

            if (_bodyRenderer != null && _normalBodySprite != null)
                _bodyRenderer.sprite = _normalBodySprite;

            SetHighlightVisible(true);
        }

        // ══════════════════════════════════════════════════════════════
        // Movement Tilt (internal, driven by LateUpdate)
        // ══════════════════════════════════════════════════════════════

        private void UpdateMovementTilt()
        {
            Vector2 velocity = _motor.CurrentVelocity;

            if (velocity.sqrMagnitude < 0.01f)
            {
                if (_enableLeanSpriteSwap && (_dash == null || !_dash.IsDashing))
                    ApplyBodySprite(_normalBodySprite);

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

            if (_enableLeanSpriteSwap && _bodyRenderer != null && (_dash == null || !_dash.IsDashing))
                ApplyLeanSprite(lateralComponent);

            // Map to tilt angle (moving right → tilt left, i.e., negative angle for visual leaning)
            float targetTilt = -lateralComponent * _juiceSettings.MoveTiltMaxAngle;

            _currentTiltAngle = Mathf.Lerp(_currentTiltAngle, targetTilt, _juiceSettings.TiltSmoothSpeed * Time.deltaTime);
            _visualChild.localRotation = Quaternion.Euler(0f, 0f, _currentTiltAngle);
        }

        private void ApplyLeanSprite()
        {
            if (_motor == null) return;

            Vector2 velocity = _motor.CurrentVelocity;
            if (velocity.sqrMagnitude < 0.01f)
            {
                ApplyBodySprite(_normalBodySprite);
                return;
            }

            Vector2 facingDir = _aiming != null ? _aiming.FacingDirection : (Vector2)transform.up;
            Vector2 rightDir = new Vector2(facingDir.y, -facingDir.x);
            float lateralComponent = Vector2.Dot(velocity.normalized, rightDir);
            ApplyLeanSprite(lateralComponent);
        }

        private void ApplyLeanSprite(float lateralComponent)
        {
            Sprite target = SelectLeanSprite(
                _normalBodySprite,
                _juiceSettings.LeanLeftSprites,
                _juiceSettings.LeanRightSprites,
                lateralComponent,
                _juiceSettings.LeanSpriteDeadZone);

            ApplyBodySprite(target);
        }

        private void ApplyBodySprite(Sprite sprite)
        {
            if (_bodyRenderer == null || sprite == null) return;

            SetHighlightVisible(sprite == _normalBodySprite);

            if (_bodyRenderer.sprite == sprite) return;

            _bodyRenderer.sprite = sprite;
        }

        private void SetHighlightVisible(bool visible)
        {
            if (_highlightRenderer == null) return;

            Color c = _highlightBaseColor;
            c.a = visible ? _highlightBaseColor.a : 0f;
            _highlightRenderer.color = c;
        }

        private static Sprite SelectLeanSprite(Sprite normalSprite, Sprite[] leftSprites, Sprite[] rightSprites, float lateralComponent, float deadZone)
        {
            float abs = Mathf.Abs(lateralComponent);
            if (abs <= deadZone)
                return normalSprite;

            Sprite[] frames = lateralComponent < 0f ? leftSprites : rightSprites;
            if (frames == null || frames.Length == 0)
                return normalSprite;

            float t = Mathf.InverseLerp(deadZone, 1f, abs);
            int index = Mathf.Clamp(Mathf.CeilToInt(t * frames.Length) - 1, 0, frames.Length - 1);
            return frames[index] != null ? frames[index] : normalSprite;
        }
    }
}
