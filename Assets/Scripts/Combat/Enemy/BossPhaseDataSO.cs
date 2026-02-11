using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Data definition for a single boss phase.
    /// A boss has multiple phases, each triggered at an HP threshold.
    /// Each phase can change attack patterns, apply stat modifiers, and spawn effects.
    /// Create via: Create > ProjectArk > Enemy > BossPhaseData
    /// </summary>
    [CreateAssetMenu(fileName = "BossPhase_New", menuName = "ProjectArk/Enemy/BossPhaseData", order = 3)]
    public class BossPhaseDataSO : ScriptableObject
    {
        // ──────────────────── Identity ────────────────────
        [Header("Identity")]
        [Tooltip("Display name for this phase (e.g. 'Phase 1: Calm', 'Phase 2: Enraged').")]
        public string PhaseName = "New Phase";

        // ──────────────────── Trigger ────────────────────
        [Header("Trigger")]
        [Tooltip("HP threshold ratio to trigger this phase (0.0-1.0). E.g., 0.5 = triggers at 50% HP.")]
        [Range(0f, 1f)]
        public float HPThresholdPercent = 0.5f;

        // ──────────────────── Attack Pattern ────────────────────
        [Header("Attack Pattern")]
        [Tooltip("Attack patterns available in this phase. Replaces the current Attacks array on the EnemyStatsSO.")]
        public AttackDataSO[] PhaseAttacks;

        // ──────────────────── Stat Modifiers ────────────────────
        [Header("Stat Modifiers")]
        [Tooltip("Damage multiplier for this phase (1.0 = base damage).")]
        [Min(0.1f)]
        public float DamageMultiplier = 1f;

        [Tooltip("Speed multiplier for this phase (1.0 = base speed).")]
        [Min(0.1f)]
        public float SpeedMultiplier = 1f;

        // ──────────────────── Visuals ────────────────────
        [Header("Visuals")]
        [Tooltip("Color tint for this phase. Used during transition flash and as base color.")]
        public Color PhaseColor = Color.white;

        // ──────────────────── Phase Transition ────────────────────
        [Header("Phase Transition")]
        [Tooltip("Prefab to spawn during phase transition (adds, VFX, shockwave). Null = none.")]
        public GameObject SpawnOnTransition;

        [Tooltip("Duration of the invulnerable transition window (seconds).")]
        [Min(0.1f)]
        public float TransitionDuration = 1.5f;
    }
}
