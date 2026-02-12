using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Snapshot of a single core's stats after prism modification.
    /// One CoreSnapshot per core per fire event.
    /// </summary>
    public class CoreSnapshot
    {
        // 修正后的数值
        public float Damage;
        public float ProjectileSpeed;
        public float Lifetime;
        public float Spread;
        public float Knockback;
        public float RecoilForce;
        public float FireRate;
        public int ProjectileCount;
        public float ProjectileSize;
        public float HeatCost;

        // 非数值数据（直通，不受棱镜修改）
        public GameObject ProjectilePrefab;
        public GameObject MuzzleFlashPrefab;
        public GameObject ImpactVFXPrefab;
        public AudioClip FireSound;
        public float FireSoundPitchVariance;
        public CoreFamily Family;
        public DamageType DamageType;

        // Anomaly family modifier prefab (set by SnapshotBuilder from StarCoreSO)
        public GameObject AnomalyModifierPrefab;

        // Tint family prism modifier prefabs (instantiated at runtime per projectile)
        public List<GameObject> TintModifierPrefabs;

        /// <summary>
        /// Convert to ProjectileParams for Projectile.Initialize().
        /// </summary>
        public ProjectileParams ToProjectileParams()
        {
            return new ProjectileParams(Damage, ProjectileSpeed, Lifetime, Knockback, ImpactVFXPrefab, DamageType);
        }
    }

    /// <summary>
    /// Complete firing snapshot for one weapon track.
    /// Produced by SnapshotBuilder, consumed by StarChartController.
    /// </summary>
    public class TrackFiringSnapshot
    {
        public readonly List<CoreSnapshot> CoreSnapshots;
        public readonly float TotalHeatCost;
        public readonly float TotalRecoilForce;
        public readonly float TrackFireInterval;
        public readonly int TotalProjectileCount;
        public readonly float ExcessDamageBonus;

        public TrackFiringSnapshot(List<CoreSnapshot> coreSnapshots, float totalHeatCost,
                                    float totalRecoilForce, float trackFireInterval,
                                    int totalProjectileCount, float excessDamageBonus)
        {
            CoreSnapshots = coreSnapshots;
            TotalHeatCost = totalHeatCost;
            TotalRecoilForce = totalRecoilForce;
            TrackFireInterval = trackFireInterval;
            TotalProjectileCount = totalProjectileCount;
            ExcessDamageBonus = excessDamageBonus;
        }
    }
}
