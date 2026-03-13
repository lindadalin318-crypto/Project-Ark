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
    /// Biome trigger (Minishoot "BiomeTrigger" equivalent).
    /// 
    /// When the player enters this trigger zone, it applies a local ambience preset
    /// that overrides the global AmbienceController. When the player exits, the
    /// override is removed and global ambience is restored.
    /// 
    /// Supports: color filter, vignette, BGM crossfade, low-pass filter, zone particles.
    /// Place as a child of a Room or as a standalone trigger zone.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BiomeTrigger : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Ambience Preset")]
        [Tooltip("The ambience preset to apply when player enters this zone.")]
        [SerializeField] private RoomAmbienceSO _ambiencePreset;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private bool _playerInZone;
        private ParticleSystem _activeParticles;
        private CancellationTokenSource _transitionCts;

        // Cached restore values (to revert when player exits)
        private float _savedVignetteIntensity;
        private Color _savedColorFilter;
        private bool _hasSavedState;

        // ──────────────────── Post-Processing Cache ────────────────────

        private Volume _postProcessVolume;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Validate trigger collider
            var boxCollider = GetComponent<BoxCollider2D>();
            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning($"[BiomeTrigger] {gameObject.name}: BoxCollider2D was not set as trigger. Auto-fixed.");
            }

            if (_ambiencePreset == null)
            {
                Debug.LogError($"[BiomeTrigger] {gameObject.name}: RoomAmbienceSO not assigned!");
            }
        }

        private void Start()
        {
            // Try to find the global post-processing volume from AmbienceController
            CachePostProcessing();
        }

        private void OnDestroy()
        {
            CancelTransition();
            CleanupParticles();
        }

        // ──────────────────── Post-Processing Discovery ────────────────────

        private void CachePostProcessing()
        {
            // Find the global volume — prefer finding it through AmbienceController to stay consistent
            _postProcessVolume = FindAnyObjectByType<Volume>();
            if (_postProcessVolume != null && _postProcessVolume.profile != null)
            {
                _postProcessVolume.profile.TryGet(out _vignette);
                _postProcessVolume.profile.TryGet(out _colorAdjustments);
            }
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            if (_playerInZone) return;

            _playerInZone = true;
            ApplyAmbience();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            if (!_playerInZone) return;

            _playerInZone = false;
            RevertAmbience();
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Apply Ambience ────────────────────

        private void ApplyAmbience()
        {
            if (_ambiencePreset == null) return;

            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            float duration = _ambiencePreset.TransitionDuration;

            // Save current state for revert
            SaveCurrentState();

            // ── Post-Processing ──
            ApplyPostProcessing(duration);

            // ── Audio ──
            ApplyAudio();

            // ── Particles ──
            ApplyParticles();

            Debug.Log($"[BiomeTrigger] {gameObject.name}: Applied ambience '{_ambiencePreset.PresetName}'");
        }

        private void SaveCurrentState()
        {
            if (_vignette != null)
                _savedVignetteIntensity = _vignette.intensity.value;
            if (_colorAdjustments != null)
                _savedColorFilter = _colorAdjustments.colorFilter.value;
            _hasSavedState = true;
        }

        private void ApplyPostProcessing(float duration)
        {
            // ── Vignette ──
            if (_ambiencePreset.HasVignetteOverride && _vignette != null)
            {
                float current = _vignette.intensity.value;
                float target = _ambiencePreset.VignetteIntensityOverride;

                _vignette.intensity.overrideState = true;
                _ = Tween.Custom(current, target, duration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_vignette != null)
                            _vignette.intensity.value = v;
                    },
                    ease: Ease.InOutSine);
            }

            // ── Color Filter ──
            if (_ambiencePreset.HasColorOverride && _colorAdjustments != null)
            {
                Color current = _colorAdjustments.colorFilter.value;
                Color target = _ambiencePreset.AmbientColorOverride;

                _colorAdjustments.colorFilter.overrideState = true;
                _ = Tween.Custom(0f, 1f, duration, useUnscaledTime: true,
                    onValueChange: t =>
                    {
                        if (_colorAdjustments != null)
                            _colorAdjustments.colorFilter.value = Color.Lerp(current, target, t);
                    },
                    ease: Ease.InOutSine);
            }
        }

        private void ApplyAudio()
        {
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio == null) return;

            // BGM crossfade
            if (_ambiencePreset.HasBGMOverride)
            {
                audio.PlayMusic(_ambiencePreset.BGMOverride, _ambiencePreset.TransitionDuration);
            }

            // Low-pass filter
            if (_ambiencePreset.ApplyLowPass)
            {
                audio.ApplyLowPassFilter(_ambiencePreset.LowPassCutoffHz, _ambiencePreset.TransitionDuration);
            }
        }

        private void ApplyParticles()
        {
            if (!_ambiencePreset.HasParticles) return;

            // Instantiate particle system at trigger center
            if (_activeParticles == null)
            {
                _activeParticles = Instantiate(_ambiencePreset.ParticlePrefab, transform.position, Quaternion.identity, transform);
            }

            _activeParticles.Play(true);
        }

        // ──────────────────── Revert Ambience ────────────────────

        private void RevertAmbience()
        {
            if (_ambiencePreset == null || !_hasSavedState) return;

            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            float duration = _ambiencePreset.TransitionDuration;

            // ── Revert Post-Processing ──
            RevertPostProcessing(duration);

            // ── Revert Audio ──
            RevertAudio();

            // ── Stop Particles ──
            StopParticles();

            Debug.Log($"[BiomeTrigger] {gameObject.name}: Reverted ambience '{_ambiencePreset.PresetName}'");
        }

        private void RevertPostProcessing(float duration)
        {
            // ── Vignette ──
            if (_ambiencePreset.HasVignetteOverride && _vignette != null)
            {
                float current = _vignette.intensity.value;
                float target = _savedVignetteIntensity;

                _ = Tween.Custom(current, target, duration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_vignette != null)
                            _vignette.intensity.value = v;
                    },
                    ease: Ease.InOutSine);
            }

            // ── Color Filter ──
            if (_ambiencePreset.HasColorOverride && _colorAdjustments != null)
            {
                Color current = _colorAdjustments.colorFilter.value;
                Color target = _savedColorFilter;

                _ = Tween.Custom(0f, 1f, duration, useUnscaledTime: true,
                    onValueChange: t =>
                    {
                        if (_colorAdjustments != null)
                            _colorAdjustments.colorFilter.value = Color.Lerp(current, target, t);
                    },
                    ease: Ease.InOutSine);
            }
        }

        private void RevertAudio()
        {
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio == null) return;

            // Remove low-pass filter
            if (_ambiencePreset.ApplyLowPass)
            {
                audio.RemoveLowPassFilter(_ambiencePreset.TransitionDuration);
            }

            // BGM: let the global AmbienceController / Room handle restoration
            // We don't explicitly revert BGM here — the room's ambient music or phase BGM
            // will naturally take over through existing event handlers.
        }

        private void StopParticles()
        {
            if (_activeParticles != null)
            {
                _activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // ──────────────────── Cleanup ────────────────────

        private void CancelTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }

        private void CleanupParticles()
        {
            if (_activeParticles != null)
            {
                Destroy(_activeParticles.gameObject);
                _activeParticles = null;
            }
        }
    }
}
