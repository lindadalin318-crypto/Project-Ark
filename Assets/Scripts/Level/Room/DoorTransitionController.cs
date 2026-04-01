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

        [Tooltip("Fade duration for Boss door transitions (seconds).")]
        [SerializeField] private float _bossFadeDuration = 0.8f;

        [Tooltip("Fade duration for Heavy door transitions (seconds).")]
        [SerializeField] private float _heavyFadeDuration = 1.0f;

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

        [Header("Boss Transition")]
        [Tooltip("Sound effect played during Boss door transitions (dramatic reveal).")]
        [SerializeField] private AudioClip _bossTransitionSFX;

        [Tooltip("Camera zoom-out amount during Boss door transition (more dramatic than Layer).")]
        [SerializeField] private float _bossZoomOutAmount = 3f;

        [Tooltip("Screen shake intensity for Boss door transitions.")]
        [SerializeField] private float _bossShakeIntensity = 0.15f;

        [Header("Heavy Transition")]
        [Tooltip("Sound effect played during Heavy door transitions (mechanical / grinding).")]
        [SerializeField] private AudioClip _heavyTransitionSFX;

        [Tooltip("Screen shake intensity for Heavy door transitions (stronger than Boss).")]
        [SerializeField] private float _heavyShakeIntensity = 0.25f;

        [Tooltip("Camera zoom-out amount during Heavy door transition.")]
        [SerializeField] private float _heavyZoomOutAmount = 4f;

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
            // Reset the static door cooldown so it doesn't bleed into the next scene load
            Door.ResetGlobalTransitionCooldown();
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

                // ── 解析目标（直接使用 Door 引用）──
                Room targetRoom = door.TargetRoom;
                Transform targetSpawn = door.TargetSpawnPoint;

                // ── Ceremony 决定演出参数 ──
                var ceremony = door.Ceremony;
                float fadeDuration = GetFadeDuration(ceremony);

                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = false;
                }

                // ── None = 瞬间切换，跳过淡黑 ──
                if (ceremony != TransitionCeremony.None)
                {
                    await FadeOutAsync(fadeDuration, token);
                }

                // ── 按 Ceremony 分级执行追加演出 ──
                switch (ceremony)
                {
                    case TransitionCeremony.Layer:
                        await PlayLayerTransitionEffects(door, targetRoom, token);
                        break;
                    case TransitionCeremony.Boss:
                        await PlayBossTransitionEffects(door, targetRoom, token);
                        break;
                    case TransitionCeremony.Heavy:
                        await PlayHeavyTransitionEffects(door, targetRoom, token);
                        break;
                }

                // ── 传送玩家 ──
                var ship = inputHandler != null ? inputHandler.transform : null;
                if (ship != null && targetSpawn != null)
                {
                    ship.position = targetSpawn.position;
                }

                // ── 切换房间 ──
                var roomManager = ServiceLocator.Get<RoomManager>();
                if (roomManager != null && targetRoom != null)
                {
                    roomManager.EnterRoom(targetRoom);
                    _cameraDirector?.ClearAllTriggers();
                    HandleBGMCrossfade(targetRoom);
                }

                if (ceremony != TransitionCeremony.None)
                {
                    bool isExtended = ceremony == TransitionCeremony.Layer
                                   || ceremony == TransitionCeremony.Boss
                                   || ceremony == TransitionCeremony.Heavy;
                    await UniTask.Delay(isExtended ? 200 : 50, cancellationToken: token);
                    await FadeInAsync(fadeDuration, token);
                }

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

        // ──────────────────── Ceremony Timing ────────────────────

        private float GetFadeDuration(TransitionCeremony ceremony)
        {
            return ceremony switch
            {
                TransitionCeremony.None => 0f,
                TransitionCeremony.Standard => _normalFadeDuration,
                TransitionCeremony.Layer => _layerFadeDuration,
                TransitionCeremony.Boss => _bossFadeDuration,
                TransitionCeremony.Heavy => _heavyFadeDuration,
                _ => _normalFadeDuration
            };
        }

        private async UniTask PlayLayerTransitionEffects(Door door, Room targetRoom, CancellationToken token)
        {
            var roomManager = ServiceLocator.Get<RoomManager>();
            int currentFloor = roomManager?.CurrentFloor ?? 0;
            int targetFloor = targetRoom?.Data?.FloorLevel ?? 0;
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

        /// <summary>
        /// Boss door transition: dramatic zoom-out + dedicated SFX + screen shake.
        /// Distinct from Layer transition to convey "you are entering something significant".
        /// </summary>
        private async UniTask PlayBossTransitionEffects(Door door, Room targetRoom, CancellationToken token)
        {
            // Particles (shared with layer for now, can be overridden later)
            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Play();
            }

            // Boss-specific SFX (fallback to layer SFX if not assigned)
            var audio = ServiceLocator.Get<AudioManager>();
            var sfx = _bossTransitionSFX != null ? _bossTransitionSFX : _layerTransitionSFX;
            if (audio != null && sfx != null)
            {
                audio.PlaySFX2D(sfx);
            }

            // Dramatic zoom-out (larger than layer)
            bool hasCameraSize = TryGetCurrentOrthoSize(out float baseSize);
            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize, baseSize + _bossZoomOutAmount, _layerZoomDuration * 1.5f);
            }

            // Screen shake via camera
            if (_bossShakeIntensity > 0f)
            {
                ApplyCameraShake(_bossShakeIntensity, 0.4f);
            }

            await UniTask.Delay(500, cancellationToken: token);

            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Stop();
            }

            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize + _bossZoomOutAmount, baseSize, _layerZoomDuration);
            }
        }

        /// <summary>
        /// Heavy door transition: multi-phase shake + grinding SFX + extreme zoom.
        /// The heaviest ceremony — conveys "this world just shifted".
        /// </summary>
        private async UniTask PlayHeavyTransitionEffects(Door door, Room targetRoom, CancellationToken token)
        {
            // Particles
            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Play();
            }

            // Heavy-specific SFX (fallback chain)
            var audio = ServiceLocator.Get<AudioManager>();
            var sfx = _heavyTransitionSFX != null ? _heavyTransitionSFX
                    : _bossTransitionSFX != null ? _bossTransitionSFX
                    : _layerTransitionSFX;
            if (audio != null && sfx != null)
            {
                audio.PlaySFX2D(sfx);
            }

            // Phase 1: initial shake
            if (_heavyShakeIntensity > 0f)
            {
                ApplyCameraShake(_heavyShakeIntensity * 0.5f, 0.3f);
            }

            bool hasCameraSize = TryGetCurrentOrthoSize(out float baseSize);
            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize, baseSize + _heavyZoomOutAmount * 0.5f, _layerZoomDuration);
            }

            await UniTask.Delay(300, cancellationToken: token);

            // Phase 2: main shake + full zoom
            if (_heavyShakeIntensity > 0f)
            {
                ApplyCameraShake(_heavyShakeIntensity, 0.5f);
            }

            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize + _heavyZoomOutAmount * 0.5f, baseSize + _heavyZoomOutAmount, _layerZoomDuration);
            }

            await UniTask.Delay(400, cancellationToken: token);

            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Stop();
            }

            if (hasCameraSize)
            {
                TweenOrthoSize(baseSize + _heavyZoomOutAmount, baseSize, _layerZoomDuration * 1.5f);
            }
        }

        /// <summary>
        /// Apply a simple camera shake via Cinemachine impulse or fallback position offset.
        /// </summary>
        private void ApplyCameraShake(float intensity, float duration)
        {
            // Use CameraDirector if available (it may expose shake API in the future)
            // For now, use a simple PrimeTween position shake on the main camera
            var cam = Camera.main;
            if (cam == null) return;

            Tween.ShakeLocalPosition(cam.transform,
                new Vector3(intensity, intensity, 0f), duration, useUnscaledTime: true);
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
