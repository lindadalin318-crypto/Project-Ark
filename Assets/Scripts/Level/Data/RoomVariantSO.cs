using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Defines a room variant that activates during specific world time phases.
    /// When active, overrides the room's default encounter and/or environment.
    /// 
    /// Example: Daytime variant has 3 merchant NPCs + 2 guards.
    ///          Nighttime variant replaces them with 5 predator enemies.
    /// </summary>
    [CreateAssetMenu(fileName = "New Room Variant", menuName = "ProjectArk/Level/Room Variant")]
    public class RoomVariantSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable variant name (e.g., 'Night Configuration', 'Storm Hazard').")]
        [SerializeField] private string _variantName;

        [Header("Activation")]
        [Tooltip("World phase indices during which this variant is active. Empty = never auto-activate.")]
        [SerializeField] private int[] _activePhaseIndices;

        [Header("Overrides")]
        [Tooltip("Override encounter for this variant. Null = keep room's default encounter.")]
        [SerializeField] private EncounterSO _overrideEncounter;

        [Header("Environment")]
        [Tooltip("Index of environment child to activate (in Room's _variantEnvironments array). -1 = no environment swap.")]
        [SerializeField] private int _environmentIndex = -1;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Variant display name. </summary>
        public string VariantName => _variantName;

        /// <summary> Phase indices where this variant is active. </summary>
        public int[] ActivePhaseIndices => _activePhaseIndices;

        /// <summary> Override encounter (null = use default). </summary>
        public EncounterSO OverrideEncounter => _overrideEncounter;

        /// <summary> Environment child index to activate (-1 = no swap). </summary>
        public int EnvironmentIndex => _environmentIndex;

        /// <summary>
        /// Check if this variant should be active during the given phase.
        /// </summary>
        public bool IsActiveInPhase(int phaseIndex)
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
