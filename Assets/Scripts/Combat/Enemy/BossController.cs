using System;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Multi-phase boss controller. Monitors HP thresholds and triggers phase
    /// transitions with invulnerability windows, attack pattern swaps, and stat changes.
    /// Attach alongside EnemyEntity and any EnemyBrain subclass.
    /// Phases are defined by BossPhaseDataSO assets, sorted by HP threshold (descending).
    /// Builds on the existing AttackDataSO system from Phase 2.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(EnemyBrain))]
    public class BossController : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────
        [Header("Boss Phases")]
        [Tooltip("Array of phase definitions, should be ordered from highest HP threshold to lowest.")]
        [SerializeField] private BossPhaseDataSO[] _phases;

        // ──────────────────── Events ────────────────────

        /// <summary>
        /// Fired when the boss transitions to a new phase.
        /// Parameters: new phase index, phase data.
        /// </summary>
        public event Action<int, BossPhaseDataSO> OnPhaseChanged;

        // ──────────────────── Runtime State ────────────────────
        private EnemyEntity _entity;
        private EnemyBrain _brain;
        private SpriteRenderer _spriteRenderer;
        private int _currentPhaseIndex = -1; // -1 = initial (no phase triggered yet)
        private bool _isTransitioning;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Current phase index (0-based). -1 if no phase has been triggered. </summary>
        public int CurrentPhaseIndex => _currentPhaseIndex;

        /// <summary> Current phase data. Null if no phase triggered. </summary>
        public BossPhaseDataSO CurrentPhase =>
            _currentPhaseIndex >= 0 && _currentPhaseIndex < _phases.Length
                ? _phases[_currentPhaseIndex]
                : null;

        /// <summary> Whether the boss is currently in a phase transition (invulnerable). </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary> Total number of phases. </summary>
        public int PhaseCount => _phases != null ? _phases.Length : 0;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _brain = GetComponent<EnemyBrain>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (_entity != null)
                _entity.OnDamageTaken += CheckPhaseTransition;
        }

        private void OnDisable()
        {
            if (_entity != null)
                _entity.OnDamageTaken -= CheckPhaseTransition;
        }

        // ──────────────────── Phase Check ────────────────────

        private void CheckPhaseTransition(float damage, float currentHP)
        {
            if (_phases == null || _phases.Length == 0) return;
            if (_isTransitioning) return;
            if (!_entity.IsAlive) return;

            float hpRatio = currentHP / _entity.RuntimeMaxHP;

            // Check each phase (should be sorted descending by threshold)
            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i] == null) continue;

                // Skip phases we've already passed
                if (i <= _currentPhaseIndex) continue;

                // Check if HP ratio dropped below this phase's threshold
                if (hpRatio <= _phases[i].HPThresholdPercent)
                {
                    StartPhaseTransition(i);
                    return; // Only one phase change per damage event
                }
            }
        }

        // ──────────────────── Phase Transition ────────────────────

        private void StartPhaseTransition(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= _phases.Length) return;
            var phase = _phases[phaseIndex];

            _isTransitioning = true;
            _currentPhaseIndex = phaseIndex;

            // Make boss invulnerable during transition
            _entity.IsInvulnerable = true;

            // Force the brain into BossTransitionState
            _brain.ForceTransition(phase);

            // Apply stat modifiers via affix-like multipliers
            _entity.ApplyAffixMultipliers(1f, phase.DamageMultiplier, phase.SpeedMultiplier);

            // Swap attack patterns
            if (phase.PhaseAttacks != null && phase.PhaseAttacks.Length > 0 && _entity.Stats != null)
            {
                _entity.Stats.Attacks = phase.PhaseAttacks;
            }

            // Visual change
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = phase.PhaseColor;
            }

            // Spawn transition effect/adds
            if (phase.SpawnOnTransition != null)
            {
                // TODO: use object pool instead of Instantiate for production
                Instantiate(phase.SpawnOnTransition, transform.position, Quaternion.identity);
            }

            // Fire event
            OnPhaseChanged?.Invoke(phaseIndex, phase);

            // Schedule end of transition (handled by BossTransitionState via timer)
            // The BossTransitionState will call EndTransition() when its timer expires.
        }

        /// <summary>
        /// Called by BossTransitionState when the transition animation completes.
        /// Ends invulnerability and returns the boss to combat.
        /// </summary>
        public void EndTransition()
        {
            _isTransitioning = false;
            _entity.IsInvulnerable = false;
        }

        // ──────────────────── Pool Support ────────────────────

        /// <summary>
        /// Reset boss controller state. Called on pool return.
        /// </summary>
        public void ResetBoss()
        {
            _currentPhaseIndex = -1;
            _isTransitioning = false;

            if (_entity != null)
                _entity.IsInvulnerable = false;
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (_phases == null || _phases.Length == 0 || _entity == null || !_entity.IsAlive) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                string phaseInfo = _currentPhaseIndex >= 0 && CurrentPhase != null
                    ? $"Phase: {CurrentPhase.PhaseName}"
                    : "Phase: Initial";

                if (_isTransitioning)
                    phaseInfo += " [TRANSITIONING]";

                GUI.color = Color.cyan;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 55, 250, 20),
                          $"<BOSS: {phaseInfo}>");
            }
        }
#endif
    }
}
