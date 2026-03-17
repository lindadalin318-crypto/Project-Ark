using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Manages the ship's multi-layer sprite visual state and all VFX feedback.
    ///
    /// Primary driver: `ShipStateController.OnStateChanged`.
    /// This class no longer keeps legacy Boost/Dash event fallbacks; missing state wiring must fail loudly.
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
        [Tooltip("Full Boost Trail VFX controller (BoostTrailRoot prefab).")]
        [SerializeField] private BoostTrailView _boostTrailView;

        [Header("VFX — Boost Liquid Sprite")]
        [Tooltip("Sprite to use for _liquidRenderer when in Boost state (Boost_16).")]
        [SerializeField] private Sprite _boostLiquidSprite;

        [Tooltip("Sprite to use for _liquidRenderer in Normal state (Movement_3).")]
        [SerializeField] private Sprite _normalLiquidSprite;

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

        private ShipDash _dash;
        private ShipStateController _stateController;

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
            _dash = GetComponent<ShipDash>();
            _stateController = GetComponent<ShipStateController>();

            if (_stateController == null)
            {
                Debug.LogError("[ShipView] Missing ShipStateController. Ship VFX now requires the unified state chain and no longer falls back to ShipBoost / ShipDash events.", this);
            }

            if (_normalLiquidSprite == null)
            {
                Debug.LogError("[ShipView] _normalLiquidSprite is not assigned. Fix the prefab wiring instead of relying on runtime fallback.", this);
            }

            if (_liquidRenderer != null)
            {
                _liquidBaseColor = _liquidRenderer.color;
            }

            if (_solidRenderer != null)
            {
                _solidBaseColor = _solidRenderer.color;
            }

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

                if (_dodgeSprite.sprite == null)
                {
                    Debug.LogError("[ShipView] Dodge_Sprite has no sprite assigned. Fix the prefab wiring instead of relying on runtime fallback.", _dodgeSprite);
                }
            }
        }

        private void OnEnable()
        {
            if (_stateController != null)
            {
                _stateController.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_stateController != null)
            {
                _stateController.OnStateChanged -= HandleStateChanged;
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

            // Reset BoostTrailView
            if (_boostTrailView != null)
                _boostTrailView.ResetState();

            // Reset liquid sprite to Normal state
            if (_liquidRenderer != null && _normalLiquidSprite != null)
                _liquidRenderer.sprite = _normalLiquidSprite;

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

            // 1. Liquid sprite enters Boost shape immediately so the ignition punch reads as a state change.
            if (_liquidRenderer != null && _boostLiquidSprite != null)
                _liquidRenderer.sprite = _boostLiquidSprite;

            // 2. Glow layer performs a fast ignition punch, then settles into the sustained Boost brightness.
            if (_liquidRenderer != null)
            {
                _liquidTween.Stop();

                float startupDuration = Mathf.Max(0f, _juiceSettings.BoostGlowRampUpDuration) +
                                        Mathf.Max(0f, _juiceSettings.BoostGlowSettleDuration);

                if (startupDuration <= Mathf.Epsilon)
                {
                    _liquidRenderer.color = GetLiquidColorWithMultiplier(_juiceSettings.BoostGlowBrightnessMultiplier);
                }
                else
                {
                    _liquidTween = Tween.Custom(
                        startValue: 0f,
                        endValue: 1f,
                        duration: startupDuration,
                        onValueChange: ApplyBoostLiquidStartup,
                        ease: Ease.Linear);
                }
            }

            // 3. BoostTrailView startup stack
            if (_boostTrailView != null)
                _boostTrailView.OnBoostStart();

            // 4. Thruster pulse
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

            // 2. Liquid sprite → Movement_3 (Normal)
            if (_liquidRenderer != null && _normalLiquidSprite != null)
                _liquidRenderer.sprite = _normalLiquidSprite;

            // 3. BoostTrailView
            if (_boostTrailView != null)
                _boostTrailView.OnBoostEnd();

            // 4. Stop thruster pulse
            StopThrusterPulse();
        }

        private void ApplyBoostLiquidStartup(float progress)
        {
            if (_liquidRenderer == null || _juiceSettings == null)
                return;

            float sustainMultiplier = Mathf.Max(1f, _juiceSettings.BoostGlowBrightnessMultiplier);
            float entryMultiplier = Mathf.Max(sustainMultiplier, _juiceSettings.BoostGlowEntryBrightnessMultiplier);
            float attackDuration = Mathf.Max(0f, _juiceSettings.BoostGlowRampUpDuration);
            float settleDuration = Mathf.Max(0f, _juiceSettings.BoostGlowSettleDuration);
            float totalDuration = attackDuration + settleDuration;

            if (totalDuration <= Mathf.Epsilon)
            {
                _liquidRenderer.color = GetLiquidColorWithMultiplier(sustainMultiplier);
                return;
            }

            float attackRatio = attackDuration / totalDuration;
            float multiplier;

            if (attackRatio <= Mathf.Epsilon)
            {
                multiplier = Mathf.Lerp(1f, sustainMultiplier, EaseOutQuad(progress));
            }
            else if (progress < attackRatio)
            {
                float t = Mathf.Clamp01(progress / attackRatio);
                multiplier = Mathf.Lerp(1f, entryMultiplier, EaseOutCubic(t));
            }
            else
            {
                float settleRatio = 1f - attackRatio;
                float t = settleRatio <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01((progress - attackRatio) / settleRatio);
                multiplier = Mathf.Lerp(entryMultiplier, sustainMultiplier, EaseOutQuad(t));
            }

            _liquidRenderer.color = GetLiquidColorWithMultiplier(multiplier);
        }

        private Color GetLiquidColorWithMultiplier(float multiplier)
        {
            Color color = _liquidBaseColor * Mathf.Max(1f, multiplier);
            color.a = _liquidBaseColor.a;
            return color;
        }

        private static float EaseOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - ((1f - t) * (1f - t));
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - (inv * inv * inv);
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

            if (_dodgeSprite.sprite == null)
            {
                Debug.LogError("[ShipView] Dodge_Sprite sprite is missing during dash preview. Fix the prefab wiring instead of relying on runtime fallback.", _dodgeSprite);
                return;
            }

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
