using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles all hit/damage visual feedback on the ship's sprite layers:
    ///   - Hit flash (all 5 layers flash to white simultaneously)
    ///   - Post-hit i-frame blink (all visible layers dim/bright cycle)
    ///   - Core low-HP warning pulse (infinite red pulse on Core layer)
    ///
    /// Driven by ShipView via OnDamageTaken().
    /// Does NOT subscribe to events directly — ShipView routes damage events.
    /// </summary>
    public class ShipHitVisuals : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References (wired by ShipPrefabRebuilder)
        // ══════════════════════════════════════════════════════════════

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _backRenderer;
        [SerializeField] private SpriteRenderer _liquidRenderer;
        [SerializeField] private SpriteRenderer _hlRenderer;
        [SerializeField] private SpriteRenderer _solidRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        [Header("Enable Toggles")]
        [Tooltip("Master switch — when OFF, all hit visuals are silently skipped.")]
        [SerializeField] private bool _enableAll = true;

        [Tooltip("Multi-layer white flash on damage.")]
        [SerializeField] private bool _enableHitFlash = true;

        [Tooltip("Post-hit i-frame blink (all layers dim/bright cycle).")]
        [SerializeField] private bool _enableIFrameBlink = true;

        [Tooltip("Core layer low-HP red warning pulse.")]
        [SerializeField] private bool _enableLowHPPulse = true;

        // ══════════════════════════════════════════════════════════════
        // Runtime State
        // ══════════════════════════════════════════════════════════════

        private Color _liquidBaseColor;
        private Color _solidBaseColor;
        private Color _hlBaseColor;
        private Color _coreBaseColor;

        // Low-HP pulse
        private Sequence _coreLowHPPulse;
        private bool _isLowHPPulsing;

        // Hit flash + hit i-frame cancellation
        private CancellationTokenSource _hitFlashCts;
        private CancellationTokenSource _hitIFrameCts;

        // ShipHealth reference for low-HP evaluation
        private ShipHealth _shipHealth;

        // ══════════════════════════════════════════════════════════════
        // Initialization
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called by ShipView after Awake to pass baseline colors and health reference.
        /// </summary>
        public void Initialize(
            Color liquidBase, Color solidBase, Color hlBase, Color coreBase,
            ShipHealth shipHealth)
        {
            _liquidBaseColor = liquidBase;
            _solidBaseColor = solidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;
            _shipHealth = shipHealth;
        }

        /// <summary>
        /// Sync baseline colors when another system modifies them.
        /// </summary>
        public void SyncBaselineColors(Color liquidBase, Color solidBase, Color hlBase, Color coreBase)
        {
            _liquidBaseColor = liquidBase;
            _solidBaseColor = solidBase;
            _hlBaseColor = hlBase;
            _coreBaseColor = coreBase;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — called by ShipView
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Triggers hit flash + i-frame blink + low-HP pulse evaluation.
        /// </summary>
        public void OnDamageTaken(float damage, float currentHP)
        {
            if (!_enableAll || _juiceSettings == null) return;

            // 1. Multi-layer hit flash
            if (_enableHitFlash)
            {
                CancelHitFlash();
                _hitFlashCts = new CancellationTokenSource();
                RunHitFlashAsync(_hitFlashCts.Token).Forget();
            }

            // 2. Multi-layer post-hit i-frame blink
            if (_enableIFrameBlink)
            {
                float iFrameDuration = _juiceSettings.HitIFrameDuration > 0f
                    ? _juiceSettings.HitIFrameDuration
                    : (_shipHealth != null && _shipHealth.IsInvulnerable ? 1.0f : 0f);

                if (iFrameDuration > 0f)
                {
                    CancelHitIFrame();
                    _hitIFrameCts = new CancellationTokenSource();
                    RunHitIFrameBlinkAsync(iFrameDuration, _hitIFrameCts.Token).Forget();
                }
            }

            // 3. Evaluate low-HP pulse state
            if (_enableLowHPPulse)
                EvaluateCoreLowHPPulse();
        }

        public void ResetState()
        {
            CancelHitFlash();
            CancelHitIFrame();

            _coreLowHPPulse.Stop();
            _isLowHPPulsing = false;

            if (_liquidRenderer != null) _liquidRenderer.color = _liquidBaseColor;
            if (_solidRenderer != null) _solidRenderer.color = _solidBaseColor;
            if (_hlRenderer != null) _hlRenderer.color = _hlBaseColor;
            if (_coreRenderer != null) _coreRenderer.color = _coreBaseColor;
        }

        // ══════════════════════════════════════════════════════════════
        // Hit Flash
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid RunHitFlashAsync(CancellationToken ct)
        {
            if (_juiceSettings == null) return;

            Color flashColor = _juiceSettings.HitFlashColor;
            int durationMs = Mathf.Max(1, Mathf.RoundToInt(_juiceSettings.HitFlashDuration * 1000f));

            // Capture current colors to restore after flash
            Color? solidOrig = _solidRenderer != null ? _solidRenderer.color : (Color?)null;
            Color? liquidOrig = _liquidRenderer != null ? _liquidRenderer.color : (Color?)null;
            Color? hlOrig = _hlRenderer != null ? _hlRenderer.color : (Color?)null;
            Color? coreOrig = _coreRenderer != null ? _coreRenderer.color : (Color?)null;
            Color? backOrig = _backRenderer != null ? _backRenderer.color : (Color?)null;

            // Flash all layers to white
            if (_solidRenderer != null) _solidRenderer.color = flashColor;
            if (_liquidRenderer != null) _liquidRenderer.color = flashColor;
            if (_hlRenderer != null) _hlRenderer.color = flashColor;
            if (_coreRenderer != null) _coreRenderer.color = flashColor;
            if (_backRenderer != null) _backRenderer.color = flashColor;

            await UniTask.Delay(durationMs, cancellationToken: ct).SuppressCancellationThrow();
            if (ct.IsCancellationRequested) return;

            // Restore original colors
            if (_solidRenderer != null && solidOrig.HasValue) _solidRenderer.color = solidOrig.Value;
            if (_liquidRenderer != null && liquidOrig.HasValue) _liquidRenderer.color = liquidOrig.Value;
            if (_hlRenderer != null && hlOrig.HasValue) _hlRenderer.color = hlOrig.Value;
            if (_coreRenderer != null && coreOrig.HasValue) _coreRenderer.color = coreOrig.Value;
            if (_backRenderer != null && backOrig.HasValue) _backRenderer.color = backOrig.Value;
        }

        // ══════════════════════════════════════════════════════════════
        // Post-Hit i-Frame Blink
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid RunHitIFrameBlinkAsync(float duration, CancellationToken ct)
        {
            if (_juiceSettings == null) return;

            float dimAlpha = _juiceSettings.HitIFrameDimAlpha;
            int intervalMs = Mathf.Max(1, Mathf.RoundToInt(_juiceSettings.HitIFrameBlinkInterval * 1000f));
            float elapsed = 0f;
            bool bright = true;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                bright = !bright;
                float alpha = bright ? 1f : dimAlpha;

                SetLayerAlpha(_solidRenderer, _solidBaseColor, alpha);
                SetLayerAlpha(_liquidRenderer, _liquidBaseColor, alpha);
                SetLayerAlpha(_hlRenderer, _hlBaseColor, alpha);
                SetLayerAlpha(_coreRenderer, _coreBaseColor, alpha);
                SetLayerAlpha(_backRenderer, Color.white, alpha);

                await UniTask.Delay(intervalMs, cancellationToken: ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) break;
                elapsed += _juiceSettings.HitIFrameBlinkInterval;
            }

            // Restore all layers to full visibility
            if (_solidRenderer != null) _solidRenderer.color = _solidBaseColor;
            if (_liquidRenderer != null) _liquidRenderer.color = _liquidBaseColor;
            if (_hlRenderer != null) _hlRenderer.color = _hlBaseColor;
            if (_coreRenderer != null) _coreRenderer.color = _coreBaseColor;
            if (_backRenderer != null)
            {
                var c = _backRenderer.color;
                c.a = 1f;
                _backRenderer.color = c;
            }
        }

        private static void SetLayerAlpha(SpriteRenderer renderer, Color baseColor, float alpha)
        {
            if (renderer == null) return;
            Color c = baseColor;
            c.a = baseColor.a * alpha;
            renderer.color = c;
        }

        // ══════════════════════════════════════════════════════════════
        // Core Low-HP Warning Pulse
        // ══════════════════════════════════════════════════════════════

        private void EvaluateCoreLowHPPulse()
        {
            if (_coreRenderer == null || _juiceSettings == null || _shipHealth == null) return;

            float hpRatio = _shipHealth.CurrentHP / _shipHealth.MaxHP;
            bool shouldPulse = hpRatio > 0f && hpRatio <= _juiceSettings.CoreLowHPThreshold;

            if (shouldPulse && !_isLowHPPulsing)
                StartCoreLowHPPulse();
            else if (!shouldPulse && _isLowHPPulsing)
                StopCoreLowHPPulse();
        }

        private void StartCoreLowHPPulse()
        {
            if (_coreRenderer == null || _juiceSettings == null) return;

            _isLowHPPulsing = true;
            _coreLowHPPulse.Stop();

            Color warningColor = _juiceSettings.CoreLowHPColor;
            float maxAlpha = warningColor.a;
            float minAlpha = _juiceSettings.CoreLowHPPulseMinAlpha;
            float halfPeriod = _juiceSettings.CoreLowHPPulsePeriod * 0.5f;

            _coreRenderer.color = warningColor;

            _coreLowHPPulse = Sequence.Create(cycles: -1)
                .Chain(Tween.Alpha(_coreRenderer, endValue: minAlpha, duration: halfPeriod, ease: Ease.InOutSine))
                .Chain(Tween.Alpha(_coreRenderer, endValue: maxAlpha, duration: halfPeriod, ease: Ease.InOutSine));
        }

        private void StopCoreLowHPPulse()
        {
            _isLowHPPulsing = false;
            _coreLowHPPulse.Stop();

            if (_coreRenderer != null)
            {
                float baseAlpha = _juiceSettings != null ? _juiceSettings.CoreBaseAlpha : 0.3f;
                var c = _coreBaseColor;
                c.a = baseAlpha;
                c.r = _coreBaseColor.r;
                c.g = _coreBaseColor.g;
                c.b = _coreBaseColor.b;
                _coreRenderer.color = c;
                _coreBaseColor = c;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Cancellation
        // ══════════════════════════════════════════════════════════════

        private void CancelHitFlash()
        {
            if (_hitFlashCts == null) return;
            _hitFlashCts.Cancel();
            _hitFlashCts.Dispose();
            _hitFlashCts = null;
        }

        private void CancelHitIFrame()
        {
            if (_hitIFrameCts == null) return;
            _hitIFrameCts.Cancel();
            _hitIFrameCts.Dispose();
            _hitIFrameCts = null;
        }
    }
}
