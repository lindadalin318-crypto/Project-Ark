using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Full Boost Trail VFX controller.
    /// Manages the active Boost presentation stack:
    ///   - Layer 1: TrailRenderer main trail (orange-yellow HDR)
    ///   - Layer 2: FlameTrail_R / FlameTrail_B particle systems (purple HDR)
    ///   - Layer 3: FlameCore burst particle system
    ///   - Layer 4: EmberTrail particle system (magenta HDR)
    ///   - Layer 5: EmberSparks one-shot burst (white HDR)
    ///   - Layer 6: BoostEnergyLayer2 / Layer3 SpriteRenderer (Shader _BoostIntensity)
    ///   - Layer 7: Boost activation halo (local sprite burst around ship)
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

        [Header("Energy Layers (Shader)")]
        [Tooltip("SpriteRenderer using BoostEnergyLayer2 shader.")]
        [SerializeField] private SpriteRenderer _energyLayer2;

        [Tooltip("SpriteRenderer using BoostEnergyLayer3 shader.")]
        [SerializeField] private SpriteRenderer _energyLayer3;

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
        [SerializeField] private float _intensityRampUpDuration = 0.22f;

        [Tooltip("Duration for _BoostIntensity 1→0 on Boost end.")]
        [SerializeField] private float _intensityRampDownDuration = 0.42f;

        [Header("Boost Startup Sequencing")]
        [Tooltip("Delay before sustained flame trails start, giving the startup burst a short head start.")]
        [SerializeField] private float _sustainFlameStartDelay = 0.045f;

        [Tooltip("Delay before EmberTrail joins, so startup layers read before sustained embers take over.")]
        [SerializeField] private float _emberTrailStartDelay = 0.07f;

        [Tooltip("Delay before EmberSparks fires, so FlameCore remains the first readable ignition cue.")]
        [SerializeField] private float _emberSparksBurstDelay = 0.018f;

        [Header("Boost Sustain Layer Blending")]
        [Tooltip("Master BoostIntensity threshold after which FlameTrail begins taking over the sustained thruster read.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flameTrailBlendInThreshold = 0.18f;

        [Tooltip("Maximum share of the master BoostIntensity granted to FlameTrail once sustain is fully established.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flameTrailMaxIntensity = 0.78f;

        [Tooltip("Master BoostIntensity threshold after which EmberTrail is allowed to join as a lighter residual layer.")]
        [Range(0f, 1f)]
        [SerializeField] private float _emberTrailBlendInThreshold = 0.42f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EmberTrail once sustain is fully established.")]
        [Range(0f, 1f)]
        [SerializeField] private float _emberTrailMaxIntensity = 0.32f;

        [Tooltip("Master BoostIntensity threshold after which EnergyLayer2 begins reading as sustained hull charge.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer2BlendInThreshold = 0.16f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EnergyLayer2.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer2MaxIntensity = 0.62f;

        [Tooltip("Master BoostIntensity threshold after which EnergyLayer3 is allowed to appear as the faintest inner charge layer.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer3BlendInThreshold = 0.38f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EnergyLayer3.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer3MaxIntensity = 0.34f;

        [Header("Activation Halo Settings")]
        [Tooltip("Peak alpha of the local Boost halo burst.")]
        [SerializeField] private float _activationHaloPeakAlpha = 1.4f;

        [Tooltip("Total duration of the local Boost halo burst.")]
        [SerializeField] private float _activationHaloDuration = 0.12f;

        [Tooltip("Scale multiplier at the beginning of the local halo burst.")]
        [SerializeField] private float _activationHaloStartScale = 0.56f;

        [Tooltip("Scale multiplier at the peak of the local halo burst.")]
        [SerializeField] private float _activationHaloPeakScale = 0.98f;

        [Tooltip("Scale multiplier after the halo flash starts collapsing.")]
        [SerializeField] private float _activationHaloEndScale = 0.82f;

        [Header("Bloom Burst Settings")]
        [Tooltip("Peak Bloom Intensity during Boost activation.")]
        [SerializeField] private float _bloomBurstIntensity = 2.15f;

        [Tooltip("Peak local volume weight during the Boost activation burst.")]
        [SerializeField] private float _bloomPeakWeight = 0.72f;

        [Tooltip("Fast attack duration of the Bloom burst.")]
        [SerializeField] private float _bloomAttackDuration = 0.05f;

        [Tooltip("Softer release duration of the Bloom burst.")]
        [SerializeField] private float _bloomReleaseDuration = 0.16f;

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        // Material Property Blocks for shader _BoostIntensity (avoids material instancing)
        private MaterialPropertyBlock _mpbMainTrail;
        private MaterialPropertyBlock _mpbLayer2;
        private MaterialPropertyBlock _mpbLayer3;

        private static readonly int BoostIntensityID = Shader.PropertyToID("_BoostIntensity");

        // Current _BoostIntensity value (driven by PrimeTween)
        private float _currentIntensity;

        // Active tweens
        private Tween _intensityTween;
        private Tween _bloomTween;
        private Tween _activationHaloTween;

        private CancellationTokenSource _boostStartupSequenceCts;

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
            _mpbMainTrail = new MaterialPropertyBlock();
            _mpbLayer2 = new MaterialPropertyBlock();
            _mpbLayer3 = new MaterialPropertyBlock();

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
            CancelBoostStartupSequence();
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
            CancelBoostStartupSequence();
            TraceStartupSequence($"OnBoostStart() begin. intensity={_currentIntensity:F3}");

            // 1. Activate TrailRenderer immediately to establish the sustained speed read.
            if (_mainTrail != null)
            {
                _mainTrail.time = _trailTime;
                _mainTrail.widthMultiplier = _trailWidthMultiplier;
                _mainTrail.Clear();
                _mainTrail.emitting = true;
            }

            // 2. Trigger the immediate startup confirmation layers first.
            TriggerParticleBurst(_flameCore);
            TriggerActivationHalo();
            TriggerBloomBurst();

            // 3. Animate _BoostIntensity 0 → 1 for trail + energy layers.
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 1f,
                duration: _intensityRampUpDuration,
                onValueChange: SetBoostIntensity,
                ease: Ease.InQuad);

            // 4. Let sparks and sustained layers join in readable phases instead of all firing at once.
            _boostStartupSequenceCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            TraceStartupSequence($"Startup sequence armed. flameDelay={_sustainFlameStartDelay:F3}, emberDelay={_emberTrailStartDelay:F3}, sparksDelay={_emberSparksBurstDelay:F3}");
            RunBoostStartupSequenceAsync(_boostStartupSequenceCts.Token).Forget();
        }

        /// <summary>
        /// Call when the ship exits Boost state.
        /// Stops trail/particles and fades out energy layers.
        /// </summary>
        public void OnBoostEnd()
        {
            CancelBoostStartupSequence();

            // 1. Stop TrailRenderer first; it should own the residual world-space tail read.
            if (_mainTrail != null)
                _mainTrail.emitting = false;

            // 2. Startup-only burst layers should not linger into the shutdown.
            StopParticle(_flameCore);
            StopParticle(_emberSparks);

            // 3. Let sustained flame / ember / energy layers fade down through the shared master intensity.
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 0f,
                duration: _intensityRampDownDuration,
                onValueChange: SetBoostIntensity,
                ease: Ease.OutQuad)
                .OnComplete(StopSustainParticles);
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
            CancelBoostStartupSequence();

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

            // Reset _BoostIntensity to 0 immediately
            _currentIntensity = 0f;
            SetBoostIntensity(0f);

            ResetActivationHalo();

            // Reset bloom volume
            if (_boostBloomVolume != null)
                _boostBloomVolume.weight = 0f;

            if (_bloomOverride != null)
                _bloomOverride.intensity.value = _baselineBloomIntensity;
        }

        /// <summary>
        /// Forces the sustained Boost stack into a deterministic preview state for Inspector debugging.
        /// </summary>
        public void DebugForceSustainPreview(
            float intensity,
            bool showMainTrail,
            bool showFlameTrail,
            bool showEmberTrail,
            bool showEnergyLayer2,
            bool showEnergyLayer3)
        {
            CancelBoostStartupSequence();
            _intensityTween.Stop();

            float clampedIntensity = Mathf.Clamp01(intensity);

            StopAndClearParticle(_flameCore);
            StopAndClearParticle(_emberSparks);
            ResetActivationHalo();
            ResetBloomState();

            if (_mainTrail != null)
            {
                _mainTrail.enabled = showMainTrail;
                _mainTrail.time = _trailTime;
                _mainTrail.widthMultiplier = _trailWidthMultiplier;

                if (showMainTrail && clampedIntensity > 0.001f)
                {
                    if (!_mainTrail.emitting)
                        _mainTrail.Clear();

                    _mainTrail.emitting = true;
                }
                else
                {
                    _mainTrail.emitting = false;
                    _mainTrail.Clear();
                }
            }

            SetBoostIntensity(clampedIntensity);

            bool showActiveSustain = clampedIntensity > 0.001f;
            SetSustainParticleVisible(_flameTrailR, showFlameTrail && showActiveSustain);
            SetSustainParticleVisible(_flameTrailB, showFlameTrail && showActiveSustain);
            SetSustainParticleVisible(_emberTrail, showEmberTrail && showActiveSustain);

            if (_energyLayer2 != null)
                _energyLayer2.enabled = showEnergyLayer2;

            if (_energyLayer3 != null)
                _energyLayer3.enabled = showEnergyLayer3;
        }

        /// <summary>
        /// Applies a visibility mask on top of the live runtime chain so layers can be isolated in Play Mode.
        /// </summary>
        public void DebugApplyVisibilityMask(
            bool showMainTrail,
            bool showFlameTrail,
            bool showFlameCore,
            bool showEmberTrail,
            bool showEmberSparks,
            bool showEnergyLayer2,
            bool showEnergyLayer3,
            bool showActivationHalo,
            bool showBloom)
        {
            if (_mainTrail != null)
            {
                _mainTrail.enabled = showMainTrail;
                if (!showMainTrail)
                {
                    _mainTrail.emitting = false;
                    _mainTrail.Clear();
                }
            }

            if (!showFlameTrail)
            {
                StopAndClearParticle(_flameTrailR);
                StopAndClearParticle(_flameTrailB);
            }

            if (!showFlameCore)
                StopAndClearParticle(_flameCore);

            if (!showEmberTrail)
                StopAndClearParticle(_emberTrail);

            if (!showEmberSparks)
                StopAndClearParticle(_emberSparks);

            if (_energyLayer2 != null)
                _energyLayer2.enabled = showEnergyLayer2;

            if (_energyLayer3 != null)
                _energyLayer3.enabled = showEnergyLayer3;

            if (!showActivationHalo)
                ResetActivationHalo();

            if (!showBloom)
                ResetBloomState();
        }

        /// <summary>
        /// Forces a single FlameCore burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewFlameCoreBurst()
        {
            StopAndClearParticle(_flameCore);
            TriggerParticleBurst(_flameCore);
        }

        /// <summary>
        /// Forces a single EmberSparks burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewEmberSparksBurst()
        {
            StopAndClearParticle(_emberSparks);
            TriggerParticleBurst(_emberSparks);
        }

        /// <summary>
        /// Forces the activation halo burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewActivationHalo()
        {
            ResetActivationHalo();
            TriggerActivationHalo();
        }

        /// <summary>
        /// Forces the Bloom burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewBloomBurst()
        {
            ResetBloomState();
            TriggerBloomBurst();
        }

        // ══════════════════════════════════════════════════════════════
        // Private Helpers
        // ══════════════════════════════════════════════════════════════

        private void EnsurePropertyBlocks()
        {
            if (_mpbMainTrail == null)
                _mpbMainTrail = new MaterialPropertyBlock();

            if (_mpbLayer2 == null)
                _mpbLayer2 = new MaterialPropertyBlock();

            if (_mpbLayer3 == null)
                _mpbLayer3 = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Sets _BoostIntensity on all energy layer shaders via MaterialPropertyBlock.
        /// </summary>
        private void SetBoostIntensity(float value)
        {
            EnsurePropertyBlocks();
            _currentIntensity = Mathf.Clamp01(value);

            if (_mainTrail != null)
            {
                _mainTrail.GetPropertyBlock(_mpbMainTrail);
                _mpbMainTrail.SetFloat(BoostIntensityID, _currentIntensity);
                _mainTrail.SetPropertyBlock(_mpbMainTrail);
            }

            ApplyFlameTrailState(_currentIntensity);
            ApplyEmberTrailState(_currentIntensity);

            float layer2Intensity = EvaluateSustainLayerIntensity(_currentIntensity, _energyLayer2BlendInThreshold, _energyLayer2MaxIntensity);
            if (_energyLayer2 != null)
            {
                _mpbLayer2.Clear();
                _mpbLayer2.SetFloat(BoostIntensityID, layer2Intensity);
                _energyLayer2.SetPropertyBlock(_mpbLayer2);
            }

            float layer3Intensity = EvaluateSustainLayerIntensity(_currentIntensity, _energyLayer3BlendInThreshold, _energyLayer3MaxIntensity);
            if (_energyLayer3 != null)
            {
                _mpbLayer3.Clear();
                _mpbLayer3.SetFloat(BoostIntensityID, layer3Intensity);
                _energyLayer3.SetPropertyBlock(_mpbLayer3);
            }
        }

        private void ApplyFlameTrailState(float masterIntensity)
        {
            float flameIntensity = EvaluateSustainLayerIntensity(masterIntensity, _flameTrailBlendInThreshold, _flameTrailMaxIntensity);
            ApplyParticleSustainState(_flameTrailR, flameIntensity, useRateOverTime: true);
            ApplyParticleSustainState(_flameTrailB, flameIntensity, useRateOverTime: true);
        }

        private void ApplyEmberTrailState(float masterIntensity)
        {
            float emberIntensity = EvaluateSustainLayerIntensity(masterIntensity, _emberTrailBlendInThreshold, _emberTrailMaxIntensity);
            ApplyParticleSustainState(_emberTrail, emberIntensity, useRateOverTime: false);
        }

        private static float EvaluateSustainLayerIntensity(float masterIntensity, float blendInThreshold, float maxIntensity)
        {
            float threshold = Mathf.Clamp01(blendInThreshold);
            float t = Mathf.InverseLerp(threshold, 1f, Mathf.Clamp01(masterIntensity));
            return Mathf.Clamp01(maxIntensity) * EaseOutCubic(t);
        }

        private static void ApplyParticleSustainState(ParticleSystem particleSystem, float intensity, bool useRateOverTime)
        {
            if (particleSystem == null)
                return;

            float clampedIntensity = Mathf.Clamp01(intensity);

            var emission = particleSystem.emission;
            if (useRateOverTime)
            {
                emission.rateOverTimeMultiplier = clampedIntensity;
                emission.rateOverDistanceMultiplier = 0f;
            }
            else
            {
                emission.rateOverTimeMultiplier = 0f;
                emission.rateOverDistanceMultiplier = clampedIntensity;
            }

            var main = particleSystem.main;
            main.startSizeMultiplier = Mathf.Lerp(0.25f, 1f, clampedIntensity);
            main.startSpeedMultiplier = Mathf.Lerp(0.45f, 1f, clampedIntensity);
        }

        private void StopSustainParticles()
        {
            StopParticle(_flameTrailR);
            StopParticle(_flameTrailB);
            StopParticle(_emberTrail);
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

            const float peakPoint = 0.09f;
            const float fadeStartPoint = 0.18f;

            float alpha;
            if (progress < peakPoint)
            {
                float t = EaseOutQuart(progress / peakPoint);
                alpha = Mathf.Lerp(0f, _activationHaloPeakAlpha, t);
            }
            else if (progress < fadeStartPoint)
            {
                float t = EaseOutQuad((progress - peakPoint) / (fadeStartPoint - peakPoint));
                alpha = Mathf.Lerp(_activationHaloPeakAlpha, _activationHaloPeakAlpha * 0.52f, t);
            }
            else
            {
                float t = EaseInCubic((progress - fadeStartPoint) / (1f - fadeStartPoint));
                alpha = Mathf.Lerp(_activationHaloPeakAlpha * 0.52f, 0f, t);
            }

            float scale = progress < peakPoint
                ? Mathf.Lerp(_activationHaloStartScale, _activationHaloPeakScale, EaseOutQuart(progress / peakPoint))
                : Mathf.Lerp(_activationHaloPeakScale, _activationHaloEndScale, EaseOutCubic((progress - peakPoint) / (1f - peakPoint)));

            _activationHalo.transform.localScale = _activationHaloBaseScale * scale;

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

        private void ResetBloomState()
        {
            _bloomTween.Stop();

            if (_boostBloomVolume != null)
                _boostBloomVolume.weight = 0f;

            if (_bloomOverride != null)
                _bloomOverride.intensity.value = _baselineBloomIntensity;
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

        private static float EaseOutQuart(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - (inv * inv * inv * inv);
        }

        private static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        /// <summary>
        /// Triggers a short local Bloom lift that supports the ship-local startup layers.
        /// </summary>
        private void TriggerBloomBurst()
        {
            if (_boostBloomVolume == null || _bloomOverride == null) return;

            _bloomTween.Stop();

            float peakIntensity = Mathf.Max(_baselineBloomIntensity, _bloomBurstIntensity);
            float peakWeight = Mathf.Clamp01(_bloomPeakWeight);

            _boostBloomVolume.weight = 0f;
            _bloomOverride.intensity.value = _baselineBloomIntensity;

            _bloomTween = Tween.Custom(
                startValue: 0f,
                endValue: 1f,
                duration: _bloomAttackDuration,
                onValueChange: t => ApplyBloomState(
                    Mathf.Lerp(_baselineBloomIntensity, peakIntensity, EaseOutCubic(t)),
                    Mathf.Lerp(0f, peakWeight, EaseOutQuad(t))),
                ease: Ease.Linear)
                .OnComplete(() =>
                {
                    if (_boostBloomVolume == null || _bloomOverride == null) return;

                    _bloomTween = Tween.Custom(
                        startValue: 0f,
                        endValue: 1f,
                        duration: _bloomReleaseDuration,
                        onValueChange: t => ApplyBloomState(
                            Mathf.Lerp(peakIntensity, _baselineBloomIntensity, EaseOutQuad(t)),
                            Mathf.Lerp(peakWeight, 0f, EaseOutCubic(t))),
                        ease: Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (_boostBloomVolume == null || _bloomOverride == null) return;
                            _bloomOverride.intensity.value = _baselineBloomIntensity;
                            _boostBloomVolume.weight = 0f;
                        });
                });
        }

        private void ApplyBloomState(float intensity, float weight)
        {
            if (_boostBloomVolume == null || _bloomOverride == null) return;

            _bloomOverride.intensity.value = intensity;
            _boostBloomVolume.weight = weight;
        }

        private void CancelBoostStartupSequence()
        {
            if (_boostStartupSequenceCts == null)
                return;

            _boostStartupSequenceCts.Cancel();
            _boostStartupSequenceCts.Dispose();
            _boostStartupSequenceCts = null;
        }

        private async UniTaskVoid RunBoostStartupSequenceAsync(CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            try
            {
                elapsed = await WaitForStartupMomentAsync(elapsed, _emberSparksBurstDelay, cancellationToken);
                TraceStartupSequence($"Reached EmberSparks moment at t={elapsed:F3}");
                TriggerParticleBurst(_emberSparks);

                elapsed = await WaitForSustainLayerReadyAsync(elapsed, _sustainFlameStartDelay, _flameTrailBlendInThreshold, cancellationToken);
                TraceStartupSequence($"Reached FlameTrail moment at t={elapsed:F3}, intensity={_currentIntensity:F3}");
                PlayParticle(_flameTrailR);
                PlayParticle(_flameTrailB);
                TraceStartupSequence($"Issued PlayParticle for FlameTrail_R/B. isPlayingR={(_flameTrailR != null && _flameTrailR.isPlaying)}, isPlayingB={(_flameTrailB != null && _flameTrailB.isPlaying)}");

                elapsed = await WaitForSustainLayerReadyAsync(elapsed, _emberTrailStartDelay, _emberTrailBlendInThreshold, cancellationToken);
                TraceStartupSequence($"Reached EmberTrail moment at t={elapsed:F3}, intensity={_currentIntensity:F3}");
                PlayParticle(_emberTrail);
            }
            catch (OperationCanceledException)
            {
                // Expected when Boost ends before delayed startup layers fully enter.
            }
        }

        private async UniTask<float> WaitForSustainLayerReadyAsync(
            float elapsed,
            float targetTime,
            float blendInThreshold,
            CancellationToken cancellationToken)
        {
            float nextElapsed = await WaitForStartupMomentAsync(elapsed, targetTime, cancellationToken);
            float activationThreshold = Mathf.Clamp01(blendInThreshold) + 0.001f;

            if (_currentIntensity < activationThreshold)
            {
                TraceStartupSequence($"Waiting for sustain threshold {activationThreshold:F3}. currentIntensity={_currentIntensity:F3}");
                await UniTask.WaitUntil(() => _currentIntensity >= activationThreshold, cancellationToken: cancellationToken);
            }

            return nextElapsed;
        }

        private static async UniTask<float> WaitForStartupMomentAsync(float elapsed, float targetTime, CancellationToken cancellationToken)
        {
            float clampedTargetTime = Mathf.Max(0f, targetTime);
            float delay = clampedTargetTime - elapsed;

            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

            return Mathf.Max(elapsed, clampedTargetTime);
        }

        private static void TriggerParticleBurst(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private static void PlayParticle(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private static void SetSustainParticleVisible(ParticleSystem ps, bool visible)
        {
            if (ps == null)
                return;

            if (visible)
            {
                if (!ps.isPlaying)
                    ps.Play();

                return;
            }

            StopAndClearParticle(ps);
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void TraceStartupSequence(string message)
        {
            if (!Application.isPlaying)
                return;

            Debug.Log(
                $"[BoostTrailViewTrace] {message} | object={name} | time={Time.time:F3} | timeScale={Time.timeScale:F3} | flameRPlaying={(_flameTrailR != null && _flameTrailR.isPlaying)} | flameBPlaying={(_flameTrailB != null && _flameTrailB.isPlaying)} | emberPlaying={(_emberTrail != null && _emberTrail.isPlaying)}",
                this);
        }
    }
}
