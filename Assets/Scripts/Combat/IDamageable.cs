using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Universal damage interface. Any entity that can receive damage
    /// (enemies, destructibles, potentially the player) implements this.
    /// Projectiles, laser beams, and echo waves call TakeDamage via this contract.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this entity.
        /// </summary>
        /// <param name="damage">Amount of damage to deal.</param>
        /// <param name="knockbackDirection">Normalized direction of knockback force.</param>
        /// <param name="knockbackForce">Magnitude of knockback impulse.</param>
        void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce);

        /// <summary>
        /// Whether this entity is still alive (HP > 0).
        /// </summary>
        bool IsAlive { get; }
    }
}
