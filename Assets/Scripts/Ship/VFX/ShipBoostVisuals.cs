using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles all Boost-state visual feedback on the ship's sprite layers:
    ///   - Liquid layer color tween (baseline → boost glow color → baseline)
    ///   - HL layer alpha ramp (energy glow intensifies during Boost)
    ///   - Core layer alpha ramp (cockpit energy surge)
    ///   - Thruster pulse (Ship_Sprite_Back scale animation)
    ///   - BoostTrailView startup/shutdown delegation
    ///
    /// Driven by ShipView via OnBoostStarted() / OnBoostEnded().
    /// Does NOT subscribe to events directly — ShipView routes state changes.
    /// </summary>
    public class ShipBoostVisuals : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References (wired by ShipPrefabRebuilder)
        // ══════════════════════════════════════════════════════════════

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _liquidRenderer;
        [SerializeField] private SpriteRenderer _hlRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;

        [Header("Thruster Pulse")]
        [SerializeField] private Transform _backSpriteTransform;

        [Header("Boost Trail")]
        [SerializeField] private BoostTrailView _boostTrailView;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        [Header("Enable Toggles")]
        [Tooltip("Master switch — when OFF, all Boost visuals are silently skipped.")]
        [SerializeField] private bool _enableAll = true;

        [Tooltip("Liquid layer color tween during Boost (baseline → glow → baseline).")]
        [SerializeField] private bool _enableLiquidGlow = true;

        [Tooltip("HL layer alpha ramp during Boost.")]
        [SerializeField] private bool _enableHLRamp = true;

        [Tooltip("Core layer alpha ramp during Boost.")]
        [SerializeField] private bool _enableCoreRamp = true;

        [Tooltip("Ship_Sprite_Back thruster pulse animation.")]
        [SerializeField] private bool _enableThrusterPulse = true;

        [Tooltip("BoostTrailView startup/shutdown (trails, particles, bloom).")]
        [SerializeField] private bool _enableBoostTrail = true;

        // ══════════════════════════════════════════════════════════════
        // Runtime State
        // ══════════════════════════════════════════════════════════════

        // Baseline colors — set by ShipView after Awake initialization
        private Color _liquidBaseColor;
        private Color _hlBaseColor;
        private Color _coreBaseColor;

        // Baseline sprite & sort order — captured on Initialize
        private Sprite _liquidBaseSprite;
        private int    _liquidBaseSortOrder;

        // Active tweens
        private Tween    _liquidTween;
        private Tween    _thrusterEntryTween;
        private Sequence _thrusterLoopSequence;
        private Tween    _hlAlphaTween;
        private Tween    _coreAlphaTween;

        // ══════════════════════════════════════════════════════════════
        // Initialization (called by ShipView after baseline colors are set)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called by ShipView after Awake to pass baseline colors.
        /// </summary>
        public void Initialize(Color liquidBase, Color hlBase, Color coreBase)
        {
            _liquidBaseColor = liquidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;

            // Capture baseline sprite & sort order for restore on Boost end
            if (_liquidRenderer != null)
            {
                _liquidBaseSprite = _liquidRenderer.sprite;
                _liquidBaseSortOrder = _liquidRenderer.sortingOrder;
            }
        }

        /// <summary>
        /// Sync baseline color when another system modifies it (e.g. hit flash restore).
        /// </summary>
        public void SyncBaselineColors(Color liquidBase, Color hlBase, Color coreBase)
        {
            _liquidBaseColor = liquidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — called by ShipView
        // ══════════════════════════════════════════════════════════════

        public void OnBoostStarted()
        {
            if (!_enableAll || _juiceSettings == null) return;

            // 1. Liquid layer → boost glow (sprite swap + sort order + color tween)
            if (_enableLiquidGlow && _liquidRenderer != null)
            {
                _liquidTween.Stop();

                // Sprite swap: use Boost sprite if configured
                Sprite boostSprite = _juiceSettings.BoostLiquidSprite;
                if (boostSprite != null)
                    _liquidRenderer.sprite = boostSprite;

                // SortOrder override: bring Liquid above Solid during Boost
                if (_juiceSettings.BoostLiquidSortOverride)
                    _liquidRenderer.sortingOrder = _juiceSettings.BoostLiquidSortOrder;

                Color targetColor = _juiceSettings.BoostLiquidColor;
                float duration = _juiceSettings.BoostLiquidRampUpDuration;

                if (duration <= Mathf.Epsilon)
                {
                    _liquidRenderer.color = targetColor;
                }
                else
                {
                    _liquidTween = Tween.Color(
                        _liquidRenderer,
                        endValue: targetColor,
                        duration: duration,
                        ease: Ease.OutCubic);
                }
            }

            // 2. BoostTrailView startup stack
            if (_enableBoostTrail && _boostTrailView != null)
                _boostTrailView.OnBoostStart();

            // 3. Thruster pulse
            if (_enableThrusterPulse)
                StartThrusterPulse();

            // 4. HL layer ramp up
            if (_enableHLRamp)
                RampHLAlpha(_juiceSettings.HLBoostAlpha, _juiceSettings.HLBoostRampUpDuration);

            // 5. Core layer ramp up
            if (_enableCoreRamp)
                RampCoreAlpha(_juiceSettings.CoreBoostAlpha, _juiceSettings.CoreBoostRampUpDuration);
        }

        public void OnBoostEnded()
        {
            if (!_enableAll || _juiceSettings == null) return;

            // 1. Liquid layer → baseline (restore sprite + sort order + color tween)
            if (_enableLiquidGlow && _liquidRenderer != null)
            {
                _liquidTween.Stop();

                // Restore baseline sprite
                if (_juiceSettings.BoostLiquidSprite != null)
                    _liquidRenderer.sprite = _liquidBaseSprite;

                // Restore baseline sort order
                if (_juiceSettings.BoostLiquidSortOverride)
                    _liquidRenderer.sortingOrder = _liquidBaseSortOrder;

                float duration = _juiceSettings.BoostLiquidRampDownDuration;

                if (duration <= Mathf.Epsilon)
                {
                    _liquidRenderer.color = _liquidBaseColor;
                }
                else
                {
                    _liquidTween = Tween.Color(
                        _liquidRenderer,
                        endValue: _liquidBaseColor,
                        duration: duration,
                        ease: Ease.InQuad);
                }
            }

            // 2. BoostTrailView
            if (_enableBoostTrail && _boostTrailView != null)
                _boostTrailView.OnBoostEnd();

            // 3. Stop thruster pulse
            if (_enableThrusterPulse)
                StopThrusterPulse();

            // 4. HL layer ramp down
            if (_enableHLRamp)
                RampHLAlpha(_juiceSettings.HLBaseAlpha, _juiceSettings.HLBoostRampDownDuration);

            // 5. Core layer ramp down
            if (_enableCoreRamp)
                RampCoreAlpha(_juiceSettings.CoreBaseAlpha, _juiceSettings.CoreBoostRampDownDuration);
        }

        public void ResetState()
        {
            _liquidTween.Stop();
            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();
            _hlAlphaTween.Stop();
            _coreAlphaTween.Stop();

            if (_liquidRenderer != null)
            {
                _liquidRenderer.color = _liquidBaseColor;

                // Restore baseline sprite & sort order
                if (_liquidBaseSprite != null)
                    _liquidRenderer.sprite = _liquidBaseSprite;
                _liquidRenderer.sortingOrder = _liquidBaseSortOrder;
            }

            if (_hlRenderer != null) _hlRenderer.color = _hlBaseColor;
            if (_coreRenderer != null) _coreRenderer.color = _coreBaseColor;

            if (_boostTrailView != null)
                _boostTrailView.ResetState();

            if (_backSpriteTransform != null)
                _backSpriteTransform.localScale = Vector3.one;
        }

        // ══════════════════════════════════════════════════════════════
        // Thruster Pulse
        // ══════════════════════════════════════════════════════════════

        private void StartThrusterPulse()
        {
            if (_backSpriteTransform == null || _juiceSettings == null) return;

            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();
            _backSpriteTransform.localScale = Vector3.one;

            float peak = _juiceSettings.BoostBackScalePeak;
            float duration = _juiceSettings.BoostBackEntryPulseDuration;

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

            float pulseScale = _juiceSettings.BoostBackPulseScale;
            float halfPeriod = _juiceSettings.BoostBackPulsePeriod * 0.5f;

            _thrusterLoopSequence = Sequence.Create(cycles: -1)
                .Chain(Tween.Scale(_backSpriteTransform, endValue: Vector3.one * pulseScale, duration: halfPeriod, ease: Ease.InOutSine))
                .Chain(Tween.Scale(_backSpriteTransform, endValue: Vector3.one, duration: halfPeriod, ease: Ease.InOutSine));
        }

        private void StopThrusterPulse()
        {
            if (_backSpriteTransform == null || _juiceSettings == null) return;

            _thrusterEntryTween.Stop();
            _thrusterLoopSequence.Stop();

            Tween.Scale(
                _backSpriteTransform,
                endValue: Vector3.one,
                duration: _juiceSettings.BoostBackRestoreDuration,
                ease: Ease.OutQuad);
        }

        // ══════════════════════════════════════════════════════════════
        // HL Layer — alpha ramp
        // ══════════════════════════════════════════════════════════════

        private void RampHLAlpha(float targetAlpha, float duration)
        {
            if (_hlRenderer == null) return;

            _hlAlphaTween.Stop();

            if (duration <= Mathf.Epsilon)
            {
                var c = _hlRenderer.color;
                c.a = targetAlpha;
                _hlRenderer.color = c;
                _hlBaseColor = c;
                return;
            }

            float startAlpha = _hlRenderer.color.a;
            _hlAlphaTween = Tween.Custom(
                startValue: startAlpha,
                endValue: targetAlpha,
                duration: duration,
                onValueChange: a =>
                {
                    if (_hlRenderer == null) return;
                    var c = _hlRenderer.color;
                    c.a = a;
                    _hlRenderer.color = c;
                    _hlBaseColor = c;
                },
                ease: Ease.OutQuad);
        }

        // ══════════════════════════════════════════════════════════════
        // Core Layer — alpha ramp
        // ══════════════════════════════════════════════════════════════

        private void RampCoreAlpha(float targetAlpha, float duration)
        {
            if (_coreRenderer == null) return;

            _coreAlphaTween.Stop();

            if (duration <= Mathf.Epsilon)
            {
                var c = _coreRenderer.color;
                c.a = targetAlpha;
                _coreRenderer.color = c;
                _coreBaseColor = c;
                return;
            }

            float startAlpha = _coreRenderer.color.a;
            _coreAlphaTween = Tween.Custom(
                startValue: startAlpha,
                endValue: targetAlpha,
                duration: duration,
                onValueChange: a =>
                {
                    if (_coreRenderer == null) return;
                    var c = _coreRenderer.color;
                    c.a = a;
                    _coreRenderer.color = c;
                    _coreBaseColor = c;
                },
                ease: Ease.OutQuad);
        }
    }
}
