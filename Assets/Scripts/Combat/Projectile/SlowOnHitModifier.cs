using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Tint Prism modifier: applies a slow debuff on hit.
    /// Currently a placeholder implementation that logs the effect;
    /// will be wired to the enemy debuff system once it exists.
    /// </summary>
    public class SlowOnHitModifier : MonoBehaviour, IProjectileModifier
    {
        [SerializeField, Range(1f, 99f)]
        private float _slowPercent = 30f;

        [SerializeField, Min(0.1f)]
        private float _duration = 2f;

        /// <summary> Slow strength in percent (0-100). </summary>
        public float SlowPercent => _slowPercent;

        /// <summary> Slow duration in seconds. </summary>
        public float Duration => _duration;

        public void OnProjectileSpawned(Projectile projectile)
        {
            // No spawn-time logic needed for slow effect.
        }

        public void OnProjectileUpdate(Projectile projectile, float deltaTime)
        {
            // No per-frame logic needed for slow effect.
        }

        public void OnProjectileHit(Projectile projectile, Collider2D other)
        {
            // TODO: Replace with actual debuff application once enemy stat system is implemented.
            // Example: var target = other.GetComponent<IDamageable>();
            //          if (target != null) target.ApplySlow(_slowPercent, _duration);

            Debug.Log($"[Tint] Slow applied to {other.name}: -{_slowPercent}% speed for {_duration}s");
        }
    }
}
