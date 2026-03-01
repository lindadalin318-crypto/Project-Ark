using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Attaches to the Header StatusDot Image and drives a looping alpha pulse animation.
    /// Uses unscaled time so it keeps running while the game is paused.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class StatusDotPulse : MonoBehaviour
    {
        private Tween _pulseTween;

        private void OnEnable()
        {
            var img = GetComponent<Image>();
            if (img == null) return;

            // Pulse: alpha 1.0 → 0.3 → 1.0, 1.2s cycle, looping
            _pulseTween = Tween.Alpha(img, startValue: 1f, endValue: 0.3f,
                duration: 0.6f, ease: Ease.InOutSine, useUnscaledTime: true,
                cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        private void OnDisable()
        {
            _pulseTween.Stop();
        }
    }
}
