using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class TransitionUI : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private Image _fadeOverlay;
        [SerializeField] private float _fadeDuration = 0.3f;

        [Header("Text")]
        [SerializeField] private Text _centerText;
        [SerializeField] private float _typewriterSpeed = 0.05f;
        [SerializeField] private float _textDisplayDuration = 1.5f;

        private CancellationTokenSource _transitionCts;

        private void Awake()
        {
            ServiceLocator.Register(this);

            if (_fadeOverlay != null)
            {
                _fadeOverlay.gameObject.SetActive(true);
                SetFadeAlpha(0f);
            }

            if (_centerText != null)
            {
                _centerText.gameObject.SetActive(false);
            }
        }

        public async UniTask PlayEnterTransitionAsync(string text = "进入飞船...")
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                await PlayTransitionAsync(text, _transitionCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async UniTask PlayExitTransitionAsync(string text = "准备出击")
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                await PlayTransitionAsync(text, _transitionCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask PlayTransitionAsync(string text, CancellationToken ct)
        {
            if (_centerText != null)
            {
                _centerText.gameObject.SetActive(true);
                _centerText.text = "";
                await TypeTextAsync(text, ct);
                await UniTask.Delay(System.TimeSpan.FromSeconds(_textDisplayDuration), cancellationToken: ct);
                _centerText.gameObject.SetActive(false);
            }

            if (_fadeOverlay != null)
            {
                await FadeOutAsync(_fadeDuration, ct);
                await FadeInAsync(_fadeDuration, ct);
            }
        }

        private async UniTask TypeTextAsync(string text, CancellationToken ct)
        {
            if (_centerText == null) return;

            _centerText.text = "";

            foreach (char c in text)
            {
                if (ct.IsCancellationRequested) return;

                _centerText.text += c;
                await UniTask.Delay(System.TimeSpan.FromSeconds(_typewriterSpeed), cancellationToken: ct);
            }
        }

        public async UniTask FadeInAsync(float duration, CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;

            float startAlpha = _fadeOverlay.color.a;
            var tween = Tween.Custom(startAlpha, 0f, duration, useUnscaledTime: true,
                onValueChange: v => SetFadeAlpha(v), ease: Ease.Linear);

            try
            {
                await tween.ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                if (tween.isAlive) tween.Stop();
                throw;
            }
        }

        public async UniTask FadeOutAsync(float duration, CancellationToken ct = default)
        {
            if (_fadeOverlay == null) return;

            float startAlpha = _fadeOverlay.color.a;
            var tween = Tween.Custom(startAlpha, 1f, duration, useUnscaledTime: true,
                onValueChange: v => SetFadeAlpha(v), ease: Ease.Linear);

            try
            {
                await tween.ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
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

        private void CancelTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }

        private void OnDestroy()
        {
            CancelTransition();
            ServiceLocator.Unregister(this);
        }
    }
}
