using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Satellite — the autonomous event responder in the Star Chart system.
    /// Operates on an IF-THEN logic: when a trigger condition is met, it executes
    /// a response action automatically without player input.
    ///
    /// Runtime IF-THEN logic will be implemented in Batch 3.
    /// This SO currently serves as data definition only.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSatellite", menuName = "ProjectArk/StarChart/Satellite")]
    public class SatelliteSO : StarChartItemSO
    {
        public override StarChartItemType ItemType => StarChartItemType.Satellite;

        [Header("Satellite Design")]
        [Tooltip("Designer-readable trigger condition (e.g., 'OnShieldBroken')")]
        [TextArea(2, 4)]
        [SerializeField] private string _triggerDescription;

        [Tooltip("Designer-readable response action (e.g., 'SpawnShockwave at ship position')")]
        [TextArea(2, 4)]
        [SerializeField] private string _actionDescription;

        [Tooltip("Minimum seconds between activations to prevent loops")]
        [SerializeField] private float _internalCooldown = 0.5f;

        [Header("Runtime Behavior")]
        [Tooltip("Prefab containing a SatelliteBehavior component. Instantiated as child of ship at runtime.")]
        [SerializeField] private GameObject _behaviorPrefab;

        // --- Public read-only properties ---

        public string TriggerDescription => _triggerDescription;
        public string ActionDescription => _actionDescription;
        public float InternalCooldown => _internalCooldown;
        public GameObject BehaviorPrefab => _behaviorPrefab;
    }
}
