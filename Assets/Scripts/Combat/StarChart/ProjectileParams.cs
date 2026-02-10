using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Lightweight value type carrying all data a Projectile needs for initialization.
    /// Decouples Projectile from StarCoreSO,
    /// allowing the Star Chart pipeline to pass prism-modified values.
    /// </summary>
    public readonly struct ProjectileParams
    {
        public readonly float Damage;
        public readonly float Speed;
        public readonly float Lifetime;
        public readonly float Knockback;
        public readonly GameObject ImpactVFXPrefab;

        public ProjectileParams(float damage, float speed, float lifetime,
                                float knockback, GameObject impactVFXPrefab)
        {
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            Knockback = knockback;
            ImpactVFXPrefab = impactVFXPrefab;
        }

        /// <summary>
        /// Returns a new ProjectileParams with damage multiplied by the given factor.
        /// Zero-allocation stack copy (readonly struct).
        /// </summary>
        public ProjectileParams WithDamageMultiplied(float multiplier)
        {
            return new ProjectileParams(
                Damage * multiplier, Speed, Lifetime, Knockback, ImpactVFXPrefab);
        }

    }
}
