using System.Threading;
using Unity.Cinemachine;
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
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private CinemachineCamera _gameplayVirtualCamera;
        [SerializeField] private Volume _postProcessVolume;
        [SerializeField] private Transform _shipTransform;

        [Header("Audio")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("Settings")]
        [Tooltip("Optional SO with all tuning knobs. When null, inline defaults below are used.")]
        [SerializeField] private WeavingTransitionSettingsSO _settings;

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

        private DepthOfField _dof;
        private Vignette _vignette;
        private CancellationTokenSource _transitionCts;
        private float _cameraZOffset;

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

        private void Awake()
        {
            CachePostProcessOverrides();

            if (_mainCamera != null)
            {
                _cameraZOffset = _mainCamera.transform.position.z;
            }

            if (_sfxSource != null)
            {
                _sfxSource.ignoreListenerPause = true;
            }
        }

        private void OnDestroy()
        {
            CancelTransition();
        }

        public void EnterWeavingState()
        {
            PlaySfx(OpenClip);
            StartTransition(
                toSize: WeavingSize,
                toVignette: WeavingVignette,
                enableDoF: DoFEnabled,
                duration: EnterDuration).Forget();
        }

        public void ExitWeavingState()
        {
            PlaySfx(CloseClip);
            StartTransition(
                toSize: CombatSize,
                toVignette: CombatVignette,
                enableDoF: false,
                duration: ExitDuration).Forget();
        }

        private async UniTaskVoid StartTransition(float toSize, float toVignette, bool enableDoF, float duration)
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            var token = _transitionCts.Token;

            if (_dof != null)
            {
                _dof.active = enableDoF || _dof.active;
                if (enableDoF)
                {
                    _dof.focusDistance.value = FocusDistance;
                    _dof.focalLength.value = FocalLength;
                }
            }

            float currentSize = GetCurrentCameraSize();
            TweenCameraSize(currentSize, toSize, duration);

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                float startVignette = _vignette.intensity.value;
                _ = Tween.Custom(startVignette, toVignette, duration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_vignette != null)
                        {
                            _vignette.intensity.value = v;
                        }
                    },
                    ease: Ease.InOutSine);
            }

            if (_gameplayVirtualCamera == null)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    LockCameraOnShip();
                    elapsed += Time.unscaledDeltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                LockCameraOnShip();
            }
            else
            {
                await UniTask.Delay(Mathf.RoundToInt(duration * 1000f), ignoreTimeScale: true, cancellationToken: token);
            }

            if (_dof != null && !enableDoF)
            {
                _dof.active = false;
            }
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

        private void CachePostProcessOverrides()
        {
            if (_postProcessVolume == null)
            {
                return;
            }

            _postProcessVolume.profile.TryGet(out _dof);
            _postProcessVolume.profile.TryGet(out _vignette);
        }

        private float GetCurrentCameraSize()
        {
            if (_gameplayVirtualCamera != null)
            {
                return _gameplayVirtualCamera.Lens.OrthographicSize;
            }

            if (_mainCamera != null)
            {
                return _mainCamera.orthographicSize;
            }

            return CombatSize;
        }

        private void TweenCameraSize(float fromSize, float toSize, float duration)
        {
            if (_gameplayVirtualCamera != null)
            {
                _ = Tween.Custom(fromSize, toSize, duration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_gameplayVirtualCamera == null)
                        {
                            return;
                        }

                        var lens = _gameplayVirtualCamera.Lens;
                        lens.OrthographicSize = v;
                        _gameplayVirtualCamera.Lens = lens;
                    },
                    ease: Ease.InOutSine);
                return;
            }

            if (_mainCamera != null)
            {
                _ = Tween.Custom(fromSize, toSize, duration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_mainCamera != null)
                        {
                            _mainCamera.orthographicSize = v;
                        }
                    },
                    ease: Ease.InOutSine);
            }
        }

        private void LockCameraOnShip()
        {
            if (_mainCamera == null || _shipTransform == null)
            {
                return;
            }

            Vector3 shipPos = _shipTransform.position;
            _mainCamera.transform.position = new Vector3(shipPos.x, shipPos.y, _cameraZOffset);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _sfxSource == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }
    }
}
