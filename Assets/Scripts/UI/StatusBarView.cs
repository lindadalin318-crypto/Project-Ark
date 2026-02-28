using TMPro;
using UnityEngine;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Bottom status bar that shows transient notification messages.
    /// Messages fade out after a configurable duration.
    /// New messages immediately interrupt any ongoing fade.
    /// </summary>
    public class StatusBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        /// <summary> Default idle text shown when no notification is active. </summary>
        [SerializeField] private string _idleText = "DRAG TO EQUIP  ·  CLICK TO INSPECT";

        private Tween _fadeTween;

        private void Awake()
        {
            ShowIdle();
        }

        /// <summary>
        /// Display a notification message with the given color.
        /// The message fades out after <paramref name="duration"/> seconds.
        /// </summary>
        public void ShowMessage(string text, Color color, float duration = 3f)
        {
            if (_label == null) return;

            // Interrupt any ongoing fade
            _fadeTween.Stop();

            _label.text = text;
            _label.color = color;

            // Hold for duration, then fade alpha to 0 over 0.5s
            _fadeTween = Sequence.Create()
                .ChainDelay(duration)
                .Chain(Tween.Alpha(_label, endValue: 0f, duration: 0.5f, useUnscaledTime: true))
                .ChainCallback(ShowIdle);
        }

        /// <summary> Restore idle state immediately. </summary>
        public void ShowIdle()
        {
            if (_label == null) return;
            _fadeTween.Stop();
            _label.text = _idleText;
            _label.color = StarChartTheme.StatusIdle;
        }

        private void OnDestroy()
        {
            _fadeTween.Stop();
        }
    }
}
