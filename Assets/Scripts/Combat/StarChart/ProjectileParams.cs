using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Lightweight value type carrying all data a Projectile needs for initialization.
    /// Decouples Projectile from StarCoreSO,
    /// allowing the Star Chart pipeline to pass prism-modified values.
    ///
    /// Also carries per-Core visual parameters (<see cref="TrailTime"/>, <see cref="TrailWidth"/>,
    /// <see cref="TrailColor"/>). Negative / NaN values are interpreted as "not specified";
    /// Projectile will keep its prefab / fallback defaults in that case. This keeps the
    /// pipeline data-driven per architecture principle §1 (no hardcoded visual magic numbers
    /// inside gameplay scripts).
    /// </summary>
    public readonly struct ProjectileParams
    {
        public readonly float Damage;
        public readonly float Speed;
        public readonly float Lifetime;
        public readonly float Knockback;
        public readonly GameObject ImpactVFXPrefab;
        public readonly DamageType DamageType;

        // --- Visual ---
        /// <summary> Trail time from StarCoreSO. <=0 means "use prefab / fallback default". </summary>
        public readonly float TrailTime;
        /// <summary> Trail head width from StarCoreSO. <=0 means "use prefab / fallback default". </summary>
        public readonly float TrailWidth;
        /// <summary> Trail head color from StarCoreSO. alpha &lt;= 0 means "use prefab / fallback default". </summary>
        public readonly Color TrailColor;

        public ProjectileParams(float damage, float speed, float lifetime,
                                float knockback, GameObject impactVFXPrefab,
                                DamageType damageType = DamageType.Physical,
                                float trailTime = -1f, float trailWidth = -1f,
                                Color trailColor = default)
        {
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            Knockback = knockback;
            ImpactVFXPrefab = impactVFXPrefab;
            DamageType = damageType;
            TrailTime = trailTime;
            TrailWidth = trailWidth;
            TrailColor = trailColor;
        }

        /// <summary>
        /// Returns a new ProjectileParams with damage multiplied by the given factor.
        /// Zero-allocation stack copy (readonly struct).
        /// </summary>
        public ProjectileParams WithDamageMultiplied(float multiplier)
        {
            return new ProjectileParams(
                Damage * multiplier, Speed, Lifetime, Knockback, ImpactVFXPrefab, DamageType,
                TrailTime, TrailWidth, TrailColor);
        }
    }
}
