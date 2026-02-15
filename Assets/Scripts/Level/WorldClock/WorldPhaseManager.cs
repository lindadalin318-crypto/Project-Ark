using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Evaluates the current world time phase based on WorldClock's normalized time.
    /// Holds an ordered list of WorldPhaseSO definitions and broadcasts phase transitions
    /// via LevelEvents.OnPhaseChanged.
    /// 
    /// ServiceLocator registered. Place on a persistent manager GameObject.
    /// </summary>
    public class WorldPhaseManager : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Phase Definitions")]
        [Tooltip("All phases for this planet's cycle, ordered. Time ranges should cover 0..1 without gaps.")]
        [SerializeField] private WorldPhaseSO[] _phases;

        // ──────────────────── Runtime State ────────────────────

        private int _currentPhaseIndex = -1;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Currently active phase SO (null if no phases configured). </summary>
        public WorldPhaseSO CurrentPhase =>
            (_phases != null && _currentPhaseIndex >= 0 && _currentPhaseIndex < _phases.Length)
                ? _phases[_currentPhaseIndex]
                : null;

        /// <summary> Index of the current phase (-1 if none). </summary>
        public int CurrentPhaseIndex => _currentPhaseIndex;

        /// <summary> All configured phases (read-only access for UI/Ambience). </summary>
        public WorldPhaseSO[] Phases => _phases;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);

            if (_phases == null || _phases.Length == 0)
            {
                Debug.LogWarning("[WorldPhaseManager] No phases configured.");
            }
        }

        private void Start()
        {
            LevelEvents.OnTimeChanged += HandleTimeChanged;

            // Evaluate initial phase immediately
            var clock = ServiceLocator.Get<WorldClock>();
            if (clock != null)
            {
                EvaluatePhase(clock.NormalizedTime, forceNotify: true);
            }
        }

        private void OnDestroy()
        {
            LevelEvents.OnTimeChanged -= HandleTimeChanged;
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleTimeChanged(float normalizedTime)
        {
            EvaluatePhase(normalizedTime, forceNotify: false);
        }

        // ──────────────────── Phase Evaluation ────────────────────

        private void EvaluatePhase(float normalizedTime, bool forceNotify)
        {
            if (_phases == null || _phases.Length == 0) return;

            int newPhaseIndex = FindPhaseIndex(normalizedTime);

            if (newPhaseIndex == _currentPhaseIndex && !forceNotify) return;

            int oldPhaseIndex = _currentPhaseIndex;
            _currentPhaseIndex = newPhaseIndex;

            if (newPhaseIndex >= 0 && newPhaseIndex < _phases.Length)
            {
                var phase = _phases[newPhaseIndex];
                string phaseName = phase != null ? phase.PhaseName : "Unknown";

                if (oldPhaseIndex != newPhaseIndex)
                {
                    string oldName = (oldPhaseIndex >= 0 && oldPhaseIndex < _phases.Length && _phases[oldPhaseIndex] != null)
                        ? _phases[oldPhaseIndex].PhaseName
                        : "None";
                    Debug.Log($"[WorldPhaseManager] Phase changed: {oldName} → {phaseName} (index {newPhaseIndex})");
                }

                LevelEvents.RaisePhaseChanged(newPhaseIndex, phaseName);
            }
        }

        private int FindPhaseIndex(float normalizedTime)
        {
            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i] != null && _phases[i].ContainsTime(normalizedTime))
                {
                    return i;
                }
            }

            // Fallback: if no phase matches, use the first one
            return _phases.Length > 0 ? 0 : -1;
        }

        // ──────────────────── Save/Load Support ────────────────────

        /// <summary>
        /// Force the phase to a specific index (used by save/load).
        /// Will broadcast OnPhaseChanged.
        /// </summary>
        public void SetPhaseIndex(int index)
        {
            if (_phases == null || index < 0 || index >= _phases.Length) return;

            _currentPhaseIndex = index;
            var phase = _phases[index];
            string phaseName = phase != null ? phase.PhaseName : "Unknown";
            LevelEvents.RaisePhaseChanged(index, phaseName);

            Debug.Log($"[WorldPhaseManager] Phase set to: {phaseName} (index {index})");
        }
    }
}
