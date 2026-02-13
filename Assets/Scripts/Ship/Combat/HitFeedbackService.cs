using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Static service for hit-stop (time freeze) and screen-shake feedback.
    /// HitStop uses Time.timeScale manipulation.
    /// ScreenShake uses Cinemachine CinemachineImpulseSource if available.
    /// </summary>
    public static class HitFeedbackService
    {
        // ══════════════════════════════════════════════════════════════
        // Hit Stop
        // ══════════════════════════════════════════════════════════════

        private static bool _isHitStopping;

        /// <summary>
        /// Freezes time for the given duration then restores it.
        /// Uses ignoreTimeScale so the delay works at timeScale = 0.
        /// </summary>
        public static async UniTaskVoid TriggerHitStop(float duration)
        {
            if (duration <= 0f) return;
            if (_isHitStopping) return; // Prevent stacking

            _isHitStopping = true;
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            int delayMs = Mathf.RoundToInt(duration * 1000f);
            await UniTask.Delay(delayMs, ignoreTimeScale: true);

            // Only restore if we still own the freeze
            if (_isHitStopping)
            {
                Time.timeScale = previousTimeScale;
                _isHitStopping = false;
            }
        }

        /// <summary>
        /// Force-end any active hit-stop. Useful for scene transitions.
        /// </summary>
        public static void CancelHitStop()
        {
            if (_isHitStopping)
            {
                Time.timeScale = 1f;
                _isHitStopping = false;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Screen Shake (Cinemachine Impulse)
        // ══════════════════════════════════════════════════════════════

        private static Unity.Cinemachine.CinemachineImpulseSource _impulseSource;

        /// <summary>
        /// Registers a CinemachineImpulseSource for screen shake.
        /// Call once during setup (e.g., from a MonoBehaviour on the camera rig).
        /// </summary>
        public static void RegisterImpulseSource(Unity.Cinemachine.CinemachineImpulseSource source)
        {
            _impulseSource = source;
        }

        /// <summary>
        /// Triggers screen shake via Cinemachine impulse.
        /// Requires a registered impulse source.
        /// </summary>
        public static void TriggerScreenShake(float intensity)
        {
            if (intensity <= 0f) return;

            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulse(intensity);
            }
            else
            {
                Debug.LogWarning("[HitFeedbackService] No CinemachineImpulseSource registered. Screen shake skipped.");
            }
        }
    }
}
