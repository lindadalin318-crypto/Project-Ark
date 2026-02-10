using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Builds a <see cref="TrackFiringSnapshot"/> from a weapon track's slot data.
    /// Aggregates prism modifiers (average-distributed for Add, full for Multiply),
    /// applies them to each core, and enforces the projectile hard cap.
    /// </summary>
    public static class SnapshotBuilder
    {
        /// <summary> Maximum projectiles spawned per single fire event. </summary>
        public const int MAX_PROJECTILES_PER_FIRE = 20;

        /// <summary> Bonus damage per excess projectile above the hard cap. </summary>
        private const float EXCESS_DAMAGE_PERCENT = 0.05f;

        // 复用字典避免每次 Build 分配
        private static readonly Dictionary<WeaponStatType, float> s_addAccumulator = new();
        private static readonly Dictionary<WeaponStatType, float> s_mulAccumulator = new();

        /// <summary>
        /// Builds a complete firing snapshot for one weapon track.
        /// Returns null if no cores are equipped (no fire).
        /// </summary>
        public static TrackFiringSnapshot Build(
            IReadOnlyList<StarCoreSO> cores,
            IReadOnlyList<PrismSO> prisms)
        {
            if (cores == null || cores.Count == 0) return null;

            int coreCount = cores.Count;

            // 聚合所有棱镜修改器
            AggregatePrismModifiers(prisms);

            // 对每个核心构建快照
            var snapshots = new List<CoreSnapshot>(coreCount);
            float totalHeat = 0f;
            float totalRecoil = 0f;
            float slowestInterval = 0f;
            int totalProjectiles = 0;

            // Collect Tint prism modifier prefabs (instantiated at runtime per projectile)
            List<GameObject> tintModifierPrefabs = CollectTintModifierPrefabs(prisms);

            for (int i = 0; i < coreCount; i++)
            {
                var snapshot = BuildCoreSnapshot(cores[i], coreCount, tintModifierPrefabs);
                snapshots.Add(snapshot);

                totalHeat += snapshot.HeatCost;
                totalRecoil += snapshot.RecoilForce;
                totalProjectiles += snapshot.ProjectileCount;

                float interval = snapshot.FireRate > 0f ? 1f / snapshot.FireRate : float.MaxValue;
                if (interval > slowestInterval)
                    slowestInterval = interval;
            }

            // 棱镜自身热量（flat，不参与分配）
            if (prisms != null)
            {
                for (int i = 0; i < prisms.Count; i++)
                    totalHeat += prisms[i].HeatCost;
            }

            // 弹幕硬上限
            float excessDamageBonus = 0f;
            if (totalProjectiles > MAX_PROJECTILES_PER_FIRE)
            {
                int excess = totalProjectiles - MAX_PROJECTILES_PER_FIRE;
                excessDamageBonus = excess * EXCESS_DAMAGE_PERCENT;

                // 按比例裁剪每个核心的 ProjectileCount
                CapProjectileCount(snapshots, MAX_PROJECTILES_PER_FIRE, totalProjectiles);
                totalProjectiles = MAX_PROJECTILES_PER_FIRE;

                // 将多余伤害均分到所有核心
                if (excessDamageBonus > 0f)
                {
                    float bonusPerCore = excessDamageBonus / coreCount;
                    for (int i = 0; i < snapshots.Count; i++)
                        snapshots[i].Damage *= (1f + bonusPerCore);
                }
            }

            return new TrackFiringSnapshot(
                snapshots, totalHeat, totalRecoil, slowestInterval,
                totalProjectiles, excessDamageBonus);
        }

        /// <summary>
        /// 聚合所有棱镜的 StatModifier 到加法和乘法累加器。
        /// </summary>
        private static void AggregatePrismModifiers(IReadOnlyList<PrismSO> prisms)
        {
            s_addAccumulator.Clear();
            s_mulAccumulator.Clear();

            if (prisms == null) return;

            for (int i = 0; i < prisms.Count; i++)
            {
                var modifiers = prisms[i].StatModifiers;
                if (modifiers == null) continue;

                for (int m = 0; m < modifiers.Length; m++)
                {
                    var mod = modifiers[m];
                    if (mod.Operation == ModifierOperation.Add)
                    {
                        s_addAccumulator.TryGetValue(mod.Stat, out float current);
                        s_addAccumulator[mod.Stat] = current + mod.Value;
                    }
                    else // Multiply
                    {
                        s_mulAccumulator.TryGetValue(mod.Stat, out float current);
                        // 乘法默认基础 = 1.0，累乘
                        s_mulAccumulator[mod.Stat] = (current == 0f ? 1f : current) * mod.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Collects all Tint family prism modifier prefab references.
        /// Returns a list of GameObjects (prefabs) rather than component instances,
        /// so that StarChartController can instantiate independent copies per projectile.
        /// Always returns a non-null list (empty if no tint prisms equipped)
        /// to prevent NullReferenceException downstream.
        /// </summary>
        private static List<GameObject> CollectTintModifierPrefabs(IReadOnlyList<PrismSO> prisms)
        {
            var result = new List<GameObject>();

            if (prisms == null || prisms.Count == 0) return result;

            for (int i = 0; i < prisms.Count; i++)
            {
                if (prisms[i].Family != PrismFamily.Tint) continue;
                if (prisms[i].ProjectileModifierPrefab == null) continue;

                // Validate that the prefab actually has an IProjectileModifier component
                if (prisms[i].ProjectileModifierPrefab.GetComponent<IProjectileModifier>() == null)
                    continue;

                result.Add(prisms[i].ProjectileModifierPrefab);
            }

            return result;
        }

        /// <summary>
        /// 对单个核心应用棱镜修正，生成 CoreSnapshot。
        /// </summary>
        private static CoreSnapshot BuildCoreSnapshot(StarCoreSO core, int coreCount,
                                                       List<GameObject> tintModifierPrefabs)
        {
            var snapshot = new CoreSnapshot
            {
                // 基础数值
                Damage = ApplyModifier(WeaponStatType.Damage, core.BaseDamage, coreCount),
                ProjectileSpeed = ApplyModifier(WeaponStatType.ProjectileSpeed, core.ProjectileSpeed, coreCount),
                Lifetime = ApplyModifier(WeaponStatType.Lifetime, core.Lifetime, coreCount),
                Spread = ApplyModifier(WeaponStatType.Spread, core.Spread, coreCount),
                Knockback = ApplyModifier(WeaponStatType.Knockback, core.Knockback, coreCount),
                RecoilForce = ApplyModifier(WeaponStatType.RecoilForce, core.RecoilForce, coreCount),
                FireRate = ApplyModifier(WeaponStatType.FireRate, core.FireRate, coreCount),
                ProjectileCount = Mathf.Max(1, Mathf.RoundToInt(
                    ApplyModifier(WeaponStatType.ProjectileCount, 1f, coreCount))),
                ProjectileSize = ApplyModifier(WeaponStatType.ProjectileSize, 1f, coreCount),
                HeatCost = ApplyModifier(WeaponStatType.HeatCost, core.HeatCost, coreCount),

                // 非数值数据（直通）
                ProjectilePrefab = core.ProjectilePrefab,
                MuzzleFlashPrefab = core.MuzzleFlashPrefab,
                ImpactVFXPrefab = core.ImpactVFXPrefab,
                FireSound = core.FireSound,
                FireSoundPitchVariance = core.FireSoundPitchVariance,
                Family = core.Family,

                // Anomaly family modifier prefab (pass-through from StarCoreSO)
                AnomalyModifierPrefab = core.AnomalyModifierPrefab,

                // Tint modifier prefabs (runtime-instantiated per projectile by StarChartController)
                TintModifierPrefabs = tintModifierPrefabs
            };

            // Clamp 到合法范围
            snapshot.Damage = Mathf.Max(0f, snapshot.Damage);
            snapshot.ProjectileSpeed = Mathf.Max(0.1f, snapshot.ProjectileSpeed);
            snapshot.Lifetime = Mathf.Max(0.1f, snapshot.Lifetime);
            snapshot.Spread = Mathf.Max(0f, snapshot.Spread);
            snapshot.Knockback = Mathf.Max(0f, snapshot.Knockback);
            snapshot.RecoilForce = Mathf.Max(0f, snapshot.RecoilForce);
            snapshot.FireRate = Mathf.Max(0.1f, snapshot.FireRate);
            snapshot.ProjectileSize = Mathf.Max(0.1f, snapshot.ProjectileSize);
            snapshot.HeatCost = Mathf.Max(0f, snapshot.HeatCost);

            return snapshot;
        }

        /// <summary>
        /// 对单个数值应用累积的棱镜修正。
        /// Multiply 全额应用，Add 平均分配给每个核心。
        /// </summary>
        private static float ApplyModifier(WeaponStatType stat, float baseValue, int coreCount)
        {
            float result = baseValue;

            // 先乘法
            if (s_mulAccumulator.TryGetValue(stat, out float mulValue))
                result *= mulValue;

            // 再加法（除以核心数 = 平均分配）
            if (s_addAccumulator.TryGetValue(stat, out float addValue))
                result += addValue / coreCount;

            return result;
        }

        /// <summary>
        /// 按比例裁剪 ProjectileCount 使总数不超过上限。
        /// </summary>
        private static void CapProjectileCount(List<CoreSnapshot> snapshots, int maxTotal, int currentTotal)
        {
            if (currentTotal <= maxTotal) return;

            float ratio = (float)maxTotal / currentTotal;
            int remaining = maxTotal;

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (i == snapshots.Count - 1)
                {
                    // 最后一个核心拿剩余配额
                    snapshots[i].ProjectileCount = Mathf.Max(1, remaining);
                }
                else
                {
                    int capped = Mathf.Max(1, Mathf.RoundToInt(snapshots[i].ProjectileCount * ratio));
                    snapshots[i].ProjectileCount = capped;
                    remaining -= capped;
                }
            }
        }
    }
}
