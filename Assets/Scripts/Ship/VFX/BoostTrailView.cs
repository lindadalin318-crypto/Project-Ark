using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Full Boost Trail VFX controller — complete GG replication.
    /// Manages all 8 layers of the Boost Trail effect:
    ///   - Layer 1: TrailRenderer main trail (orange-yellow HDR)
    ///   - Layer 2: FlameTrail_R / FlameTrail_B particle systems (purple HDR)
    ///   - Layer 3: FlameCore burst particle system
    ///   - Layer 4: EmberTrail particle system (magenta HDR)
    ///   - Layer 5: EmberSparks one-shot burst (white HDR)
    ///   - Layer 5b: EmberGlow burst particle system (orange-yellow HDR, Burst mode)
    ///   - Layer 6: BoostEnergyLayer2 / Layer3 SpriteRenderer (Shader _BoostIntensity)
    ///   - Layer 7: BoostEnergyField MeshRenderer (world-space shader)
    ///   - Layer 8: Boost activation halo (local sprite burst around ship)
    ///   - Post:    URP Volume Bloom burst
    ///
    /// Call OnBoostStart() / OnBoostEnd() / ResetState() from ShipView.
    /// All references via [SerializeField] — no FindObjectOfType.
    /// </summary>
    public class BoostTrailView : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References
        // ══════════════════════════════════════════════════════════════

        [Header("Trail")]
        [Tooltip("TrailRenderer on MainTrail child node.")]
        [SerializeField] private TrailRenderer _mainTrail;

        [Header("Flame Particles")]
        [Tooltip("Right-side flame trail particle system.")]
        [SerializeField] private ParticleSystem _flameTrailR;

        [Tooltip("Left-side flame trail particle system.")]
        [SerializeField] private ParticleSystem _flameTrailB;

        [Tooltip("Core flame burst particle system (very short lifetime).")]
        [SerializeField] private ParticleSystem _flameCore;

        [Header("Ember Particles")]
        [Tooltip("Ember trail particle system (rateOverDistance=2).")]
        [SerializeField] private ParticleSystem _emberTrail;

        [Tooltip("Ember sparks one-shot burst (Loop=false, StartSpeed=50).")]
        [SerializeField] private ParticleSystem _emberSparks;

        [Tooltip("Ember glow burst particle system (ps_ember_glow, StartLifetime=0.12s, Burst mode, orange-yellow HDR).")]
        [SerializeField] private ParticleSystem _emberGlow;

        [Header("Energy Layers (Shader)")]
        [Tooltip("SpriteRenderer using BoostEnergyLayer2 shader.")]
        [SerializeField] private SpriteRenderer _energyLayer2;

        [Tooltip("SpriteRenderer using BoostEnergyLayer3 shader.")]
        [SerializeField] private SpriteRenderer _energyLayer3;

        [Tooltip("MeshRenderer (Quad) using BoostEnergyField shader (world-space).")]
        [SerializeField] private MeshRenderer _energyField;

        [Tooltip("Temporary kill switch for the world-space energy field while the effect is still too dominant compared to the local activation halo.")]
        [SerializeField] private bool _enableWorldEnergyField;

        [Header("Activation Halo")]
        [Tooltip("Local additive sprite burst centered on the ship. This is the primary Boost activation flash, not the full-screen overlay.")]
        [SerializeField] private SpriteRenderer _activationHalo;

        [Header("Post-Processing")]
        [Tooltip("Local Volume for Boost Bloom burst (weight animated 0→1→0).")]
        [SerializeField] private Volume _boostBloomVolume;

        [Header("Trail Settings")]
        [Tooltip("Trail fade time in seconds (how long trail persists after stop).")]
        [SerializeField] private float _trailTime = 2.2f;

        [Tooltip("Trail width multiplier.")]
        [SerializeField] private float _trailWidthMultiplier = 2.75f;

        [Header("Boost Intensity Animation")]
        [Tooltip("Duration for _BoostIntensity 0→1 on Boost start.")]
        [SerializeField] private float _intensityRampUpDuration = 0.3f;

        [Tooltip("Duration for _BoostIntensity 1→0 on Boost end.")]
        [SerializeField] private float _intensityRampDownDuration = 0.5f;

        [Header("Activation Halo Settings")]
        [Tooltip("Peak alpha of the local Boost halo burst.")]
        [SerializeField] private float _activationHaloPeakAlpha = 1.15f;

        [Tooltip("Total duration of the local Boost halo burst.")]
        [SerializeField] private float _activationHaloDuration = 0.24f;

        [Tooltip("Scale multiplier at the beginning of the local halo burst.")]
        [SerializeField] private float _activationHaloStartScale = 0.72f;

        [Tooltip("Scale multiplier at the end of the local halo burst.")]
        [SerializeField] private float _activationHaloEndScale = 1.45f;

        [Header("Bloom Burst Settings")]
        [Tooltip("Peak Bloom Intensity during Boost activation.")]
        [SerializeField] private float _bloomBurstIntensity = 3.0f;

        [Tooltip("Duration of Bloom burst animation.")]
        [SerializeField] private float _bloomBurstDuration = 0.4f;

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        // Material Property Blocks for shader _BoostIntensity (avoids material instancing)
        private MaterialPropertyBlock _mpbLayer2;
        private MaterialPropertyBlock _mpbLayer3;
        private MaterialPropertyBlock _mpbField;

        private static readonly int BoostIntensityID = Shader.PropertyToID("_BoostIntensity");

        // Current _BoostIntensity value (driven by PrimeTween)
        private float _currentIntensity;

        // Active tweens
        private Tween _intensityTween;
        private Tween _bloomTween;
        private Tween _activationHaloTween;

        // Bloom component from local volume
        private Bloom _bloomOverride;

        // Baseline bloom intensity (restored after burst)
        private float _baselineBloomIntensity;
        private Vector3 _activationHaloBaseScale = Vector3.one;
        private Color _activationHaloBaseColor = Color.white;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Initialize Material Property Blocks
            _mpbLayer2 = new MaterialPropertyBlock();
            _mpbLayer3 = new MaterialPropertyBlock();
            _mpbField  = new MaterialPropertyBlock();

            // Cache Bloom component from local volume
            if (_boostBloomVolume != null)
            {
                _boostBloomVolume.profile.TryGet(out _bloomOverride);
                if (_bloomOverride != null)
                    _baselineBloomIntensity = _bloomOverride.intensity.value;
            }

            if (_activationHalo != null)
            {
                _activationHaloBaseScale = _activationHalo.transform.localScale;
                _activationHaloBaseColor = _activationHalo.color;
            }

            // Ensure all VFX start in reset state
            ResetState();
        }

        private void OnDestroy()
        {
            // Stop all active tweens to prevent callbacks from accessing destroyed components
            _intensityTween.Stop();
            _bloomTween.Stop();
            _activationHaloTween.Stop();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Call when the ship enters Boost state.
        /// Activates all trail/particle layers and triggers halo + bloom burst.
        /// </summary>
        public void OnBoostStart()
        {
            // 1. Activate TrailRenderer
            if (_mainTrail != null)
            {
                _mainTrail.time             = _trailTime;
                _mainTrail.widthMultiplier  = _trailWidthMultiplier;
                _mainTrail.Clear();
                _mainTrail.emitting = true;
            }

            // 2. Activate flame particle systems
            PlayParticle(_flameTrailR);
            PlayParticle(_flameTrailB);
            PlayParticle(_flameCore);

            // 3. Activate ember trail + ember glow (synchronized)
            PlayParticle(_emberTrail);
            PlayParticle(_emberGlow);

            // 4. Trigger EmberSparks one-shot burst
            if (_emberSparks != null)
            {
                _emberSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _emberSparks.Play();
            }

            // 5. Activate energy field MeshRenderer
            if (_enableWorldEnergyField && _energyField != null)
                _energyField.enabled = true;

            // 6. Animate _BoostIntensity 0 → 1 (EaseInQuad, 0.3s)
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 1f,
                duration: _intensityRampUpDuration,
                onValueChange: SetBoostIntensity,
                ease: Ease.InQuad);

            // 7. Local halo burst
            TriggerActivationHalo();

            // 8. Bloom burst: Intensity → 3.0 → baseline (EaseOutQuad, 0.4s)
            TriggerBloomBurst();
        }

        /// <summary>
        /// Call when the ship exits Boost state.
        /// Stops trail/particles and fades out energy layers.
        /// </summary>
        public void OnBoostEnd()
        {
            // 1. Stop TrailRenderer (trail fades naturally over _trailTime)
            if (_mainTrail != null)
                _mainTrail.emitting = false;

            // 2. Stop flame particles
            StopParticle(_flameTrailR);
            StopParticle(_flameTrailB);
            StopParticle(_flameCore);

            // 3. Stop ember trail + ember glow (synchronized)
            StopParticle(_emberTrail);
            StopParticle(_emberGlow);

            // 4. Animate _BoostIntensity 1 → 0 (EaseOutQuad, 0.5s)
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 0f,
                duration: _intensityRampDownDuration,
                onValueChange: SetBoostIntensity,
                ease: Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Disable energy field after fade completes
                    if (_energyField != null)
                        _energyField.enabled = false;
                });
        }

        /// <summary>
        /// Fully resets all VFX state. Call when returning ship to object pool.
        /// </summary>
        public void ResetState()
        {
            // Stop all tweens
            _intensityTween.Stop();
            _bloomTween.Stop();
            _activationHaloTween.Stop();

            // Reset TrailRenderer
            if (_mainTrail != null)
            {
                _mainTrail.emitting = false;
                _mainTrail.Clear();
            }

            // Stop + clear all particle systems
            StopAndClearParticle(_flameTrailR);
            StopAndClearParticle(_flameTrailB);
            StopAndClearParticle(_flameCore);
            StopAndClearParticle(_emberTrail);
            StopAndClearParticle(_emberSparks);
            StopAndClearParticle(_emberGlow);

            // Reset _BoostIntensity to 0 immediately
            _currentIntensity = 0f;
            SetBoostIntensity(0f);

            // Disable energy field
            if (_energyField != null)
                _energyField.enabled = false;

            ResetActivationHalo();

            // Reset bloom volume
            if (_boostBloomVolume != null)
                _boostBloomVolume.weight = 0f;

            if (_bloomOverride != null)
                _bloomOverride.intensity.value = _baselineBloomIntensity;
        }

        // ══════════════════════════════════════════════════════════════
        // Private Helpers
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets _BoostIntensity on all energy layer shaders via MaterialPropertyBlock.
        /// </summary>
        private void SetBoostIntensity(float value)
        {
            _currentIntensity = value;

            if (_energyLayer2 != null)
            {
                _energyLayer2.GetPropertyBlock(_mpbLayer2);
                _mpbLayer2.SetFloat(BoostIntensityID, value);
                _energyLayer2.SetPropertyBlock(_mpbLayer2);
            }

            if (_energyLayer3 != null)
            {
                _energyLayer3.GetPropertyBlock(_mpbLayer3);
                _mpbLayer3.SetFloat(BoostIntensityID, value);
                _energyLayer3.SetPropertyBlock(_mpbLayer3);
            }

            if (_energyField != null)
            {
                _energyField.GetPropertyBlock(_mpbField);
                _mpbField.SetFloat(BoostIntensityID, value);
                _energyField.SetPropertyBlock(_mpbField);
            }
        }

        /// <summary>
        /// Triggers the local halo burst around the ship, which should carry the main Boost activation read.
        /// </summary>
        private void TriggerActivationHalo()
        {
            if (_activationHalo == null) return;

            _activationHaloTween.Stop();
            _activationHalo.enabled = true;
            ApplyActivationHalo(0f);

            _activationHaloTween = Tween.Custom(
                startValue: 0f,
                endValue: 1f,
                duration: _activationHaloDuration,
                onValueChange: ApplyActivationHalo,
                ease: Ease.OutQuad)
                .OnComplete(ResetActivationHalo);
        }

        private void ApplyActivationHalo(float progress)
        {
            if (_activationHalo == null) return;

            float peakPoint = 0.2f;
            float alpha = progress < peakPoint
                ? Mathf.Lerp(0f, _activationHaloPeakAlpha, progress / peakPoint)
                : Mathf.Lerp(_activationHaloPeakAlpha, 0f, (progress - peakPoint) / (1f - peakPoint));

            _activationHalo.transform.localScale = _activationHaloBaseScale *
                Mathf.Lerp(_activationHaloStartScale, _activationHaloEndScale, progress);

            Color color = _activationHaloBaseColor;
            color.a = alpha;
            _activationHalo.color = color;
        }

        private void ResetActivationHalo()
        {
            if (_activationHalo == null) return;

            _activationHaloTween.Stop();
            _activationHalo.transform.localScale = _activationHaloBaseScale;

            Color color = _activationHaloBaseColor;
            color.a = 0f;
            _activationHalo.color = color;
            _activationHalo.enabled = false;
        }

        /// <summary>
        /// Triggers URP Volume Bloom burst: Intensity → burstIntensity → baseline.
        /// </summary>
        private void TriggerBloomBurst()
        {
            if (_boostBloomVolume == null || _bloomOverride == null) return;

            _bloomTween.Stop();

            // Activate local volume
            _boostBloomVolume.weight = 1f;

            float halfDuration = _bloomBurstDuration * 0.5f;

            // Phase 1: baseline → burstIntensity
            _bloomTween = Tween.Custom(
                startValue: _baselineBloomIntensity,
                endValue: _bloomBurstIntensity,
                duration: halfDuration,
                onValueChange: v => { if (_bloomOverride != null) _bloomOverride.intensity.value = v; },
                ease: Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_bloomOverride == null) return;
                    // Phase 2: burstIntensity → baseline
                    _bloomTween = Tween.Custom(
                        startValue: _bloomBurstIntensity,
                        endValue: _baselineBloomIntensity,
                        duration: halfDuration,
                        onValueChange: v => { if (_bloomOverride != null) _bloomOverride.intensity.value = v; },
                        ease: Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            // Deactivate local volume after burst
                            if (_boostBloomVolume != null)
                                _boostBloomVolume.weight = 0f;
                        });
                });
        }

        private static void PlayParticle(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private static void StopParticle(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private static void StopAndClearParticle(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
