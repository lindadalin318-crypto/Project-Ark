using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Sole orchestrator for the weaving-state visual / audio transition.
    /// Drives camera zoom, URP post-processing (DoF + Vignette), and SFX
    /// using PrimeTween + UniTask so it works correctly at <c>timeScale = 0</c>.
    /// </summary>
    public class WeavingStateTransition : MonoBehaviour
    {
        // ── External references ──────────────────────────────────────
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Volume _postProcessVolume;
        [SerializeField] private Transform _shipTransform;

        [Header("Audio")]
        [SerializeField] private AudioSource _sfxSource;

        // ── Data-driven settings (optional SO) ──────────────────────
        [Header("Settings")]
        [Tooltip("Optional SO with all tuning knobs. When null, inline defaults below are used.")]
        [SerializeField] private WeavingTransitionSettingsSO _settings;

        // ── Inline fallback defaults (used when _settings == null) ──
        [Header("Fallback — Timing")]
        [SerializeField] private float _enterDuration = 0.35f;
        [SerializeField] private float _exitDuration = 0.25f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Fallback — Camera")]
        [SerializeField] private float _combatCameraSize = 5f;
        [SerializeField] private float _weavingCameraSize = 3f;

        [Header("Fallback — Vignette")]
        [SerializeField] private float _combatVignetteIntensity = 0.1f;
        [SerializeField] private float _weavingVignetteIntensity = 0.5f;

        [Header("Fallback — Depth of Field")]
        [SerializeField] private bool _enableDoFInWeaving = true;
        [SerializeField] private float _weavingFocusDistance = 5f;
        [SerializeField] private float _weavingFocalLength = 50f;

        [Header("Fallback — Audio")]
        [SerializeField] private AudioClip _openSfx;
        [SerializeField] private AudioClip _closeSfx;

        // ── Cached post-processing overrides ─────────────────────────
        private DepthOfField _dof;
        private Vignette _vignette;

        // ── Runtime state ────────────────────────────────────────────
        private CancellationTokenSource _transitionCts;
        private float _cameraZOffset;

        // ══════════════════════════════════════════════════════════════
        // Settings accessors — prefer SO, fall back to inline fields
        // ══════════════════════════════════════════════════════════════
        private float EnterDuration => _settings != null ? _settings.EnterDuration : _enterDuration;
        private float ExitDuration => _settings != null ? _settings.ExitDuration : _exitDuration;
        private AnimationCurve Curve => _settings != null ? _settings.TransitionCurve : _transitionCurve;
        private float CombatSize => _settings != null ? _settings.CombatCameraSize : _combatCameraSize;
        private float WeavingSize => _settings != null ? _settings.WeavingCameraSize : _weavingCameraSize;
        private float CombatVignette => _settings != null ? _settings.CombatVignetteIntensity : _combatVignetteIntensity;
        private float WeavingVignette => _settings != null ? _settings.WeavingVignetteIntensity : _weavingVignetteIntensity;
        private bool DoFEnabled => _settings != null ? _settings.EnableDoFInWeaving : _enableDoFInWeaving;
        private float FocusDistance => _settings != null ? _settings.WeavingFocusDistance : _weavingFocusDistance;
        private float FocalLength => _settings != null ? _settings.WeavingFocalLength : _weavingFocalLength;
        private AudioClip OpenClip => _settings != null ? _settings.OpenSfx : _openSfx;
        private AudioClip CloseClip => _settings != null ? _settings.CloseSfx : _closeSfx;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════
        private void Awake()
        {
            CachePostProcessOverrides();

            // Remember the camera's Z offset so we can preserve it while locking XY.
            if (_mainCamera != null)
                _cameraZOffset = _mainCamera.transform.position.z;

            // Ensure the AudioSource can play while the game is paused.
            if (_sfxSource != null)
                _sfxSource.ignoreListenerPause = true;
        }

        private void OnDestroy()
        {
            CancelTransition();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Begin the combat → weaving state transition.
        /// Safe to call at <c>timeScale = 0</c>.
        /// </summary>
        public void EnterWeavingState()
        {
            PlaySfx(OpenClip);
            StartTransition(
                fromSize: CombatSize,
                toSize: WeavingSize,
                fromVignette: CombatVignette,
                toVignette: WeavingVignette,
                enableDoF: DoFEnabled,
                duration: EnterDuration).Forget();
        }

        /// <summary>
        /// Begin the weaving → combat state transition.
        /// Safe to call at <c>timeScale = 0</c>.
        /// </summary>
        public void ExitWeavingState()
        {
            PlaySfx(CloseClip);
            StartTransition(
                fromSize: WeavingSize,
                toSize: CombatSize,
                fromVignette: WeavingVignette,
                toVignette: CombatVignette,
                enableDoF: false,
                duration: ExitDuration).Forget();
        }

        // ══════════════════════════════════════════════════════════════
        // Transition (UniTask + PrimeTween)
        // ══════════════════════════════════════════════════════════════
        private async UniTaskVoid StartTransition(
            float fromSize, float toSize,
            float fromVignette, float toVignette,
            bool enableDoF, float duration)
        {
            // Cancel any in-flight transition to avoid conflicts.
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            var token = _transitionCts.Token;

            // Snap DoF state at the beginning.
            if (_dof != null)
            {
                _dof.active = enableDoF || _dof.active;
                if (enableDoF)
                {
                    _dof.focusDistance.value = FocusDistance;
                    _dof.focalLength.value = FocalLength;
                }
            }

            // Use PrimeTween's Tween.Custom for smooth interpolation with unscaled time.
            // Camera size tween
            if (_mainCamera != null)
            {
                _ = Tween.Custom(fromSize, toSize, duration, useUnscaledTime: true,
                    onValueChange: v => { if (_mainCamera != null) _mainCamera.orthographicSize = v; },
                    ease: Ease.InOutSine);
            }

            // Vignette tween
            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _ = Tween.Custom(fromVignette, toVignette, duration, useUnscaledTime: true,
                    onValueChange: v => { if (_vignette != null) _vignette.intensity.value = v; },
                    ease: Ease.InOutSine);
            }

            // Lock camera on ship each frame during transition
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) return;

                LockCameraOnShip();
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // Final snap
            LockCameraOnShip();

            // Disable DoF cleanly at end of exit transition.
            if (_dof != null && !enableDoF)
                _dof.active = false;
        }

        private void CancelTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════

        private void CachePostProcessOverrides()
        {
            if (_postProcessVolume == null) return;

            _postProcessVolume.profile.TryGet(out _dof);
            _postProcessVolume.profile.TryGet(out _vignette);
        }

        private void LockCameraOnShip()
        {
            if (_mainCamera == null || _shipTransform == null) return;

            Vector3 shipPos = _shipTransform.position;
            _mainCamera.transform.position = new Vector3(shipPos.x, shipPos.y, _cameraZOffset);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
