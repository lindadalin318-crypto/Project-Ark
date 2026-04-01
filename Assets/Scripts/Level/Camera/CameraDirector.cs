using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Camera mode enumeration - matches Minishoot/Silksong patterns.
    /// </summary>
    public enum CameraMode
    {
        /// <summary>Normal follow mode - camera follows the player's ship.</summary>
        FOLLOWING = 0,

        /// <summary>Camera locked to a fixed target/position.</summary>
        LOCKED = 1,

        /// <summary>Camera panning from current position to a target position.</summary>
        PANNING = 2,

        /// <summary>Camera frozen in place (e.g., during dialogue or pause).</summary>
        FROZEN = 3,

        /// <summary>Fade to black (screen goes dark).</summary>
        FADEOUT = 4,

        /// <summary>Fade from black (screen becomes visible).</summary>
        FADEIN = 5
    }

    /// <summary>
    /// Camera state snapshot for saving/restoring camera parameters.
    /// </summary>
    [Serializable]
    public struct CameraState
    {
        public float OrthographicSize;
        public Vector3 Position;
        public Transform FollowTarget;
        public Transform LookAtTarget;
    }

    /// <summary>
    /// Central camera controller - manages camera modes, transitions, and state.
    /// Acts as the "Camera Director" equivalent from Minishoot/Silksong.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The main CinemachineCamera in the scene.")]
        [SerializeField] private CinemachineCamera _vcam;

        [Header("Default Settings")]
        [Tooltip("Default orthographic size. Leave at 0 to adopt the current vcam size on Awake.")]
        [SerializeField] private float _defaultOrthoSize;

        [Tooltip("Default follow target (usually the player ship). Leave null to adopt the current vcam Follow target on Awake.")]
        [SerializeField] private Transform _defaultFollowTarget;

        [Header("Transition Settings")]
        [Tooltip("Default transition duration for mode changes (seconds).")]
        [SerializeField] private float _defaultTransitionDuration = 0.5f;

        [Tooltip("Default easing for transitions.")]
        [SerializeField] private Ease _defaultEase = Ease.InOutSine;

        [Header("Fade Settings")]
        [Tooltip("CanvasGroup for screen fade (assign FadeOverlay from Canvas).")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;

        private CameraMode _currentMode = CameraMode.FOLLOWING;
        private CameraMode _previousModeBeforeFreeze = CameraMode.FOLLOWING;
        private CameraState _savedState;
        private bool _hasSavedState;
        private Tween _activeTween;
        private readonly List<CameraTrigger> _activeTriggers = new();
        private bool _isFading;

        public CameraMode CurrentMode => _currentMode;
        public CinemachineCamera VCam => _vcam;
        public float DefaultOrthoSize => _defaultOrthoSize;
        public Transform DefaultFollowTarget => _defaultFollowTarget;
        public bool IsFading => _isFading;
        public float CurrentOrthoSize => _vcam != null ? _vcam.Lens.OrthographicSize : _defaultOrthoSize;
        public Transform CurrentFollowTarget => _vcam != null ? _vcam.Follow : _defaultFollowTarget;
        public CameraTrigger CurrentActiveTrigger => GetHighestPriorityTrigger();

        public event Action<CameraMode> OnModeChanged;
        public event Action<CameraMode, CameraMode> OnModeTransition;

        private void Awake()
        {
            ServiceLocator.Register(this);

            if (_vcam == null)
            {
                _vcam = GetComponent<CinemachineCamera>();
                if (_vcam == null)
                {
                    Debug.LogError("[CameraDirector] CinemachineCamera not assigned and not found on this GameObject!");
                }
            }

            if (_vcam != null)
            {
                if (_defaultOrthoSize <= 0f)
                {
                    _defaultOrthoSize = _vcam.Lens.OrthographicSize;
                }

                if (_defaultFollowTarget == null)
                {
                    _defaultFollowTarget = _vcam.Follow;
                }
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            SaveCurrentState();
        }

        private void OnDestroy()
        {
            StopActiveTween();
            ServiceLocator.Unregister(this);
        }

        public void SetMode(CameraMode newMode, float transitionDuration = -1f)
        {
            if (_currentMode == newMode && newMode != CameraMode.FADEOUT && newMode != CameraMode.FADEIN)
            {
                return;
            }

            var fromMode = _currentMode;
            _currentMode = newMode;
            float duration = transitionDuration >= 0f ? transitionDuration : _defaultTransitionDuration;

            switch (newMode)
            {
                case CameraMode.FOLLOWING:
                    ApplyFollowMode(duration);
                    break;
                case CameraMode.LOCKED:
                    break;
                case CameraMode.PANNING:
                    break;
                case CameraMode.FROZEN:
                    _previousModeBeforeFreeze = fromMode == CameraMode.FROZEN ? _previousModeBeforeFreeze : fromMode;
                    break;
                case CameraMode.FADEOUT:
                    FadeOutAsync(duration).Forget();
                    break;
                case CameraMode.FADEIN:
                    FadeInAsync(duration).Forget();
                    break;
            }

            OnModeChanged?.Invoke(newMode);
            OnModeTransition?.Invoke(fromMode, newMode);
        }

        public void RestoreToFollow(float transitionDuration = -1f)
        {
            SetMode(CameraMode.FOLLOWING, transitionDuration);
        }

        public void PanToPosition(
            Vector3 targetPosition,
            float duration = 1f,
            Ease? ease = null,
            Action onComplete = null,
            bool returnAfterDelay = false,
            float returnDelay = 1f)
        {
            if (_vcam == null)
            {
                return;
            }

            SaveCurrentState();
            StopActiveTween();

            _vcam.Follow = null;
            _vcam.LookAt = null;

            SetMode(CameraMode.PANNING, 0f);

            Vector3 startPos = _vcam.transform.position;
            targetPosition.z = startPos.z;

            _activeTween = Tween.Custom(startPos, targetPosition, duration,
                useUnscaledTime: true,
                ease: ease ?? _defaultEase,
                onValueChange: pos =>
                {
                    if (_vcam == null)
                    {
                        return;
                    }

                    _vcam.transform.position = pos;
                })
                .OnComplete(() =>
                {
                    onComplete?.Invoke();

                    if (returnAfterDelay)
                    {
                        ReturnAfterDelay(returnDelay).Forget();
                    }
                });
        }

        public void PanToTarget(Transform target, float duration = 1f, bool returnAfterDelay = false)
        {
            if (target == null)
            {
                Debug.LogWarning("[CameraDirector] PanToTarget called with null target.");
                return;
            }

            Vector3 targetCamPos = target.position;
            if (_vcam != null)
            {
                targetCamPos.z = _vcam.transform.position.z;
            }

            PanToPosition(targetCamPos, duration, null, null, returnAfterDelay);
        }

        public void SaveCurrentState()
        {
            if (_vcam == null)
            {
                return;
            }

            _savedState = new CameraState
            {
                OrthographicSize = _vcam.Lens.OrthographicSize,
                Position = _vcam.transform.position,
                FollowTarget = _vcam.Follow,
                LookAtTarget = _vcam.LookAt
            };
            _hasSavedState = true;
        }

        public async UniTask RestoreFromSavedState(float duration = -1f)
        {
            if (!_hasSavedState || _vcam == null)
            {
                return;
            }

            StopActiveTween();
            float transDuration = duration >= 0f ? duration : _defaultTransitionDuration;
            float startSize = _vcam.Lens.OrthographicSize;
            float targetSize = _savedState.OrthographicSize;

            if (!Mathf.Approximately(startSize, targetSize))
            {
                _ = Tween.Custom(startSize, targetSize, transDuration,
                    useUnscaledTime: true,
                    ease: _defaultEase,
                    onValueChange: SetLensOrthoSize);
            }

            Vector3 startPos = _vcam.transform.position;
            Vector3 targetPos = _savedState.Position;
            targetPos.z = startPos.z;

            _activeTween = Tween.Custom(startPos, targetPos, transDuration,
                useUnscaledTime: true,
                ease: _defaultEase,
                onValueChange: pos =>
                {
                    if (_vcam == null)
                    {
                        return;
                    }

                    _vcam.transform.position = pos;
                });

            await _activeTween.ToUniTask();

            _vcam.Follow = _savedState.FollowTarget;
            _vcam.LookAt = _savedState.LookAtTarget;

            var fromMode = _currentMode;
            _currentMode = CameraMode.FOLLOWING;
            OnModeChanged?.Invoke(_currentMode);
            OnModeTransition?.Invoke(fromMode, _currentMode);
        }

        public void SetOrthoSize(float targetSize, float duration = -1f, Ease? ease = null)
        {
            if (_vcam == null)
            {
                return;
            }

            StopActiveTween();

            float transDuration = duration >= 0f ? duration : _defaultTransitionDuration;
            float startSize = _vcam.Lens.OrthographicSize;

            _activeTween = Tween.Custom(startSize, targetSize, transDuration,
                useUnscaledTime: true,
                ease: ease ?? _defaultEase,
                onValueChange: SetLensOrthoSize);
        }

        public void RestoreOrthoSize(float duration = -1f)
        {
            SetOrthoSize(_defaultOrthoSize, duration);
        }

        public void SetFollowTarget(Transform target, float duration = -1f)
        {
            if (_vcam == null)
            {
                return;
            }

            _vcam.Follow = target;
        }

        public void RestoreFollowTarget()
        {
            SetFollowTarget(_defaultFollowTarget);
        }

        public void SetLookAtTarget(Transform target)
        {
            if (_vcam == null)
            {
                return;
            }

            _vcam.LookAt = target;
        }

        public void ClearLookAtTarget()
        {
            SetLookAtTarget(null);
        }

        public void PushTrigger(CameraTrigger trigger)
        {
            if (trigger == null || _activeTriggers.Contains(trigger))
            {
                return;
            }

            _activeTriggers.Add(trigger);
            EvaluateTopTrigger();
        }

        public void PopTrigger(CameraTrigger trigger)
        {
            if (trigger == null)
            {
                return;
            }

            if (_activeTriggers.Remove(trigger))
            {
                EvaluateTopTrigger();
            }
        }

        public void ClearAllTriggers(bool restoreFollow = true)
        {
            _activeTriggers.Clear();

            if (restoreFollow)
            {
                RestoreToFollow(0f);
            }
        }

        public async UniTask FadeOutAsync(float duration = -1f)
        {
            if (_fadeCanvasGroup == null)
            {
                Debug.LogWarning("[CameraDirector] Fade CanvasGroup not assigned. Fade skipped.");
                return;
            }

            _isFading = true;
            float transDuration = duration >= 0f ? duration : _defaultTransitionDuration;
            _fadeCanvasGroup.blocksRaycasts = true;

            await Tween.Custom(_fadeCanvasGroup.alpha, 1f, transDuration,
                useUnscaledTime: true,
                ease: Ease.InQuad,
                onValueChange: v => _fadeCanvasGroup.alpha = v).ToUniTask();

            _isFading = false;
        }

        public async UniTask FadeInAsync(float duration = -1f)
        {
            if (_fadeCanvasGroup == null)
            {
                Debug.LogWarning("[CameraDirector] Fade CanvasGroup not assigned. Fade skipped.");
                return;
            }

            _isFading = true;
            float transDuration = duration >= 0f ? duration : _defaultTransitionDuration;

            await Tween.Custom(_fadeCanvasGroup.alpha, 0f, transDuration,
                useUnscaledTime: true,
                ease: Ease.OutQuad,
                onValueChange: v => _fadeCanvasGroup.alpha = v).ToUniTask();

            _fadeCanvasGroup.blocksRaycasts = false;
            _isFading = false;
        }

        public void AutoFindReferences()
        {
            if (_vcam == null)
            {
            _vcam = FindAnyObjectByType<CinemachineCamera>();
            }

            if (_fadeCanvasGroup == null)
            {
                var fadeOverlay = GameObject.Find("FadeOverlay");
                if (fadeOverlay != null)
                {
                    _fadeCanvasGroup = fadeOverlay.GetComponent<CanvasGroup>();
                }
            }

            if (_defaultFollowTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _defaultFollowTarget = player.transform;
                }
            }

            if (_defaultOrthoSize <= 0f && _vcam != null)
            {
                _defaultOrthoSize = _vcam.Lens.OrthographicSize;
            }
        }

        private async UniTaskVoid ReturnAfterDelay(float delay)
        {
            await UniTask.Delay(Mathf.RoundToInt(delay * 1000f), ignoreTimeScale: true);
            await RestoreFromSavedState(_defaultTransitionDuration);
        }

        private void ApplyFollowMode(float duration)
        {
            RestoreFollowTarget();
            ClearLookAtTarget();
            RestoreOrthoSize(duration);
        }

        private CameraTrigger GetHighestPriorityTrigger()
        {
            CameraTrigger best = null;

            for (int i = 0; i < _activeTriggers.Count; i++)
            {
                var candidate = _activeTriggers[i];
                if (candidate == null)
                {
                    continue;
                }

                if (best == null || candidate.Priority >= best.Priority)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private void EvaluateTopTrigger()
        {
            _activeTriggers.RemoveAll(trigger => trigger == null);

            var topTrigger = GetHighestPriorityTrigger();
            if (topTrigger == null)
            {
                RestoreToFollow();
                return;
            }

            topTrigger.ApplyToCamera(this);
        }

        private void SetLensOrthoSize(float value)
        {
            if (_vcam == null)
            {
                return;
            }

            var lens = _vcam.Lens;
            lens.OrthographicSize = value;
            _vcam.Lens = lens;
        }

        private void StopActiveTween()
        {
            _activeTween.Stop();
        }
    }
}
