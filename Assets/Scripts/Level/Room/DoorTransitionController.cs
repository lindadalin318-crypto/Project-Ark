using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
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

                // ── 3. Teleport player ──
                var ship = inputHandler != null ? inputHandler.transform : null;
                if (ship != null && door.TargetSpawnPoint != null)
                {
                    ship.position = door.TargetSpawnPoint.position;
                }

                // ── 4. Switch room ──
                var roomManager = ServiceLocator.Get<RoomManager>();
                if (roomManager != null && door.TargetRoom != null)
                {
                    roomManager.EnterRoom(door.TargetRoom);
                }

                // Brief pause at full black for clean transition
                await UniTask.Delay(50, cancellationToken: token);

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
