using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Manages the ship's multi-layer sprite visual state and all VFX feedback.
    /// Responds to ShipBoost and ShipDash events to drive:
    ///   - Liquid/glow layer brightness ramp on Boost
    ///   - Solid layer alpha flicker during Dash i-frames
    ///   - Boost TrailRenderer (BoostTrail)
    ///   - Dodge_Sprite static ghost at dash origin
    ///   - Ship_Sprite_Back thruster pulse animation (PrimeTween)
    ///
    /// All visual logic lives here; ShipBoost and ShipDash contain no rendering code.
    /// Call ResetVFX() when returning to object pool.
    /// </summary>
    public class ShipView : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References — 5-layer sprite structure
        // ══════════════════════════════════════════════════════════════

        [Header("Sprite Layers")]
        [Tooltip("Rear thruster layer (SortOrder -3).")]
        [SerializeField] private SpriteRenderer _backRenderer;

        [Tooltip("Energy/glow layer (SortOrder -2). Uses Additive material.")]
        [SerializeField] private SpriteRenderer _liquidRenderer;

        [Tooltip("Highlight layer (SortOrder -1). Default alpha 0.5.")]
        [SerializeField] private SpriteRenderer _hlRenderer;

        [Tooltip("Main solid body layer (SortOrder 0).")]
        [SerializeField] private SpriteRenderer _solidRenderer;

        [Tooltip("Core/cockpit layer (SortOrder 1). Placeholder — no sprite required.")]
        [SerializeField] private SpriteRenderer _coreRenderer;

        // ══════════════════════════════════════════════════════════════
        // Serialized References — VFX Components
        // ══════════════════════════════════════════════════════════════

        [Header("VFX — Boost Trail")]
        [Tooltip("TrailRenderer on BoostTrail GO (child of Ship_Sprite_Back).")]
        [SerializeField] private TrailRenderer _boostTrail;

        [Header("VFX — Dodge Sprite")]
        [Tooltip("SpriteRenderer on Dodge_Sprite GO (child of ShipVisual, SortOrder -1).")]
        [SerializeField] private SpriteRenderer _dodgeSprite;

        [Header("VFX — Thruster Pulse")]
        [Tooltip("Transform of Ship_Sprite_Back for scale pulse animation.")]
        [SerializeField] private Transform _backSpriteTransform;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached Components
        // ══════════════════════════════════════════════════════════════

        private ShipBoost  _boost;
        private ShipDash   _dash;
        private ShipStateController _stateController; // primary VFX driver

        // Parent of Dodge_Sprite (to re-attach after dash)
        private Transform _dodgeSpriteOriginalParent;

        // Baseline colors captured at Awake so tweens can restore them correctly
        private Color _liquidBaseColor;
        private Color _solidBaseColor;

        // Active tweens — killed before starting a new one to avoid conflicts
        private Tween    _liquidTween;
        private Tween    _dodgeFadeTween;
        private Tween    _thrusterEntryTween;
        private Sequence _thrusterLoopSequence;

        // i-frame flicker cancellation
        private CancellationTokenSource _iFrameCts;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _boost           = GetComponent<ShipBoost>();
            _dash            = GetComponent<ShipDash>();
            _stateController = GetComponent<ShipStateController>(); // optional

            if (_liquidRenderer != null) _liquidBaseColor = _liquidRenderer.color;
            if (_solidRenderer  != null) _solidBaseColor  = _solidRenderer.color;

            // HL layer default alpha = 0.5 (per spec)
            if (_hlRenderer != null)
            {
                var c = _hlRenderer.color;
                c.a = 0.5f;
                _hlRenderer.color = c;
            }

            // Cache Dodge_Sprite parent for re-attachment
            if (_dodgeSprite != null)
            {
                _dodgeSpriteOriginalParent = _dodgeSprite.transform.parent;
                // Ensure starts hidden and inactive
                var dc = _dodgeSprite.color;
                dc.a = 0f;
                _dodgeSprite.color = dc;
                _dodgeSprite.gameObject.SetActive(false);
            }

            // Ensure BoostTrail starts off
            if (_boostTrail != null)
            {
                _boostTrail.emitting = false;
                _boostTrail.Clear();
            }
        }

        private void OnEnable()
        {
            // Primary: subscribe to ShipStateController for unified VFX dispatch
            if (_stateController != null)
            {
                _stateController.OnStateChanged += HandleStateChanged;
            }
            else
            {
                // Legacy fallback: subscribe to individual component events
                if (_boost != null)
                {
                    _boost.OnBoostStarted += HandleBoostStarted;
                    _boost.OnBoostEnded   += HandleBoostEnded;
                }

                if (_dash != null)
                {
                    _dash.OnDashStarted += HandleDashStarted;
                    _dash.OnDashEnded   += HandleDashEnded;
                }
            }
        }

        private void OnDisable()
        {
            if (_stateController != null)
            {
                _stateController.OnStateChanged -= HandleStateChanged;
            }
            else
            {
                if (_boost != null)
                {
                    _boost.OnBoostStarted -= HandleBoostStarted;
                    _boost.OnBoostEnded   -= HandleBoostEnded;
                }

                if (_dash != null)
                {
                    _dash.OnDashStarted -= HandleDashStarted;
                    _dash.OnDashEnded   -= HandleDashEnded;
                }
            }

            ResetVFX();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Resets all VFX to their initial state.
        /// Call this when returning the ship to an object pool.
        /// </summary>
        public void ResetVFX()
        {
            // Cancel i-frame flicker
            CancelIFrameFlicker();

            // Kill all active tweens
            _liquidTween.Stop();
            _dodgeFadeTween.Stop();
            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();

            // Restore baseline colors
            if (_liquidRenderer != null) _liquidRenderer.color = _liquidBaseColor;
            if (_solidRenderer  != null) _solidRenderer.color  = _solidBaseColor;

            // Reset BoostTrail
            if (_boostTrail != null)
            {
                _boostTrail.emitting = false;
                _boostTrail.Clear();
            }

            // Reset Dodge_Sprite
            ResetDodgeSprite();

            // Reset thruster scale
            if (_backSpriteTransform != null)
                _backSpriteTransform.localScale = Vector3.one;
        }

        // ══════════════════════════════════════════════════════════════
        // State-Driven VFX  (primary path via ShipStateController)
        // ══════════════════════════════════════════════════════════════

        private void HandleStateChanged(ShipShipState prevState, ShipShipState newState)
        {
            // Dispatch VFX based on new state
            switch (newState)
            {
                case ShipShipState.Boost:
                    HandleBoostStarted();
                    break;

                case ShipShipState.Dash:
                    // Read dash direction from ShipDash if available
                    Vector2 dashDir = (_dash != null) ? _dash.DashDirection : Vector2.zero;
                    HandleDashStarted(dashDir);
                    break;

                case ShipShipState.Normal:
                    // Exiting Boost
                    if (prevState == ShipShipState.Boost)
                        HandleBoostEnded();
                    // Exiting Dash
                    else if (prevState == ShipShipState.Dash)
                        HandleDashEnded();
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Visual — liquid/glow layer brightness
        // ══════════════════════════════════════════════════════════════

        private void HandleBoostStarted()
        {
            if (_juiceSettings == null) return;

            // 1. Glow layer brightness ramp
            if (_liquidRenderer != null)
            {
                _liquidTween.Stop();
                Color target = _liquidBaseColor * _juiceSettings.BoostGlowBrightnessMultiplier;
                target.a = _liquidBaseColor.a;
                _liquidTween = Tween.Color(
                    _liquidRenderer,
                    endValue: target,
                    duration: _juiceSettings.BoostGlowRampUpDuration,
                    ease: Ease.OutQuad);
            }

            // 2. BoostTrail TrailRenderer
            StartBoostTrail();

            // 3. Thruster pulse
            StartThrusterPulse();
        }

        private void HandleBoostEnded()
        {
            if (_juiceSettings == null) return;

            // 1. Glow layer ramp down
            if (_liquidRenderer != null)
            {
                _liquidTween.Stop();
                _liquidTween = Tween.Color(
                    _liquidRenderer,
                    endValue: _liquidBaseColor,
                    duration: _juiceSettings.BoostGlowRampDownDuration,
                    ease: Ease.InQuad);
            }

            // 2. Stop BoostTrail
            StopBoostTrail();

            // 3. Stop thruster pulse
            StopThrusterPulse();
        }

        // ══════════════════════════════════════════════════════════════
        // Boost Trail — TrailRenderer
        // ══════════════════════════════════════════════════════════════

        private void StartBoostTrail()
        {
            if (_boostTrail == null || _juiceSettings == null) return;

            // Apply parameters
            _boostTrail.time               = _juiceSettings.BoostTrailTime;
            _boostTrail.startWidth         = _juiceSettings.BoostTrailStartWidth;
            _boostTrail.endWidth           = 0f;
            _boostTrail.minVertexDistance  = _juiceSettings.BoostTrailMinVertexDistance;

            // Color gradient: teal → transparent
            Color startColor = _juiceSettings.BoostTrailRendererColor;
            Color endColor   = new Color(startColor.r, startColor.g, startColor.b, 0f);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            _boostTrail.colorGradient = gradient;

            _boostTrail.Clear();
            _boostTrail.emitting = true;
        }

        private void StopBoostTrail()
        {
            if (_boostTrail == null) return;
            _boostTrail.emitting = false;
            // Existing trail fades naturally over _boostTrail.time seconds
        }

        // ══════════════════════════════════════════════════════════════
        // Thruster Pulse — Ship_Sprite_Back scale animation
        // ══════════════════════════════════════════════════════════════

        private void StartThrusterPulse()
        {
            if (_backSpriteTransform == null || _juiceSettings == null) return;

            // Stop any running pulse
            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();
            _backSpriteTransform.localScale = Vector3.one;

            float peak     = _juiceSettings.BoostBackScalePeak;
            float duration = _juiceSettings.BoostBackEntryPulseDuration;

            // Entry pulse: 1.0 → peak → 1.0
            _thrusterEntryTween = Tween.Scale(
                _backSpriteTransform,
                startValue: Vector3.one,
                endValue: Vector3.one * peak,
                duration: duration * 0.5f,
                ease: Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_backSpriteTransform == null) return;
                    Tween.Scale(
                        _backSpriteTransform,
                        endValue: Vector3.one,
                        duration: duration * 0.5f,
                        ease: Ease.InQuad)
                        .OnComplete(StartThrusterLoop);
                });
        }

        private void StartThrusterLoop()
        {
            if (_backSpriteTransform == null || _juiceSettings == null) return;

            float pulseScale  = _juiceSettings.BoostBackPulseScale;
            float halfPeriod  = _juiceSettings.BoostBackPulsePeriod * 0.5f;

            _thrusterLoopSequence = Sequence.Create(cycles: -1) // infinite
                .Chain(Tween.Scale(_backSpriteTransform, endValue: Vector3.one * pulseScale, duration: halfPeriod, ease: Ease.InOutSine))
                .Chain(Tween.Scale(_backSpriteTransform, endValue: Vector3.one,              duration: halfPeriod, ease: Ease.InOutSine));
        }

        private void StopThrusterPulse()
        {
            if (_backSpriteTransform == null || _juiceSettings == null) return;

            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();

            // Restore scale smoothly
            Tween.Scale(
                _backSpriteTransform,
                endValue: Vector3.one,
                duration: _juiceSettings.BoostBackRestoreDuration,
                ease: Ease.OutQuad);
        }

        // ══════════════════════════════════════════════════════════════
        // Dash Visual — i-frame flicker + Dodge_Sprite ghost
        // ══════════════════════════════════════════════════════════════

        private void HandleDashStarted(Vector2 _direction)
        {
            if (_juiceSettings == null) return;

            // 1. i-frame flicker on solid layer
            if (_solidRenderer != null)
            {
                CancelIFrameFlicker();
                _iFrameCts = new CancellationTokenSource();
                RunIFrameFlickerAsync(_iFrameCts.Token).Forget();
            }

            // 2. Dodge_Sprite static ghost at dash origin
            SpawnDodgeSprite();
        }

        private void HandleDashEnded()
        {
            // Stop i-frame flicker and restore solid layer
            CancelIFrameFlicker();
            if (_solidRenderer != null)
            {
                _solidRenderer.color = _solidBaseColor;
            }

            // Dodge_Sprite fades on its own; re-parent and deactivate after fade
            // (handled inside SpawnDodgeSprite via tween OnComplete)
        }

        // ══════════════════════════════════════════════════════════════
        // Dodge_Sprite — static ghost at dash origin
        // ══════════════════════════════════════════════════════════════

        private void SpawnDodgeSprite()
        {
            if (_dodgeSprite == null) return;

            // Fallback: use solid renderer sprite if dodge sprite has none
            if (_dodgeSprite.sprite == null && _solidRenderer != null)
                _dodgeSprite.sprite = _solidRenderer.sprite;

            // Detach from parent so it stays at world position
            _dodgeSprite.transform.SetParent(null, worldPositionStays: true);

            // Snap to current ship world position
            _dodgeSprite.transform.position = transform.position;
            _dodgeSprite.transform.rotation = transform.rotation;

            // Set teal color, full alpha
            Color teal = _juiceSettings != null
                ? _juiceSettings.AfterImageColor
                : new Color(0.28f, 0.43f, 0.43f, 1f);
            teal.a = 1f;
            _dodgeSprite.color = teal;
            _dodgeSprite.gameObject.SetActive(true);

            // Fade out over AfterImageFadeDuration
            _dodgeFadeTween.Stop();
            float fadeDuration = _juiceSettings != null ? _juiceSettings.AfterImageFadeDuration : 0.15f;
            _dodgeFadeTween = Tween.Alpha(
                _dodgeSprite,
                endValue: 0f,
                duration: fadeDuration,
                ease: Ease.Linear)
                .OnComplete(ResetDodgeSprite);
        }

        private void ResetDodgeSprite()
        {
            if (_dodgeSprite == null) return;

            _dodgeFadeTween.Stop();

            // Re-attach to original parent
            if (_dodgeSpriteOriginalParent != null)
                _dodgeSprite.transform.SetParent(_dodgeSpriteOriginalParent, worldPositionStays: false);

            // Reset alpha and deactivate
            var c = _dodgeSprite.color;
            c.a = 0f;
            _dodgeSprite.color = c;
            _dodgeSprite.gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════
        // i-frame Flicker
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid RunIFrameFlickerAsync(CancellationToken ct)
        {
            if (_juiceSettings == null) return;

            int intervalMs = Mathf.Max(1, Mathf.RoundToInt(_juiceSettings.IFrameFlashInterval * 1000f));
            bool bright = false;

            while (!ct.IsCancellationRequested)
            {
                if (_solidRenderer == null) break;

                Color c = _solidBaseColor;
                c.a = bright ? _solidBaseColor.a : _juiceSettings.IFrameFlashAlpha;
                _solidRenderer.color = c;
                bright = !bright;

                await UniTask.Delay(intervalMs, cancellationToken: ct)
                    .SuppressCancellationThrow();

                if (ct.IsCancellationRequested) break;
            }
        }

        private void CancelIFrameFlicker()
        {
            if (_iFrameCts == null) return;
            _iFrameCts.Cancel();
            _iFrameCts.Dispose();
            _iFrameCts = null;
        }
    }
}
