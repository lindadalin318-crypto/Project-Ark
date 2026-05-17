using System;
using ProjectArk.Core;
using UnityEngine;

namespace ProjectArk.HyperWind
{
    /// <summary>
    /// MVP controller for HyperWind global phase rhythm: weak wind, two warning windows, then strong wind.
    /// </summary>
    public sealed class WindPhaseController : MonoBehaviour, IWindPhaseService
    {
        [Header("Cycle")]
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] [Min(1f)] private float _cycleDuration = 60f;
        [SerializeField] [Min(0.1f)] private float _strongDuration = 10f;

        [Header("Warnings")]
        [SerializeField] [Min(0f)] private float _sandWarningLeadTime = 5f;
        [SerializeField] [Min(0f)] private float _audioWarningLeadTime = 3f;

        [Header("Wind Strength")]
        [SerializeField] [Min(0f)] private float _weakMultiplier = 0.65f;
        [SerializeField] [Min(0f)] private float _warningMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _strongMultiplier = 1.6f;

        private float _elapsed;
        private WindPhaseState _currentState = WindPhaseState.Weak;

        public event Action<WindPhaseState, WindPhaseState> OnPhaseStateChanged;

        public WindPhaseState CurrentState => _currentState;
        public float Cycle01 => _cycleDuration > 0.001f ? Mathf.Repeat(_elapsed, _cycleDuration) / _cycleDuration : 0f;
        public bool IsStrong => _currentState == WindPhaseState.Strong;

        public float CurrentWindMultiplier => _currentState switch
        {
            WindPhaseState.Strong => _strongMultiplier,
            WindPhaseState.AudioWarning => _warningMultiplier,
            WindPhaseState.SandWarning => _warningMultiplier,
            _ => _weakMultiplier
        };

        private void Awake()
        {
            ServiceLocator.Register<IWindPhaseService>(this);
            EvaluateState(forceEvent: true);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IWindPhaseService>(this);
            OnPhaseStateChanged = null;
        }

        private void Update()
        {
            if (!_autoPlay)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            EvaluateState(forceEvent: false);
        }

        public void SetElapsedTime(float elapsedSeconds)
        {
            _elapsed = Mathf.Max(0f, elapsedSeconds);
            EvaluateState(forceEvent: true);
        }

        private void EvaluateState(bool forceEvent)
        {
            WindPhaseState previous = _currentState;
            _currentState = ResolveState(Mathf.Repeat(_elapsed, _cycleDuration));

            if (forceEvent || previous != _currentState)
            {
                OnPhaseStateChanged?.Invoke(previous, _currentState);
            }
        }

        private WindPhaseState ResolveState(float timeInCycle)
        {
            float strongStart = Mathf.Max(0f, _cycleDuration - _strongDuration);
            if (timeInCycle >= strongStart)
            {
                return WindPhaseState.Strong;
            }

            float audioWarningStart = Mathf.Max(0f, strongStart - _audioWarningLeadTime);
            if (timeInCycle >= audioWarningStart)
            {
                return WindPhaseState.AudioWarning;
            }

            float sandWarningStart = Mathf.Max(0f, strongStart - _sandWarningLeadTime);
            if (timeInCycle >= sandWarningStart)
            {
                return WindPhaseState.SandWarning;
            }

            return WindPhaseState.Weak;
        }

        private void OnValidate()
        {
            _cycleDuration = Mathf.Max(1f, _cycleDuration);
            _strongDuration = Mathf.Clamp(_strongDuration, 0.1f, _cycleDuration);
            _sandWarningLeadTime = Mathf.Clamp(_sandWarningLeadTime, 0f, _cycleDuration - _strongDuration);
            _audioWarningLeadTime = Mathf.Clamp(_audioWarningLeadTime, 0f, _sandWarningLeadTime);
            _weakMultiplier = Mathf.Max(0f, _weakMultiplier);
            _warningMultiplier = Mathf.Max(0f, _warningMultiplier);
            _strongMultiplier = Mathf.Max(0f, _strongMultiplier);
        }
    }
}
