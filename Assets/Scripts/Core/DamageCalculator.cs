using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Centralized damage calculation utility. Applies elemental resistance and
    /// block reduction, producing a final damage value from a <see cref="DamagePayload"/>.
    /// All damage in the game flows through this single point.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate final damage after applying resistance and block modifiers.
        /// </summary>
        /// <param name="payload">The incoming damage data.</param>
        /// <param name="target">The entity receiving damage.</param>
        /// <returns>Final damage value (always >= 0).</returns>
        public static float Calculate(DamagePayload payload, IDamageable target)
        {
            float finalDamage = payload.BaseDamage;

            // Apply elemental resistance (if target exposes it)
            if (target is IResistant resistant)
            {
                float resistance = resistant.GetResistance(payload.Type);
                finalDamage *= (1f - Mathf.Clamp01(resistance));
            }

            // Apply block reduction (if target is blocking)
            if (target is IBlockable blockable && blockable.IsBlocking)
            {
                finalDamage *= (1f - Mathf.Clamp01(blockable.BlockDamageReduction));
            }

            return Mathf.Max(0f, finalDamage);
        }
    }
}
