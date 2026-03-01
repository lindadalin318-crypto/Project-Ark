using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Fractal Prism modifier: stops the projectile in place on spawn and
    /// extends its lifetime to 3× the original value, turning shots into mines.
    /// Attach to a Prefab referenced by <see cref="PrismSO.ProjectileModifierPrefab"/>.
    /// </summary>
    public class MinePlacerModifier : MonoBehaviour, IProjectileModifier
    {
        [Header("Mine Settings")]
        [Tooltip("Multiplier applied to the projectile's original lifetime")]
        [SerializeField] private float _lifetimeMultiplier = 3f;

        // --- IProjectileModifier ---

        public void OnProjectileSpawned(Projectile projectile)
        {
            if (projectile == null) return;

            // Stop the projectile immediately
            projectile.Speed = 0f;
            var rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            // Extend lifetime: we cannot modify the private _lifetimeTimer directly,
            // so we use the public LifetimeRemaining property if available,
            // or re-initialize via the runtime field exposed by Projectile.
            // Since Projectile exposes LifetimeRemaining as a settable property, use it.
            projectile.LifetimeRemaining *= _lifetimeMultiplier;
        }

        public void OnProjectileUpdate(Projectile projectile, float deltaTime)
        {
            // Ensure the mine stays stationary every frame (in case of physics drift)
            if (projectile == null) return;
            if (!Mathf.Approximately(projectile.Speed, 0f))
            {
                projectile.Speed = 0f;
                var rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
        }

        public void OnProjectileHit(Projectile projectile, Collider2D other)
        {
            // Normal hit handling — Projectile will return to pool
        }
    }
}
