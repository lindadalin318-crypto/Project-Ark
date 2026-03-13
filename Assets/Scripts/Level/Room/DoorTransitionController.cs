using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Ship;

namespace ProjectArk.Level
{
    /// <summary>
    /// Handles the visual transition when passing through a door:
    /// fade to black → teleport player → update room → fade from black.
    /// Acts as the shared fade overlay owner for level flow systems.
    /// </summary>
    public class DoorTransitionController : MonoBehaviour
    {
        [Header("Fade Overlay")]
        [Tooltip("Full-screen Image used for the fade-to-black effect. Must be a child of a Canvas.")]
        [SerializeField] private Image _fadeImage;

        [Header("Timing")]
        [Tooltip("Fade duration for normal door transitions (seconds).")]
        [SerializeField] private float _normalFadeDuration = 0.3f;

        [Tooltip("Fade duration for layer transitions (seconds). Longer for dramatic effect.")]
        [SerializeField] private float _layerFadeDuration = 0.5f;

        [Header("Layer Transition Effects")]
        [Tooltip("Optional particle system for layer transitions (e.g., descending/ascending particles).")]
        [SerializeField] private ParticleSystem _layerTransitionParticles;

        [Tooltip("Sound effect played during layer transitions (rumble/whoosh).")]
        [SerializeField] private AudioClip _layerTransitionSFX;

        [Tooltip("BGM crossfade duration when changing floors with different ambient music.")]
        [SerializeField] private float _bgmCrossfadeDuration = 1.5f;

        [Tooltip("Camera zoom-out amount during layer transition (added to current ortho size).")]
        [SerializeField] private float _layerZoomOutAmount = 2f;

        [Tooltip("Camera zoom transition duration.")]
        [SerializeField] private float _layerZoomDuration = 0.3f;

        [Header("Camera Fallback")]
        [Tooltip("Fallback gameplay virtual camera used when CameraDirector is not present.")]
        [SerializeField] private CinemachineCamera _fallbackVirtualCamera;

        private CancellationTokenSource _transitionCts;
        private bool _isTransitioning;
        private CameraDirector _cameraDirector;

        public bool IsTransitioning => _isTransitioning;
        public Image FadeImage => _fadeImage;

        private void Awake()
        {
            ServiceLocator.Register(this);
            ResolveCameraBindings();
            ResetFadeOverlay();
        }

        private void OnDestroy()
        {
            CancelTransition();
            ServiceLocator.Unregister(this);
        }

        public void TransitionThroughDoor(Door door, Action onComplete = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[DoorTransitionController] Transition already in progress. Ignoring.");
                return;
            }

            ExecuteTransition(door, onComplete).Forget();
        }

        public async UniTask FadeOutAsync(float duration, CancellationToken token = default)
        {
            if (_fadeImage == null)
            {
                return;
            }

            _fadeImage.raycastTarget = true;
            await Tween.Custom(_fadeImage.color.a, 1f, duration,
                useUnscaledTime: true,
                ease: Ease.InQuad,
                onValueChange: SetFadeAlpha).ToUniTask(cancellationToken: token);
        }

        public async UniTask FadeInAsync(float duration, CancellationToken token = default)
        {
            if (_fadeImage == null)
            {
                return;
            }

            await Tween.Custom(_fadeImage.color.a, 0f, duration,
                useUnscaledTime: true,
                ease: Ease.OutQuad,
                onValueChange: SetFadeAlpha).ToUniTask(cancellationToken: token);

            _fadeImage.raycastTarget = false;
        }

