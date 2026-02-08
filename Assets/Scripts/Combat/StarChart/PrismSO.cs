using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Prism — the data modifier in the Star Chart system.
    /// Alters weapon stats via vertical injection: Prism effects are averaged
    /// across all Cores in the same weapon track.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPrism", menuName = "ProjectArk/StarChart/Prism")]
    public class PrismSO : StarChartItemSO
    {
        public override StarChartItemType ItemType => StarChartItemType.Prism;

        [Header("Prism Identity")]
        [SerializeField] private PrismFamily _family;

        [Header("Stat Modifications")]
        [Tooltip("List of stat changes applied to Cores below this Prism")]
        [SerializeField] private StatModifier[] _statModifiers;

        [Header("Behavior Injection (Tint Family)")]
        [Tooltip("Optional: prefab containing IProjectileModifier for runtime behavior injection")]
        [SerializeField] private GameObject _projectileModifierPrefab;

        // --- Public read-only properties ---

        public PrismFamily Family => _family;
        public StatModifier[] StatModifiers => _statModifiers;
        public GameObject ProjectileModifierPrefab => _projectileModifierPrefab;
    }
}
