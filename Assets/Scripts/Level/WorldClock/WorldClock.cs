using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// In-game world clock driving the planet's day/night cycle.
    /// Tracks elapsed time within a configurable cycle (e.g., 20 min real-time = one planet rotation).
    /// Broadcasts normalized time every frame so WorldPhaseManager and other consumers can react.
    /// 
    /// ServiceLocator registered. Place on a persistent manager GameObject.
    /// </summary>
    public class WorldClock : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Cycle")]
        [Tooltip("Total duration of one full cycle in seconds (real-time). Default 1200s = 20 minutes.")]
        [SerializeField] private float _cycleDuration = 1200f;

        [Tooltip("Time speed multiplier. 1 = real-time, 2 = double speed, 0.5 = half speed.")]
        [SerializeField] private float _timeSpeed = 1f;

        [Header("Initial State")]
        [Tooltip("Starting time as a normalized value (0..1). 0 = cycle start, 0.5 = midpoint.")]
        [SerializeField] private float _startTimeNormalized = 0f;

        // ──────────────────── Runtime State ────────────────────

        private float _currentTime;   // seconds elapsed in current cycle (0.._cycleDuration)
        private int _cycleCount;      // how many full cycles completed
        private bool _isPaused;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Current time in seconds within the cycle (0..CycleDuration). </summary>
        public float CurrentTime => _currentTime;

        /// <summary> Current time as normalized value (0..1). </summary>
        public float NormalizedTime => _cycleDuration > 0f ? _currentTime / _cycleDuration : 0f;

        /// <summary> Number of completed full cycles. </summary>
        public int CycleCount => _cycleCount;

        /// <summary> Total cycle duration in seconds. </summary>
        public float CycleDuration => _cycleDuration;

        /// <summary> Current time speed multiplier. </summary>
        public float TimeSpeed => _timeSpeed;

        /// <summary> Whether the clock is paused. </summary>
        public bool IsPaused => _isPaused;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
            _currentTime = Mathf.Clamp01(_startTimeNormalized) * _cycleDuration;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        private void Update()
        {
            if (_isPaused) return;
            if (_cycleDuration <= 0f) return;

            float previousNormalized = NormalizedTime;

            _currentTime += Time.deltaTime * _timeSpeed;

            // Cycle wrap
            if (_currentTime >= _cycleDuration)
            {
                _currentTime -= _cycleDuration;
                _cycleCount++;
                LevelEvents.RaiseCycleCompleted(_cycleCount);
            }

            LevelEvents.RaiseTimeChanged(NormalizedTime);
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Set the clock to a specific normalized time (0..1).
        /// </summary>
        public void SetTime(float normalizedTime)
        {
            _currentTime = Mathf.Clamp01(normalizedTime) * _cycleDuration;
            LevelEvents.RaiseTimeChanged(NormalizedTime);
        }

        /// <summary>
        /// Set the clock to a specific time in seconds and cycle count.
        /// Used by save/load.
        /// </summary>
        public void SetTimeExact(float timeSeconds, int cycleCount)
        {
            _currentTime = Mathf.Clamp(timeSeconds, 0f, _cycleDuration);
            _cycleCount = Mathf.Max(0, cycleCount);
            LevelEvents.RaiseTimeChanged(NormalizedTime);
        }

        /// <summary> Pause the clock. </summary>
        public void Pause()
        {
            _isPaused = true;
            Debug.Log("[WorldClock] Paused.");
        }

        /// <summary> Resume the clock. </summary>
        public void Resume()
        {
            _isPaused = false;
            Debug.Log("[WorldClock] Resumed.");
        }

        /// <summary>
        /// Set the time speed multiplier.
        /// </summary>
        public void SetSpeed(float speed)
        {
            _timeSpeed = Mathf.Max(0f, speed);
        }
    }
}
