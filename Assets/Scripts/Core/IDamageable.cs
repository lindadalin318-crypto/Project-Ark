using System;
using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Universal damage interface. Any entity that can receive damage
    /// (enemies, destructibles, the player) implements this.
    /// Lives in Core so both Ship and Combat assemblies can reference it.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage using the structured payload (preferred path).
        /// DamageCalculator runs resistance/block checks internally.
        /// </summary>
        void TakeDamage(DamagePayload payload);

        /// <summary>
        /// Legacy overload — forwards to TakeDamage(DamagePayload) with Physical type.
        /// </summary>
        [Obsolete("Use TakeDamage(DamagePayload) instead")]
        void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce);

        /// <summary>
        /// Whether this entity is still alive (HP > 0).
        /// </summary>
        bool IsAlive { get; }
    }
}
