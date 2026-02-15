using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
using ProjectArk.Core.Audio;

namespace ProjectArk.Level
{
    /// <summary>
    /// Drives global ambience changes in response to world time phase transitions.
    /// Controls: post-processing (Vignette, ColorAdjustments), environment particles,
    /// BGM crossfade, and audio low-pass filter.
    /// 
    /// ServiceLocator registered. Place on a persistent manager GameObject.
    /// </summary>
    public class AmbienceController : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Post-Processing")]
        [Tooltip("URP Post-Processing Volume to control. Should be set to Global or cover the entire level.")]
        [SerializeField] private Volume _postProcessVolume;

        [Tooltip("Duration for post-processing transitions between phases (seconds).")]
        [SerializeField] private float _postProcessTransitionDuration = 2f;

        [Header("Vignette")]
        [Tooltip("Default vignette intensity (used when phase has no special vignette).")]
        [SerializeField] private float _defaultVignetteIntensity = 0.2f;

        [Tooltip("Storm phase vignette intensity (darker edges for oppressive feel).")]
        [SerializeField] private float _stormVignetteIntensity = 0.45f;

        [Tooltip("Radiation phase vignette intensity.")]
        [SerializeField] private float _radiationVignetteIntensity = 0.35f;

        [Header("Environment Particles")]
        [Tooltip("Particle systems for each phase index. Array index matches phase index. Null entries = no particles for that phase.")]
        [SerializeField] private ParticleSystem[] _phaseParticles;

        [Header("Audio")]
        [Tooltip("BGM crossfade duration when switching phases (seconds).")]
        [SerializeField] private float _bgmCrossfadeDuration = 2f;

        [Tooltip("Low-pass filter transition duration (seconds).")]
        [SerializeField] private float _lowPassTransitionDuration = 1f;

        // ──────────────────── Runtime State ────────────────────

        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private int _currentPhaseIndex = -1;
        private CancellationTokenSource _transitionCts;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
            CacheVolumeOverrides();
        }

        private void Start()
        {
            LevelEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDestroy()
        {
            LevelEvents.OnPhaseChanged -= HandlePhaseChanged;
            CancelTransition();
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Volume Cache ────────────────────

        private void CacheVolumeOverrides()
        {
            if (_postProcessVolume == null)
            {
                Debug.LogWarning("[AmbienceController] Post-Processing Volume not assigned.");
                return;
            }

            var profile = _postProcessVolume.profile;
            if (profile == null) return;

            profile.TryGet(out _vignette);
            profile.TryGet(out _colorAdjustments);
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandlePhaseChanged(int phaseIndex, string phaseName)
        {
            if (phaseIndex == _currentPhaseIndex) return;

            int previousPhase = _currentPhaseIndex;
            _currentPhaseIndex = phaseIndex;

            TransitionToPhase(phaseIndex, previousPhase).Forget();
        }

        // ──────────────────── Phase Transition ────────────────────

        private async UniTaskVoid TransitionToPhase(int phaseIndex, int previousPhase)
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _transitionCts.Token, destroyCancellationToken);
            var token = linkedCts.Token;

            try
            {
                // Get phase data
                var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
                WorldPhaseSO phase = null;
                if (phaseManager != null && phaseManager.Phases != null &&
                    phaseIndex >= 0 && phaseIndex < phaseManager.Phases.Length)
                {
                    phase = phaseManager.Phases[phaseIndex];
                }

                // ── 1. Post-Processing Transition ──
                TransitionPostProcessing(phase);

                // ── 2. Environment Particles ──
                TransitionParticles(phaseIndex, previousPhase);

                // ── 3. BGM Crossfade ──
                TransitionBGM(phase);

                // ── 4. Low-Pass Filter ──
                TransitionLowPass(phase);

                // Await transition duration to allow cancellation tracking
                int durationMs = Mathf.RoundToInt(_postProcessTransitionDuration * 1000f);
                await UniTask.Delay(durationMs, cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                // Transition was cancelled by a new phase change — expected
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        // ──────────────────── Post-Processing ────────────────────

        private void TransitionPostProcessing(WorldPhaseSO phase)
        {
            if (phase == null) return;

            // ── Vignette ──
            if (_vignette != null)
            {
                float targetIntensity = GetVignetteIntensityForPhase(phase);
                float currentIntensity = _vignette.intensity.value;

                _vignette.intensity.overrideState = true;
                _ = Tween.Custom(currentIntensity, targetIntensity, _postProcessTransitionDuration,
                    useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_vignette != null)
                            _vignette.intensity.value = v;
                    },
                    ease: Ease.InOutSine);
            }

            // ── Color Adjustments (color filter tint) ──
            if (_colorAdjustments != null)
            {
                Color currentColor = _colorAdjustments.colorFilter.value;
                Color targetColor = phase.AmbientColor;

                _colorAdjustments.colorFilter.overrideState = true;
                _ = Tween.Custom(0f, 1f, _postProcessTransitionDuration,
                    useUnscaledTime: true,
                    onValueChange: t =>
                    {
                        if (_colorAdjustments != null)
                            _colorAdjustments.colorFilter.value = Color.Lerp(currentColor, targetColor, t);
                    },
                    ease: Ease.InOutSine);
            }
        }

        private float GetVignetteIntensityForPhase(WorldPhaseSO phase)
        {
            // 根据阶段名关键字匹配特定 vignette 强度
            if (phase.PhaseName != null)
            {
                string name = phase.PhaseName.ToLowerInvariant();
                if (name.Contains("storm") || name.Contains("风暴"))
                    return _stormVignetteIntensity;
                if (name.Contains("radiation") || name.Contains("辐射"))
                    return _radiationVignetteIntensity;
            }

            return _defaultVignetteIntensity;
        }

        // ──────────────────── Particles ────────────────────

        private void TransitionParticles(int newPhase, int previousPhase)
        {
            if (_phaseParticles == null || _phaseParticles.Length == 0) return;

            // Stop previous phase particles
            if (previousPhase >= 0 && previousPhase < _phaseParticles.Length)
            {
                var prevParticles = _phaseParticles[previousPhase];
                if (prevParticles != null) prevParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            // Start new phase particles
            if (newPhase >= 0 && newPhase < _phaseParticles.Length)
            {
                var newParticles = _phaseParticles[newPhase];
                if (newParticles != null) newParticles.Play(true);
            }
        }

        // ──────────────────── Audio ────────────────────

        private void TransitionBGM(WorldPhaseSO phase)
        {
            if (phase == null || phase.PhaseBGM == null) return;

            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null)
            {
                audio.PlayMusic(phase.PhaseBGM, _bgmCrossfadeDuration);
            }
        }

        private void TransitionLowPass(WorldPhaseSO phase)
        {
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio == null) return;

            if (phase != null && phase.ApplyLowPassFilter)
            {
                audio.ApplyLowPassFilter(phase.LowPassCutoffHz, _lowPassTransitionDuration);
            }
            else
            {
                audio.RemoveLowPassFilter(_lowPassTransitionDuration);
            }
        }

        // ──────────────────── Cancellation ────────────────────

        private void CancelTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }
    }
}
