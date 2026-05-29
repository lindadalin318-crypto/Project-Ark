using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Boost trail VFX controller for the Canary ship.
    /// Manages the active Boost presentation stack:
    ///   - AresBoostTrail: adapted QFZ VFX_Ares_Projectile sustained particle layers
    ///   - Post: URP Volume Bloom burst
    ///
    /// Call OnBoostStart() / OnBoostEnd() / ResetState() from ShipView.
    /// All references via [SerializeField] — no FindObjectOfType.
    /// </summary>
    public class BoostTrailView : MonoBehaviour
    {
        [Header("Ares Sustained Trail")]
        [Tooltip("Adapted QFZ VFX_Ares_Projectile sustained particle layers used for Boost propulsion.")]
        [SerializeField] private ParticleSystem[] _aresSustainParticles = Array.Empty<ParticleSystem>();

        [Header("Post-Processing")]
        [Tooltip("Local Volume for Boost Bloom burst (weight animated 0→1→0).")]
        [SerializeField] private Volume _boostBloomVolume;

        [Header("Settings (Data-Driven)")]
        [Tooltip("ShipJuiceSettingsSO holds Boost Trail parameters shared with other ship juice systems.")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        private PrimeTween.Tween _bloomTween;
        private Bloom _bloomOverride;
        private float _baselineBloomIntensity;

        private void Awake()
        {
            if (_juiceSettings == null)
                Debug.LogError("[BoostTrailView] _juiceSettings is null! Boost Trail timing will use fallback defaults.", this);

            if (_aresSustainParticles == null || _aresSustainParticles.Length == 0)
                Debug.LogError("[BoostTrailView] _aresSustainParticles is empty. Boost will have no Ares trail.", this);

            if (_boostBloomVolume != null)
            {
                _boostBloomVolume.profile.TryGet(out _bloomOverride);
                if (_bloomOverride != null)
                    _baselineBloomIntensity = _bloomOverride.intensity.value;
            }
            else
            {
                Debug.LogError("[BoostTrailView] Awake: _boostBloomVolume is null! Bloom burst will not work.", this);
            }

            ValidateCameraPostProcessing();
            ResetState();
        }

        private void OnDestroy()
        {
            _bloomTween.Stop();
        }

        /// <summary>
        /// Call when the ship enters Boost state.
        /// Activates the Ares trail and triggers bloom burst.
        /// </summary>
        public void OnBoostStart()
        {
            PlayParticles(_aresSustainParticles);
            TriggerBloomBurst();
        }

        /// <summary>
        /// Call when the ship exits Boost state.
        /// Stops Ares emission and lets existing particles finish naturally.
        /// </summary>
        public void OnBoostEnd()
        {
            StopParticles(_aresSustainParticles, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>
        /// Fully resets all VFX state. Call when returning ship to object pool.
        /// </summary>
        public void ResetState()
        {
            _bloomTween.Stop();
            StopAndClearParticles(_aresSustainParticles);
            ResetBloomState();
        }

        /// <summary>
        /// Forces the Ares sustain stack into a deterministic preview state for Inspector debugging.
        /// </summary>
        public void DebugForceSustainPreview(float intensity, bool showAresTrail)
        {
            if (showAresTrail && intensity > 0.001f)
                PlayParticles(_aresSustainParticles);
            else
                StopAndClearParticles(_aresSustainParticles);
        }

        /// <summary>
        /// Applies a visibility mask on top of the live runtime chain so layers can be isolated in Play Mode.
        /// </summary>
        public void DebugApplyVisibilityMask(bool showAresTrail, bool showBloom)
        {
            if (!showAresTrail)
                StopAndClearParticles(_aresSustainParticles);

            if (!showBloom)
                ResetBloomState();
        }

        /// <summary>
        /// Forces the Bloom burst for Inspector debugging.
        /// </summary>
        public void DebugPreviewBloomBurst()
        {
            ResetBloomState();
            TriggerBloomBurst();
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

            _boostBloomVolume.weight = 0f;
            _bloomOverride.intensity.value = _baselineBloomIntensity;

            _bloomTween = PrimeTween.Tween.Custom(
                startValue: 0f,
                endValue: 1f,
                duration: bloomAttackDuration,
                onValueChange: t => ApplyBloomState(
                    Mathf.Lerp(_baselineBloomIntensity, peakIntensity, EaseOutCubic(t)),
                    Mathf.Lerp(0f, peakWeight, EaseOutQuad(t))),
                ease: PrimeTween.Ease.Linear)
                .OnComplete(() =>
                {
                    if (_boostBloomVolume == null || _bloomOverride == null) return;

                    ApplyBloomState(peakIntensity, peakWeight);

                    _bloomTween = PrimeTween.Tween.Delay(
                        duration: bloomSustainDuration,
                        onComplete: () =>
                        {
                            if (_boostBloomVolume == null || _bloomOverride == null) return;

                            _bloomTween = PrimeTween.Tween.Custom(
                                startValue: 0f,
                                endValue: 1f,
                                duration: bloomReleaseDuration,
                                onValueChange: t => ApplyBloomState(
                                    Mathf.Lerp(peakIntensity, _baselineBloomIntensity, EaseOutQuad(t)),
                                    Mathf.Lerp(peakWeight, 0f, EaseOutCubic(t))),
                                ease: PrimeTween.Ease.Linear)
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
        }

        private static void PlayParticles(ParticleSystem[] particleSystems)
        {
            if (particleSystems == null) return;

            foreach (ParticleSystem particleSystem in particleSystems)
                PlayParticle(particleSystem);
        }

        private static void PlayParticle(ParticleSystem particleSystem)
        {
            if (particleSystem == null) return;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play();
        }

        private static void StopParticles(ParticleSystem[] particleSystems, ParticleSystemStopBehavior stopBehavior)
        {
            if (particleSystems == null) return;

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null) continue;
                particleSystem.Stop(false, stopBehavior);
            }
        }

        private static void StopAndClearParticles(ParticleSystem[] particleSystems)
        {
            if (particleSystems == null) return;

            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem == null) continue;
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
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
    }
}
