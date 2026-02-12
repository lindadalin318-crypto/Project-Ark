using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Immutable value type carrying all data for a single damage event.
    /// Replaces the (float damage, Vector2 knockbackDir, float knockbackForce) tuple
    /// with a structured payload that supports elemental types and source tracking.
    /// </summary>
    public readonly struct DamagePayload
    {
        public readonly float BaseDamage;
        public readonly DamageType Type;
        public readonly Vector2 KnockbackDirection;
        public readonly float KnockbackForce;
        public readonly GameObject Source;

        public DamagePayload(float baseDamage, DamageType type,
                             Vector2 knockbackDirection, float knockbackForce,
                             GameObject source = null)
        {
            BaseDamage = baseDamage;
            Type = type;
            KnockbackDirection = knockbackDirection;
            KnockbackForce = knockbackForce;
            Source = source;
        }

        /// <summary>
        /// Convenience constructor for Physical damage (most common case).
        /// </summary>
        public DamagePayload(float baseDamage, Vector2 knockbackDirection, float knockbackForce,
                             GameObject source = null)
            : this(baseDamage, DamageType.Physical, knockbackDirection, knockbackForce, source)
        {
        }
    }
}
