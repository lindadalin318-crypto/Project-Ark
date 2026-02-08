using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Light Sail — the driving style modifier in the Star Chart system.
    /// Monitors ship movement state and provides combat buffs based on player behavior.
    /// Only one Light Sail can be equipped at a time (unique engine slot).
    ///
    /// Runtime condition/effect logic will be implemented in Batch 3.
    /// This SO currently serves as data definition only.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLightSail", menuName = "ProjectArk/StarChart/Light Sail")]
    public class LightSailSO : StarChartItemSO
    {
        public override StarChartItemType ItemType => StarChartItemType.LightSail;

        [Header("Light Sail Design")]
        [Tooltip("Designer-readable trigger condition (e.g., 'Speed > 80% for 2s')")]
        [TextArea(2, 4)]
        [SerializeField] private string _conditionDescription;

        [Tooltip("Designer-readable effect description (e.g., '+30% damage for 5s')")]
        [TextArea(2, 4)]
        [SerializeField] private string _effectDescription;

        [Header("Runtime Behavior")]
        [Tooltip("Prefab containing a LightSailBehavior component. Instantiated as child of ship at runtime.")]
        [SerializeField] private GameObject _behaviorPrefab;

        // --- Public read-only properties ---

        public string ConditionDescription => _conditionDescription;
        public string EffectDescription => _effectDescription;
        public GameObject BehaviorPrefab => _behaviorPrefab;
    }
}
