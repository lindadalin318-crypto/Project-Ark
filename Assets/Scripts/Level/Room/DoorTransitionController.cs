using System;
using System.Threading;
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
    /// 
    /// Place on a persistent Canvas GameObject with a full-screen Image child.
    /// Registers to ServiceLocator so Door components can access it.
    /// </summary>
    public class DoorTransitionController : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

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

        [Tooltip("Camera zoom-out amount during layer transition (added to base ortho size).")]
        [SerializeField] private float _layerZoomOutAmount = 2f;

        [Tooltip("Camera zoom transition duration.")]
        [SerializeField] private float _layerZoomDuration = 0.3f;

        // ──────────────────── Runtime State ────────────────────

        private CancellationTokenSource _transitionCts;
        private bool _isTransitioning;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> True while a door transition is in progress. </summary>
        public bool IsTransitioning => _isTransitioning;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);

            // Start fully transparent
            if (_fadeImage != null)
            {
                _fadeImage.color = new Color(0f, 0f, 0f, 0f);
                _fadeImage.raycastTarget = false;
            }
            else
            {
                Debug.LogError("[DoorTransitionController] Fade Image is not assigned!");
            }
        }

        private void OnDestroy()
        {
            CancelTransition();
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Execute a door transition: fade out → teleport → room change → fade in.
        /// </summary>
        /// <param name="door">The door to transition through.</param>
        /// <param name="onComplete">Optional callback when transition finishes.</param>
        public void TransitionThroughDoor(Door door, Action onComplete = null)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[DoorTransitionController] Transition already in progress. Ignoring.");
                return;
            }

            ExecuteTransition(door, onComplete).Forget();
        }

        // ──────────────────── Transition Logic ────────────────────

        private async UniTaskVoid ExecuteTransition(Door door, Action onComplete)
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _transitionCts.Token, destroyCancellationToken);
            var token = linkedCts.Token;

            _isTransitioning = true;

            try
            {
                float fadeDuration = door.IsLayerTransition
                    ? _layerFadeDuration
                    : _normalFadeDuration;

                // ── 1. Disable player input ──
                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                    inputHandler.enabled = false;

                // Block raycasts during fade
                if (_fadeImage != null)
                    _fadeImage.raycastTarget = true;

                // ── 2. Fade to black ──
                _ = Tween.Custom(0f, 1f, fadeDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_fadeImage != null)
                            _fadeImage.color = new Color(0f, 0f, 0f, v);
                    },
                    ease: Ease.InQuad);

                int fadeMs = Mathf.RoundToInt(fadeDuration * 1000f);
                await UniTask.Delay(fadeMs, cancellationToken: token);

                // ── 3. Layer transition effects (during black screen) ──
                if (door.IsLayerTransition)
                {
                    await PlayLayerTransitionEffects(door, token);
                }

                // ── 4. Teleport player ──
                var ship = inputHandler != null ? inputHandler.transform : null;
                if (ship != null && door.TargetSpawnPoint != null)
                {
                    ship.position = door.TargetSpawnPoint.position;
                }

                // ── 5. Switch room ──
                var roomManager = ServiceLocator.Get<RoomManager>();
                if (roomManager != null && door.TargetRoom != null)
                {
                    roomManager.EnterRoom(door.TargetRoom);

                    // BGM crossfade if target room has different ambient music
                    HandleBGMCrossfade(door.TargetRoom);
                }

                // Brief pause at full black for clean transition
                await UniTask.Delay(door.IsLayerTransition ? 200 : 50, cancellationToken: token);

                // ── 5. Fade from black ──
                _ = Tween.Custom(1f, 0f, fadeDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_fadeImage != null)
                            _fadeImage.color = new Color(0f, 0f, 0f, v);
                    },
                    ease: Ease.OutQuad);

                await UniTask.Delay(fadeMs, cancellationToken: token);

                // ── 6. Re-enable player input ──
                if (inputHandler != null)
                    inputHandler.enabled = true;

                if (_fadeImage != null)
                    _fadeImage.raycastTarget = false;
            }
            catch (OperationCanceledException)
            {
                // Transition was cancelled — ensure clean state
                if (_fadeImage != null)
                {
                    _fadeImage.color = new Color(0f, 0f, 0f, 0f);
                    _fadeImage.raycastTarget = false;
                }

                var inputHandler = ServiceLocator.Get<InputHandler>();
                if (inputHandler != null)
                    inputHandler.enabled = true;
            }
            finally
            {
                _isTransitioning = false;
                linkedCts.Dispose();
                onComplete?.Invoke();
            }
        }

        // ──────────────────── Layer Transition Effects ────────────────────

        private async UniTask PlayLayerTransitionEffects(Door door, CancellationToken token)
        {
            // Determine direction (descending if target floor < current floor)
            var roomManager = ServiceLocator.Get<RoomManager>();
            int currentFloor = roomManager?.CurrentFloor ?? 0;
            int targetFloor = door.TargetRoom?.Data?.FloorLevel ?? 0;
            bool descending = targetFloor < currentFloor;

            // ── Particle effect ──
            if (_layerTransitionParticles != null)
            {
                // Flip particle direction based on ascending/descending
                var mainModule = _layerTransitionParticles.main;
                float speed = Mathf.Abs(mainModule.startSpeed.constant);
                mainModule.startSpeed = descending ? -speed : speed;

                _layerTransitionParticles.Play();
            }

            // ── SFX ──
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null && _layerTransitionSFX != null)
            {
                audio.PlaySFX2D(_layerTransitionSFX);
            }

            // ── Camera zoom-out ──
            var cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                float baseSize = cam.orthographicSize;
                float targetSize = baseSize + _layerZoomOutAmount;

                _ = Tween.Custom(baseSize, targetSize, _layerZoomDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (cam != null)
                            cam.orthographicSize = v;
                    },
                    ease: Ease.InOutSine);
            }

            // Hold for particles/effect duration
            await UniTask.Delay(300, cancellationToken: token);

            // ── Stop particles ──
            if (_layerTransitionParticles != null)
            {
                _layerTransitionParticles.Stop();
            }

            // ── Camera snap back (will happen during fade-in) ──
            if (cam != null && cam.orthographic)
            {
                float currentSize = cam.orthographicSize;
                float baseSize = currentSize - _layerZoomOutAmount;

                _ = Tween.Custom(currentSize, baseSize, _layerZoomDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (cam != null)
                            cam.orthographicSize = v;
                    },
                    ease: Ease.InOutSine);
            }
        }

        private void HandleBGMCrossfade(Room targetRoom)
        {
            if (targetRoom?.Data?.AmbientMusic == null) return;

            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null)
            {
                audio.PlayMusic(targetRoom.Data.AmbientMusic, _bgmCrossfadeDuration);
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
