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

        [Header("Post-Processing")]
        [Tooltip("Local Volume for Boost Bloom burst (weight animated 0→1→0).")]
        [SerializeField] private Volume _boostBloomVolume;

        [Header("Settings (Data-Driven)")]
        [Tooltip("ShipJuiceSettingsSO holds ALL Boost Trail parameters. No local hardcoded values.")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

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

        private CancellationTokenSource _boostStartupSequenceCts;

        // Bloom component from local volume
        private Bloom _bloomOverride;

        // Baseline bloom intensity (restored after burst)
        private float _baselineBloomIntensity;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Validate SO reference — all Boost Trail params live here
            if (_juiceSettings == null)
                Debug.LogError("[BoostTrailView] _juiceSettings is null! All Boost Trail parameters will use fallback defaults.", this);

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

                Debug.Log($"[BoostTrailView] Awake Bloom init: volume={_boostBloomVolume.name}, " +
                    $"hasProfile={_boostBloomVolume.profile != null}, " +
                    $"bloomOverride={(_bloomOverride != null ? "OK" : "NULL")}, " +
                    $"baseline={_baselineBloomIntensity:F2}", this);
            }
            else
            {
                Debug.LogError("[BoostTrailView] Awake: _boostBloomVolume is null! Bloom burst will not work.", this);
            }

            // Validate camera post-processing for Bloom to be visible
            ValidateCameraPostProcessing();

            // Ensure all VFX start in reset state
            ResetState();
        }

        private void OnDestroy()
        {
            // Stop all active tweens to prevent callbacks from accessing destroyed components
            _intensityTween.Stop();
            _bloomTween.Stop();
            CancelBoostStartupSequence();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Call when the ship enters Boost state.
        /// Activates all trail/particle layers and triggers bloom burst.
        /// </summary>
        public void OnBoostStart()
        {
            CancelBoostStartupSequence();
            TraceStartupSequence($"OnBoostStart() begin. intensity={_currentIntensity:F3}");

            float trailTime = _juiceSettings != null ? _juiceSettings.BoostTrailTime : 2.2f;
            float trailWidth = _juiceSettings != null ? _juiceSettings.BoostTrailWidthMultiplier : 2.75f;
            float rampUpDuration = _juiceSettings != null ? _juiceSettings.BoostIntensityRampUpDuration : 0.22f;
            float flameDelay = _juiceSettings != null ? _juiceSettings.BoostSustainFlameStartDelay : 0.045f;
            float emberDelay = _juiceSettings != null ? _juiceSettings.BoostEmberTrailStartDelay : 0.07f;
            float sparksDelay = _juiceSettings != null ? _juiceSettings.BoostEmberSparksBurstDelay : 0.018f;
            float flameThreshold = _juiceSettings != null ? _juiceSettings.FlameTrailBlendInThreshold : 0.18f;
            float emberThreshold = _juiceSettings != null ? _juiceSettings.EmberTrailBlendInThreshold : 0.42f;

            // 1. Activate TrailRenderer immediately to establish the sustained speed read.
            if (_mainTrail != null)
            {
                _mainTrail.time = trailTime;
                _mainTrail.widthMultiplier = trailWidth;
                _mainTrail.Clear();
                _mainTrail.emitting = true;
            }

            // 2. Trigger the immediate startup confirmation layers first.
            TriggerParticleBurst(_flameCore);
            TriggerBloomBurst();

            // 3. Animate _BoostIntensity 0 → 1 for trail + energy layers.
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 1f,
                duration: rampUpDuration,
                onValueChange: SetBoostIntensity,
                ease: Ease.InQuad);

            // 4. Let sparks and sustained layers join in readable phases instead of all firing at once.
            _boostStartupSequenceCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            TraceStartupSequence($"Startup sequence armed. flameDelay={flameDelay:F3}, emberDelay={emberDelay:F3}, sparksDelay={sparksDelay:F3}");
            RunBoostStartupSequenceAsync(
                sparksDelay, flameDelay, flameThreshold, emberDelay, emberThreshold,
                _boostStartupSequenceCts.Token).Forget();
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
            float rampDownDuration = _juiceSettings != null ? _juiceSettings.BoostIntensityRampDownDuration : 0.42f;
            _intensityTween.Stop();
            _intensityTween = Tween.Custom(
                startValue: _currentIntensity,
                endValue: 0f,
                duration: rampDownDuration,
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
            ResetBloomState();

            if (_mainTrail != null)
            {
                float trailTime = _juiceSettings != null ? _juiceSettings.BoostTrailTime : 2.2f;
                float trailWidth = _juiceSettings != null ? _juiceSettings.BoostTrailWidthMultiplier : 2.75f;

                _mainTrail.enabled = showMainTrail;
                _mainTrail.time = trailTime;
                _mainTrail.widthMultiplier = trailWidth;

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
        /// Forces the Bloom burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewBloomBurst()
        {
            ResetBloomState();
            TriggerBloomBurst();
        }

        /// <summary>
        /// Forces FlameTrail_R only into a sustained preview at full intensity.
        /// Resets all other layers (including FlameTrail_B) so FlameTrail_R is isolated.
        /// </summary>
        public void DebugPreviewFlameTrailR()
        {
            IsolateForFlameSustainPreview();
            StopAndClearParticle(_flameTrailB);
            PlayParticle(_flameTrailR);
        }

        /// <summary>
        /// Forces FlameTrail_B only into a sustained preview at full intensity.
        /// Resets all other layers (including FlameTrail_R) so FlameTrail_B is isolated.
        /// </summary>
        public void DebugPreviewFlameTrailB()
        {
            IsolateForFlameSustainPreview();
            StopAndClearParticle(_flameTrailR);
            PlayParticle(_flameTrailB);
        }

        /// <summary>
        /// Forces FlameTrail_R + FlameTrail_B into a sustained preview at full intensity.
        /// Resets other layers so the flame trails are isolated and immediately visible.
        /// </summary>
        public void DebugPreviewFlameTrailBoth()
        {
            IsolateForFlameSustainPreview();
            PlayParticle(_flameTrailR);
            PlayParticle(_flameTrailB);
        }

        /// <summary>
        /// Shared isolation setup for all FlameTrail preview variants.
        /// Stops everything except flame trails and drives intensity to 1.
        /// </summary>
        private void IsolateForFlameSustainPreview()
        {
            CancelBoostStartupSequence();
            _intensityTween.Stop();

            if (_mainTrail != null)
            {
                _mainTrail.emitting = false;
                _mainTrail.Clear();
            }
            StopAndClearParticle(_flameCore);
            StopAndClearParticle(_emberTrail);
            StopAndClearParticle(_emberSparks);
            ResetBloomState();

            SetBoostIntensity(1f);
        }

        /// <summary>
        /// Forces EmberTrail into a sustained preview at full intensity.
        /// Resets other layers so the ember trail is isolated and immediately visible.
        ///
        /// EmberTrail uses rateOverDistance (world-space), so it produces zero particles
        /// when the ship is stationary (the typical Inspector preview scenario).
        /// This method temporarily switches to rateOverTime for the preview so the
        /// effect is visible without requiring actual movement.
        /// </summary>
        public void DebugPreviewEmberTrailSustain()
        {
            CancelBoostStartupSequence();
            _intensityTween.Stop();

            // Stop all other layers to isolate ember trail
            if (_mainTrail != null)
            {
                _mainTrail.emitting = false;
                _mainTrail.Clear();
            }
            StopAndClearParticle(_flameCore);
            StopAndClearParticle(_flameTrailR);
            StopAndClearParticle(_flameTrailB);
            StopAndClearParticle(_emberSparks);
            ResetBloomState();

            // Drive intensity to 1 so the sustain logic fully activates ember trail
            _currentIntensity = 1f;

            // EmberTrail normally uses rateOverDistance which requires movement.
            // For stationary preview, temporarily switch to rateOverTime so particles
            // are visible immediately in the Inspector.
            if (_emberTrail != null)
            {
                var emission = _emberTrail.emission;
                float baseRate = 2.2f; // Match the authored rateOverDistance value
                emission.rateOverDistanceMultiplier = 0f;
                emission.rateOverTimeMultiplier = baseRate * 12f; // Approximate visual density
            }

            // Apply shader intensity for energy layers
            SetBoostIntensityShaderOnly(1f);

            // Ensure ember trail is playing
            PlayParticle(_emberTrail);
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
        /// Also drives sustain particles and auto-stops them when intensity falls below threshold.
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

            // Read all sustain params from SO (with fallbacks)
            float flameThreshold = _juiceSettings != null ? _juiceSettings.FlameTrailBlendInThreshold : 0.18f;
            float flameMax = _juiceSettings != null ? _juiceSettings.FlameTrailMaxIntensity : 0.78f;
            float emberThreshold = _juiceSettings != null ? _juiceSettings.EmberTrailBlendInThreshold : 0.42f;
            float emberMax = _juiceSettings != null ? _juiceSettings.EmberTrailMaxIntensity : 0.32f;
            float layer2Threshold = _juiceSettings != null ? _juiceSettings.EnergyLayer2BlendInThreshold : 0.16f;
            float layer2Max = _juiceSettings != null ? _juiceSettings.EnergyLayer2MaxIntensity : 0.62f;
            float layer3Threshold = _juiceSettings != null ? _juiceSettings.EnergyLayer3BlendInThreshold : 0.38f;
            float layer3Max = _juiceSettings != null ? _juiceSettings.EnergyLayer3MaxIntensity : 0.34f;
            float minSize = _juiceSettings != null ? _juiceSettings.SustainParticleMinSize : 0.25f;
            float minSpeed = _juiceSettings != null ? _juiceSettings.SustainParticleMinSpeed : 0.45f;
            float stopThreshold = _juiceSettings != null ? _juiceSettings.SustainParticleStopThreshold : 0.005f;

            // Sustain particles — FlameTrail
            float flameIntensity = EvaluateSustainLayerIntensity(_currentIntensity, flameThreshold, flameMax);
            ApplyParticleSustainState(_flameTrailR, flameIntensity, useRateOverTime: true, minSize, minSpeed, stopThreshold);
            ApplyParticleSustainState(_flameTrailB, flameIntensity, useRateOverTime: true, minSize, minSpeed, stopThreshold);

            // Sustain particles — EmberTrail
            float emberIntensity = EvaluateSustainLayerIntensity(_currentIntensity, emberThreshold, emberMax);
            ApplyParticleSustainState(_emberTrail, emberIntensity, useRateOverTime: false, minSize, minSpeed, stopThreshold);

            // Energy layers — shader _BoostIntensity
            float layer2Intensity = EvaluateSustainLayerIntensity(_currentIntensity, layer2Threshold, layer2Max);
            if (_energyLayer2 != null)
            {
                _mpbLayer2.Clear();
                _mpbLayer2.SetFloat(BoostIntensityID, layer2Intensity);
                _energyLayer2.SetPropertyBlock(_mpbLayer2);
            }

            float layer3Intensity = EvaluateSustainLayerIntensity(_currentIntensity, layer3Threshold, layer3Max);
            if (_energyLayer3 != null)
            {
                _mpbLayer3.Clear();
                _mpbLayer3.SetFloat(BoostIntensityID, layer3Intensity);
                _energyLayer3.SetPropertyBlock(_mpbLayer3);
            }
        }

        /// <summary>
        /// Drives only the shader _BoostIntensity on MainTrail and EnergyLayers
        /// without touching particle emission parameters. Used by debug preview
        /// methods that need to set emission modes independently.
        /// </summary>
        private void SetBoostIntensityShaderOnly(float value)
        {
            EnsurePropertyBlocks();
            float clampedValue = Mathf.Clamp01(value);

            if (_mainTrail != null)
            {
                _mainTrail.GetPropertyBlock(_mpbMainTrail);
                _mpbMainTrail.SetFloat(BoostIntensityID, clampedValue);
                _mainTrail.SetPropertyBlock(_mpbMainTrail);
            }

            float layer2Threshold = _juiceSettings != null ? _juiceSettings.EnergyLayer2BlendInThreshold : 0.16f;
            float layer2Max = _juiceSettings != null ? _juiceSettings.EnergyLayer2MaxIntensity : 0.62f;
            float layer3Threshold = _juiceSettings != null ? _juiceSettings.EnergyLayer3BlendInThreshold : 0.38f;
            float layer3Max = _juiceSettings != null ? _juiceSettings.EnergyLayer3MaxIntensity : 0.34f;

            float layer2Intensity = EvaluateSustainLayerIntensity(clampedValue, layer2Threshold, layer2Max);
            if (_energyLayer2 != null)
            {
                _mpbLayer2.Clear();
                _mpbLayer2.SetFloat(BoostIntensityID, layer2Intensity);
                _energyLayer2.SetPropertyBlock(_mpbLayer2);
            }

            float layer3Intensity = EvaluateSustainLayerIntensity(clampedValue, layer3Threshold, layer3Max);
            if (_energyLayer3 != null)
            {
                _mpbLayer3.Clear();
                _mpbLayer3.SetFloat(BoostIntensityID, layer3Intensity);
                _energyLayer3.SetPropertyBlock(_mpbLayer3);
            }
        }

        private static float EvaluateSustainLayerIntensity(float masterIntensity, float blendInThreshold, float maxIntensity)
        {
            float threshold = Mathf.Clamp01(blendInThreshold);
            float t = Mathf.InverseLerp(threshold, 1f, Mathf.Clamp01(masterIntensity));
            return Mathf.Clamp01(maxIntensity) * EaseOutCubic(t);
        }

        /// <summary>
        /// Applies emission rate, start size, and start speed to a sustain particle system.
        /// When intensity falls below stopThreshold, auto-stops the particle system
        /// to eliminate idle "playing but emitting nothing" overhead.
        /// </summary>
        private static void ApplyParticleSustainState(
            ParticleSystem particleSystem,
            float intensity,
            bool useRateOverTime,
            float minSize,
            float minSpeed,
            float stopThreshold)
        {
            if (particleSystem == null)
                return;

            float clampedIntensity = Mathf.Clamp01(intensity);

            // Auto-stop: when intensity is negligible, stop the particle system entirely
            // to avoid idle "playing but emitting nothing" overhead in the profiler.
            if (clampedIntensity <= stopThreshold)
            {
                if (particleSystem.isPlaying)
                    particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

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
            main.startSizeMultiplier = Mathf.Lerp(minSize, 1f, clampedIntensity);
            main.startSpeedMultiplier = Mathf.Lerp(minSpeed, 1f, clampedIntensity);
        }

        private void StopSustainParticles()
        {
            StopParticle(_flameTrailR);
            StopParticle(_flameTrailB);
            StopParticle(_emberTrail);
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

        /// <summary>
        /// Triggers a short local Bloom lift that supports the ship-local startup layers.
        /// </summary>
        private void TriggerBloomBurst()
        {
            if (_boostBloomVolume == null)
            {
                Debug.LogError("[BoostTrailView] TriggerBloomBurst: _boostBloomVolume is null. " +
                    "Run 'ProjectArk > Ship > VFX > Authority > Bind BoostTrail Scene Bloom References' to wire it.", this);
                return;
            }
            if (_bloomOverride == null)
            {
                Debug.LogError("[BoostTrailView] TriggerBloomBurst: _bloomOverride is null. " +
                    "Ensure BoostBloomVolumeProfile has a Bloom override added.", this);
                return;
            }

            _bloomTween.Stop();

            float bloomBurstIntensity = _juiceSettings != null ? _juiceSettings.BloomBurstIntensity : 3.2f;
            float bloomPeakWeight = _juiceSettings != null ? _juiceSettings.BloomPeakWeight : 0.88f;
            float bloomAttackDuration = _juiceSettings != null ? _juiceSettings.BloomAttackDuration : 0.05f;
            float bloomSustainDuration = _juiceSettings != null ? _juiceSettings.BloomSustainDuration : 0f;
            float bloomReleaseDuration = _juiceSettings != null ? _juiceSettings.BloomReleaseDuration : 0.22f;

            float peakIntensity = Mathf.Max(_baselineBloomIntensity, bloomBurstIntensity);
            float peakWeight = Mathf.Clamp01(bloomPeakWeight);
            TraceBloomBurst($"TriggerBloomBurst: baseline={_baselineBloomIntensity:F2}, peak={peakIntensity:F2}, peakWeight={peakWeight:F3}, attack={bloomAttackDuration:F3}s, sustain={bloomSustainDuration:F3}s, release={bloomReleaseDuration:F3}s");

            _boostBloomVolume.weight = 0f;
            _bloomOverride.intensity.value = _baselineBloomIntensity;

            // Phase 1: Attack — ramp up to peak
            _bloomTween = Tween.Custom(
                startValue: 0f,
                endValue: 1f,
                duration: bloomAttackDuration,
                onValueChange: t => ApplyBloomState(
                    Mathf.Lerp(_baselineBloomIntensity, peakIntensity, EaseOutCubic(t)),
                    Mathf.Lerp(0f, peakWeight, EaseOutQuad(t))),
                ease: Ease.Linear)
                .OnComplete(() =>
                {
                    if (_boostBloomVolume == null || _bloomOverride == null) return;

                    // Ensure peak is fully applied during sustain
                    ApplyBloomState(peakIntensity, peakWeight);

                    // Phase 2: Sustain — hold at peak
                    _bloomTween = Tween.Delay(
                        duration: bloomSustainDuration,
                        onComplete: () =>
                        {
                            if (_boostBloomVolume == null || _bloomOverride == null) return;

                            // Phase 3: Release — fade back to baseline
                            _bloomTween = Tween.Custom(
                                startValue: 0f,
                                endValue: 1f,
                                duration: bloomReleaseDuration,
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
                });
        }

        private void ApplyBloomState(float intensity, float weight)
        {
            if (_boostBloomVolume == null || _bloomOverride == null) return;

            _bloomOverride.intensity.value = intensity;
            _boostBloomVolume.weight = weight;
            TraceBloomBurst($"ApplyBloomState: intensity={intensity:F2}, weight={weight:F3}, volume.weight={_boostBloomVolume.weight:F3}");
        }

        private void CancelBoostStartupSequence()
        {
            if (_boostStartupSequenceCts == null)
                return;

            _boostStartupSequenceCts.Cancel();
            _boostStartupSequenceCts.Dispose();
            _boostStartupSequenceCts = null;
        }

        private async UniTaskVoid RunBoostStartupSequenceAsync(
            float sparksDelay,
            float flameDelay,
            float flameThreshold,
            float emberDelay,
            float emberThreshold,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            try
            {
                elapsed = await WaitForStartupMomentAsync(elapsed, sparksDelay, cancellationToken);
                TraceStartupSequence($"Reached EmberSparks moment at t={elapsed:F3}");
                TriggerParticleBurst(_emberSparks);

                elapsed = await WaitForSustainLayerReadyAsync(elapsed, flameDelay, flameThreshold, cancellationToken);
                TraceStartupSequence($"Reached FlameTrail moment at t={elapsed:F3}, intensity={_currentIntensity:F3}");
                PlayParticle(_flameTrailR);
                PlayParticle(_flameTrailB);
                TraceStartupSequence($"Issued PlayParticle for FlameTrail_R/B. isPlayingR={(_flameTrailR != null && _flameTrailR.isPlaying)}, isPlayingB={(_flameTrailB != null && _flameTrailB.isPlaying)}");

                elapsed = await WaitForSustainLayerReadyAsync(elapsed, emberDelay, emberThreshold, cancellationToken);
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
        private static void ValidateCameraPostProcessing()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var urpCamData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamData == null)
            {
                Debug.LogWarning("[BoostTrailView] Main Camera missing UniversalAdditionalCameraData. " +
                    "Bloom burst will not be visible.", cam);
                return;
            }

            if (!urpCamData.renderPostProcessing)
            {
                Debug.LogError("[BoostTrailView] Main Camera has Post Processing DISABLED. " +
                    "Bloom burst will be invisible! Enable 'Post Processing' on the Camera's URP settings.", cam);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void TraceBloomBurst(string message)
        {
            if (!Application.isPlaying)
                return;

            Debug.Log($"[BloomBurstTrace] {message} | time={Time.time:F3}", this);
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
