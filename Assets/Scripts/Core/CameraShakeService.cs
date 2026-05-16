using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Lightweight camera shake service for short combat impact feedback.
    /// Attach it to the camera rig or gameplay camera transform and call Shake()
    /// from combat presentation code. The service uses unscaled time so it still
    /// completes during hit-stop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShakeService : MonoBehaviour
    {
        private Vector3 _baseLocalPosition;
        private float _remainingDuration;
        private float _duration;
        private float _amplitude;
        private float _frequency;
        private float _elapsed;

        /// <summary>Whether a shake request is currently active.</summary>
        public bool IsShaking => _remainingDuration > 0f;

        /// <summary>Remaining shake time in real-time seconds.</summary>
        public float RemainingDuration => _remainingDuration;

        /// <summary>Current shake amplitude in local units.</summary>
        public float CurrentAmplitude => _amplitude;

        /// <summary>Current shake frequency in cycles per second.</summary>
        public float CurrentFrequency => _frequency;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
            ServiceLocator.Register(this);
        }

        private void LateUpdate()
        {
            Step(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            StopShake();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        /// <summary>
        /// Starts or refreshes a short camera shake. Overlapping calls keep the strongest
        /// amplitude/frequency and extend to the longest remaining duration.
        /// </summary>
        /// <param name="duration">Shake duration in real-time seconds.</param>
        /// <param name="amplitude">Local-space offset amplitude.</param>
        /// <param name="frequency">Oscillation frequency in cycles per second.</param>
        public void Shake(float duration, float amplitude, float frequency)
        {
            if (duration <= 0f)
                return;

            _baseLocalPosition = IsShaking ? _baseLocalPosition : transform.localPosition;
            _duration = Mathf.Max(_duration, duration);
            _remainingDuration = Mathf.Max(_remainingDuration, duration);
            _amplitude = Mathf.Max(_amplitude, amplitude, 0f);
            _frequency = Mathf.Max(_frequency, frequency, 0f);
            _elapsed = 0f;
        }

        /// <summary>
        /// Advances the shake by a real-time delta. Exposed for deterministic tests and
        /// for callers that need manual ticking.
        /// </summary>
        public void Step(float unscaledDeltaTime)
        {
            if (!IsShaking)
                return;

            float deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            _elapsed += deltaTime;
            _remainingDuration = Mathf.Max(0f, _remainingDuration - deltaTime);

            if (_remainingDuration <= 0f)
            {
                StopShake();
                return;
            }

            float normalizedTime = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
            float damping = 1f - normalizedTime;
            float phase = _elapsed * _frequency * Mathf.PI * 2f;
            float x = Mathf.Sin(phase) * _amplitude * damping;
            float y = Mathf.Cos(phase * 1.37f) * _amplitude * 0.65f * damping;
            transform.localPosition = _baseLocalPosition + new Vector3(x, y, 0f);
        }

        private void StopShake()
        {
            _remainingDuration = 0f;
            _duration = 0f;
            _amplitude = 0f;
            _frequency = 0f;
            _elapsed = 0f;
            transform.localPosition = _baseLocalPosition;
        }
    }
}
