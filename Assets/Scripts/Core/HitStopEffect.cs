using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Global hit-stop (顿帧) effect. Briefly freezes Time.timeScale on significant
    /// combat events (poise break, killing blow) to add weight and impact.
    /// Auto-creates a persistent singleton via [RuntimeInitializeOnLoadMethod].
    /// 
    /// Usage: HitStopEffect.Trigger(0.06f);
    /// 
    /// Safety:
    ///   - Skips if timeScale is already near-zero (e.g. star chart panel open).
    ///   - Overlapping triggers extend rather than stack.
    ///   - Uses Time.unscaledDeltaTime so the timer runs regardless of timeScale.
    /// </summary>
    public class HitStopEffect : MonoBehaviour
    {
        private static HitStopEffect _instance;

        private float _remainingFreeze;
        private float _savedTimeScale = 1f;
        private bool _isFreezing;

        // ──────────────────── Auto Bootstrap ────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null) return;

            var go = new GameObject("[HitStop]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HitStopEffect>();
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Trigger a hit-stop freeze for the given duration (in real-time seconds).
        /// If already freezing, extends the remaining time (no stacking).
        /// </summary>
        /// <param name="duration">Freeze duration in real-time seconds (typical: 0.03–0.10).</param>
        public static void Trigger(float duration)
        {
            if (_instance == null) return;
            _instance.DoFreeze(duration);
        }

        // ──────────────────── Internal ────────────────────

        private void DoFreeze(float duration)
        {
            if (duration <= 0f) return;

            // If timeScale is already near-zero (star chart / pause menu), skip freeze
            if (!_isFreezing && Time.timeScale < 0.01f) return;

            if (_isFreezing)
            {
                // Already frozen — just extend the timer
                _remainingFreeze = Mathf.Max(_remainingFreeze, duration);
                return;
            }

            // Start a new freeze
            _savedTimeScale = Time.timeScale;
            _remainingFreeze = duration;
            _isFreezing = true;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!_isFreezing) return;

            _remainingFreeze -= Time.unscaledDeltaTime;

            if (_remainingFreeze <= 0f)
            {
                // Restore the saved timeScale
                Time.timeScale = _savedTimeScale;
                _isFreezing = false;
                _remainingFreeze = 0f;
            }
        }

        private void OnDestroy()
        {
            // Safety: if somehow destroyed while freezing, restore timeScale
            if (_isFreezing)
            {
                Time.timeScale = _savedTimeScale;
                _isFreezing = false;
            }

            if (_instance == this)
                _instance = null;
        }
    }
}
