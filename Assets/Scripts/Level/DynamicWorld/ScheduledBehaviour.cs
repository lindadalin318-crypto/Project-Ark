using System;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Generic time-driven component that enables/disables a target GameObject
    /// based on the current world time phase.
    /// 
    /// Attach to any GameObject. Configure which phase indices should activate the target.
    /// Use cases: NPC trading windows, timed doors, hidden passages, enemy night-mode buff.
    /// </summary>
    public class ScheduledBehaviour : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Schedule")]
        [Tooltip("Phase indices during which the target is ACTIVE. Outside these phases it will be disabled.")]
        [SerializeField] private int[] _activePhaseIndices;

        [Header("Target")]
        [Tooltip("The GameObject to enable/disable. If null, this GameObject itself is toggled (children only — this component stays active to receive events).")]
        [SerializeField] private GameObject _targetGameObject;

        [Header("Options")]
        [Tooltip("If true, inverts the logic: target is DISABLED during active phases and ENABLED otherwise.")]
        [SerializeField] private bool _invertLogic;

        // ──────────────────── Runtime State ────────────────────

        private bool _initialized;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            LevelEvents.OnPhaseChanged += HandlePhaseChanged;

            // Apply initial state from current phase
            var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
            if (phaseManager != null && phaseManager.CurrentPhaseIndex >= 0)
            {
                ApplyState(phaseManager.CurrentPhaseIndex);
            }

            _initialized = true;
        }

        private void OnDestroy()
        {
            LevelEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandlePhaseChanged(int phaseIndex, string phaseName)
        {
            ApplyState(phaseIndex);
        }

        // ──────────────────── Logic ────────────────────

        private void ApplyState(int currentPhaseIndex)
        {
            bool isActivePhase = IsActivePhase(currentPhaseIndex);
            bool shouldBeActive = _invertLogic ? !isActivePhase : isActivePhase;

            if (_targetGameObject != null)
            {
                _targetGameObject.SetActive(shouldBeActive);
            }
            else
            {
                // Toggle children instead of self (keep this MonoBehaviour alive)
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(shouldBeActive);
                }
            }
        }

        private bool IsActivePhase(int phaseIndex)
        {
            if (_activePhaseIndices == null || _activePhaseIndices.Length == 0)
                return false;

            for (int i = 0; i < _activePhaseIndices.Length; i++)
            {
                if (_activePhaseIndices[i] == phaseIndex)
                    return true;
            }

            return false;
        }
    }
}
