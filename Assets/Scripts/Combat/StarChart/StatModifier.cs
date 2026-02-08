using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// A single stat modification entry used by Prisms to alter weapon parameters.
    /// Applied during the firing pipeline's "Processing" step.
    /// </summary>
    [System.Serializable]
    public struct StatModifier
    {
        [Tooltip("Which weapon stat to modify")]
        public WeaponStatType Stat;

        [Tooltip("Add: base + value. Multiply: base * value")]
        public ModifierOperation Operation;

        [Tooltip("The modification value")]
        public float Value;
    }
}
