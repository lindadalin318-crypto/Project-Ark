using TMPro;
using UnityEngine;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Bottom status bar that shows transient notification messages.
    /// Messages fade in (150ms) then fade out after a configurable duration.
    /// New messages immediately interrupt any ongoing animation.
    /// </summary>
    public class StatusBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        /// <summary> Default idle text shown when no notification is active. </summary>
        [SerializeField] private string _idleText = "DRAG TO EQUIP  ·  CLICK TO INSPECT";

        private Sequence _animSequence;

        private void Awake()
        {
            ShowIdle();
        }

        /// <summary>
        /// Display a notification message with the given color.
        /// Fades in over 150ms, holds for <paramref name="duration"/> seconds, then fades out.
        /// </summary>
        public void ShowMessage(string text, Color color, float duration = 3f)
        {
            if (_label == null) return;

            // Interrupt any ongoing animation
            _animSequence.Stop();

            _label.text = text;

            // Start from alpha 0, fade in → hold → fade out
            var startColor = new Color(color.r, color.g, color.b, 0f);
            _label.color = startColor;

            _animSequence = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Color(_label, endValue: color, duration: 0.15f,
                    ease: Ease.OutQuad, useUnscaledTime: true))
                .ChainDelay(duration)
                .Chain(Tween.Color(_label, endValue: new Color(color.r, color.g, color.b, 0f),
                    duration: 0.5f, ease: Ease.InQuad, useUnscaledTime: true))
                .ChainCallback(ShowIdle);
        }

        /// <summary> Restore idle state immediately. </summary>
        public void ShowIdle()
        {
            if (_label == null) return;
            _animSequence.Stop();
            _label.text = _idleText;
            _label.color = StarChartTheme.StatusIdle;
        }

        /// <summary>
        /// Set text and color instantly with no animation.
        /// Use this for persistent status text (e.g. equipped count) where
        /// a fade-in / fade-out cycle is not desired and a zero-duration
        /// tween would trigger a PrimeTween warning.
        /// </summary>
        public void SetText(string text, Color color)
        {
            if (_label == null) return;
            _animSequence.Stop();
            _label.text = text;
            _label.color = color;
        }

        private void OnDestroy()
        {
            _animSequence.Stop();
        }
    }
}
