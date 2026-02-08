using UnityEngine;

namespace ProjectArk.Heat
{
    /// <summary>
    /// Configuration data for the ship's heat/entropy system.
    /// Controls heat capacity, passive cooling, and overheat behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeatStats", menuName = "ProjectArk/Heat/Heat Stats")]
    public class HeatStatsSO : ScriptableObject
    {
        [Header("Capacity")]
        [Tooltip("Maximum heat value before overheat triggers")]
        [SerializeField] private float _maxHeat = 100f;

        [Header("Cooling")]
        [Tooltip("Heat dissipated per second (passive)")]
        [SerializeField] private float _coolingRate = 15f;

        [Header("Overheat")]
        [Tooltip("Duration of the overheat silence penalty (seconds)")]
        [SerializeField] private float _overheatDuration = 3f;

        [Tooltip("Normalized threshold to trigger overheat (1.0 = at max heat)")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _overheatThreshold = 1f;

        // --- Public read-only properties ---

        public float MaxHeat => _maxHeat;
        public float CoolingRate => _coolingRate;
        public float OverheatDuration => _overheatDuration;
        public float OverheatThreshold => _overheatThreshold;

        /// <summary> Absolute heat value at which overheat triggers. </summary>
        public float OverheatHeatValue => _maxHeat * _overheatThreshold;
    }
}
