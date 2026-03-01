using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    /// <summary>
    /// Handles full-screen fade-to-black transitions.
    /// Intentionally kept minimal: only fade in/out, no typewriter text.
    /// </summary>
    public class TransitionUI : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private Image _fadeOverlay;
        [SerializeField] private float _fadeDuration = 0.15f;

        private CancellationTokenSource _transitionCts;

        private void Awake()
        {
            ServiceLocator.Register(this);

            if (_fadeOverlay != null)
            {
                _fadeOverlay.gameObject.SetActive(true);
                SetFadeAlpha(0f);
            }
        }

        /// <summary>Fades screen to black (alpha 0 → 1).</summary>
        public async UniTask FadeOutAsync(CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;

            float startAlpha = _fadeOverlay.color.a;
            var tween = Tween.Custom(startAlpha, 1f, _fadeDuration, useUnscaledTime: true,
                onValueChange: v => SetFadeAlpha(v), ease: Ease.Linear);

            try
            {
                await tween.ToUniTask(cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                if (tween.isAlive) tween.Stop();
                throw;
            }
        }

        /// <summary>Fades screen from black back to clear (alpha 1 → 0).</summary>
        public async UniTask FadeInAsync(CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;

            float startAlpha = _fadeOverlay.color.a;
            var tween = Tween.Custom(startAlpha, 0f, _fadeDuration, useUnscaledTime: true,
                onValueChange: v => SetFadeAlpha(v), ease: Ease.Linear);

            try
            {
                await tween.ToUniTask(cancellationToken: ct);
            }
            catch (System.OperationCanceledException)
            {
                if (tween.isAlive) tween.Stop();
                throw;
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeOverlay == null) return;
            var color = _fadeOverlay.color;
            color.a = alpha;
            _fadeOverlay.color = color;
        }

        private void OnDestroy()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
            ServiceLocator.Unregister(this);
        }
    }
}
