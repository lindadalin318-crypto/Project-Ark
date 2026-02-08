using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Hook interface for the Star Chart (The Loom) system to modify projectile behavior.
    /// Implementations will be injected by equipped Prisms, Sails, and Satellites at fire time.
    /// </summary>
    public interface IProjectileModifier
    {
        /// <summary> Called once when the projectile is spawned. </summary>
        void OnProjectileSpawned(Projectile projectile);

        /// <summary> Called every frame while the projectile is alive. </summary>
        void OnProjectileUpdate(Projectile projectile, float deltaTime);

        /// <summary> Called when the projectile hits a collider (before pool return). </summary>
        void OnProjectileHit(Projectile projectile, Collider2D other);
    }
}