        private async UniTaskVoid ExecuteTransition(Door door, Action onComplete)
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_transitionCts.Token, destroyCancellationToken);
            var token = linkedCts.Token;

            _isTransitioning = true;

            try
            {
                ResolveCameraBindings();

                float fadeDuration = door.IsLayerTransition ? _layerFadeDuration : _normalFadeDuration;

                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = false;
                }

                await FadeOutAsync(fadeDuration, token);

                if (door.IsLayerTransition)
                {
                    await PlayLayerTransitionEffects(door, token);
                }

                var ship = inputHandler != null ? inputHandler.transform : null;
                if (ship != null && door.TargetSpawnPoint != null)
                {
                    ship.position = door.TargetSpawnPoint.position;
                }

                var roomManager = ServiceLocator.Get<RoomManager>();
                if (roomManager != null && door.TargetRoom != null)
                {
                    roomManager.EnterRoom(door.TargetRoom);
                    _cameraDirector?.ClearAllTriggers();
                    HandleBGMCrossfade(door.TargetRoom);
                }

                await UniTask.Delay(door.IsLayerTransition ? 200 : 50, cancellationToken: token);
                await FadeInAsync(fadeDuration, token);

                if (inputHandler != null)
                {
                    inputHandler.enabled = true;
                }
            }
            catch (OperationCanceledException)
            {
                ResetFadeOverlay();

                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = true;
                }
            }
            finally
            {
                _isTransitioning = false;
                onComplete?.Invoke();
            }
        }

        private async UniTask PlayLayerTransitionEffects(Door door, CancellationToken token)
        {
            var roomManager = ServiceLocator.Get<RoomManager>();
            int currentFloor = roomManager?.CurrentFloor ?? 0;
            int targetFloor = door.TargetRoom?.Data?.FloorLevel ?? 0;
            bool descending = targetFloor < currentFloor;

            if (_layerTransitionParticles != null)
            {
                var mainModule = _layerTransitionParticles.main;
                float speed = Mathf.Abs(mainModule.startSpeed.constant);
                mainModule.startSpeed = descending ? -speed : speed;
                _layerTransitionParticles.Play();
            }

            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null && _layerTransitionSFX != null)
            {
                audio.PlaySFX2D(_layerTransitionSFX);
            }

            bool hasCameraSize = TryGetCurrentOrthoSize(out float baseSize);
            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize, baseSize + _layerZoomOutAmount, _layerZoomDuration);
            }

            await UniTask.Delay(300, cancellationToken: token);

            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Stop();
            }

            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize + _layerZoomOutAmount, baseSize, _layerZoomDuration);
            }
        }

        private void HandleBGMCrossfade(Room targetRoom)
        {
            if (targetRoom?.Data?.AmbientMusic == null)
            {
                return;
            }

            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null)
            {
                audio.PlayMusic(targetRoom.Data.AmbientMusic, _bgmCrossfadeDuration);
            }
        }

        private void ResolveCameraBindings()
        {
            if (_cameraDirector == null)
            {
                _cameraDirector = ServiceLocator.TryGet<CameraDirector>();
            }

            if (_fallbackVirtualCamera == null && _cameraDirector != null)
            {
                _fallbackVirtualCamera = _cameraDirector.VCam;
            }
        }

        private bool TryGetCurrentOrthoSize(out float size)
        {
            ResolveCameraBindings();

            if (_cameraDirector != null)
            {
                size = _cameraDirector.CurrentOrthoSize;
                return true;
            }

            if (_fallbackVirtualCamera != null)
            {
                size = _fallbackVirtualCamera.Lens.OrthographicSize;
                return true;
            }

            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                size = cam.orthographicSize;
                return true;
            }

            size = 0f;
            return false;
        }

        private void TweenOrthoSize(float from, float to, float duration)
        {
            ResolveCameraBindings();

            if (_cameraDirector != null)
            {
                _cameraDirector.SetOrthoSize(to, duration, Ease.InOutSine);
                return;
            }

            if (_fallbackVirtualCamera != null)
            {
                Tween.Custom(from, to, duration,
                    useUnscaledTime: true,
                    ease: Ease.InOutSine,
                    onValueChange: value =>
                    {
                        if (_fallbackVirtualCamera == null)
                        {
                            return;
                        }

                        var lens = _fallbackVirtualCamera.Lens;
                        lens.OrthographicSize = value;
                        _fallbackVirtualCamera.Lens = lens;
                    });
                return;
            }

            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                Tween.Custom(from, to, duration,
                    useUnscaledTime: true,
                    ease: Ease.InOutSine,
                    onValueChange: value =>
                    {
                        if (cam != null)
                        {
                            cam.orthographicSize = value;
                        }
                    });
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeImage == null)
            {
                return;
            }

            var color = _fadeImage.color;
            color.a = alpha;
            _fadeImage.color = color;
        }

        private void ResetFadeOverlay()
        {
            if (_fadeImage == null)
            {
                Debug.LogError("[DoorTransitionController] Fade Image is not assigned!");
                return;
            }

            SetFadeAlpha(0f);
            _fadeImage.raycastTarget = false;
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
    }
}
