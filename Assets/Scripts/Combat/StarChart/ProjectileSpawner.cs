using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Pure C# helper extracted from <see cref="StarChartController"/> (L3-1 Phase B).
    /// Owns the full per-projectile spawn pipeline:
    /// CoreFamily dispatch, prefab pooling, ProjectileSize scaling, Modifier injection,
    /// LightSail parameter mutation, MuzzleFlash, and fire-sound playback.
    ///
    /// Per <c>Implement_rules.md</c> §8.3.2 / Plan §八.6: this class is the
    /// <b>single owner</b> of the <see cref="CoreFamily"/> switch. No other type
    /// may introduce a parallel CoreFamily branch.
    ///
    /// Non-goals (kept in <c>StarChartController.ExecuteFire</c>):
    ///   • Snapshot retrieval / spread fan calculation
    ///   • Recoil impulse, heat accumulation, OnTrackFired event, CombatEvents broadcast
    ///
    /// Dependencies are injected via constructor:
    ///   • FirePoint (optional — null means use <paramref name="shipTransform"/> as fallback)
    ///   • Ship Transform (for fallback spawn position)
    ///   • AudioSource (dedicated, 2D, pre-configured)
    ///   • Func&lt;LightSailRunner&gt; — resolves the active slot's sail runner lazily
    ///     so this class does not have to track loadout-switch state.
    /// </summary>
    internal sealed class ProjectileSpawner
    {
        private readonly Transform _shipTransform;
        private readonly FirePoint _firePoint;
        private readonly AudioSource _audioSource;
        private readonly Func<LightSailRunner> _activeSailRunnerProvider;

        internal ProjectileSpawner(
            Transform shipTransform,
            FirePoint firePoint,
            AudioSource audioSource,
            Func<LightSailRunner> activeSailRunnerProvider)
        {
            _shipTransform = shipTransform;
            _firePoint = firePoint;
            _audioSource = audioSource;
            _activeSailRunnerProvider = activeSailRunnerProvider;
        }

        /// <summary>
        /// Resolve the spawn position. Returns FirePoint.Position when available,
        /// otherwise the ship transform's world position.
        /// </summary>
        internal Vector3 GetSpawnPosition()
        {
            return _firePoint != null ? _firePoint.Position : _shipTransform.position;
        }

        // ══════════════════════════════════════════════════════════════
        // CoreFamily dispatch
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Dispatches projectile spawning based on CoreFamily.
        /// Direction already includes spread calculation from ExecuteFire.
        /// </summary>
        internal void SpawnProjectile(WeaponTrack track, CoreSnapshot coreSnap,
                                      Vector2 direction, Vector3 spawnPos)
        {
            switch (coreSnap.Family)
            {
                case CoreFamily.Matter:
                    SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Light:
                    SpawnLightBeam(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Echo:
                    SpawnEchoWave(track, coreSnap, direction, spawnPos);
                    break;
                case CoreFamily.Anomaly:
                    SpawnAnomalyEntity(track, coreSnap, direction, spawnPos);
                    break;
                default:
                    Debug.LogWarning($"[ProjectileSpawner] Unknown CoreFamily '{coreSnap.Family}', falling back to Matter.");
                    SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                    break;
            }
        }

        /// <summary> Matter family: physical rigidbody projectile. </summary>
        private void SpawnMatterProjectile(WeaponTrack track, CoreSnapshot coreSnap,
                                           Vector2 direction, Vector3 spawnPos)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            GameObject bulletObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _activeSailRunnerProvider?.Invoke()?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this projectile
            var modifiers = InstantiateModifiers(bulletObj, coreSnap.TintModifierPrefabs);
            projectile.Initialize(direction, parms, modifiers);
        }

        /// <summary> Light family: instant raycast laser beam. </summary>
        private void SpawnLightBeam(WeaponTrack track, CoreSnapshot coreSnap,
                                    Vector2 direction, Vector3 spawnPos)
        {
            // Get LaserBeam prefab pool (uses the same ProjectilePrefab field, but expects LaserBeam component)
            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            GameObject beamObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var laserBeam = beamObj.GetComponent<LaserBeam>();
            if (laserBeam == null)
            {
                Debug.LogWarning("[ProjectileSpawner] Light core prefab missing LaserBeam component, falling back to Matter.");
                // Return the beam object and fall back
                var poolRef = beamObj.GetComponent<PoolReference>();
                poolRef?.ReturnToPool();
                SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                return;
            }

            var parms = coreSnap.ToProjectileParams();
            _activeSailRunnerProvider?.Invoke()?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this beam
            var modifiers = InstantiateModifiers(beamObj, coreSnap.TintModifierPrefabs);
            laserBeam.Fire(spawnPos, direction, parms, modifiers);
        }

        /// <summary> Echo family: expanding shockwave AOE. </summary>
        private void SpawnEchoWave(WeaponTrack track, CoreSnapshot coreSnap,
                                   Vector2 direction, Vector3 spawnPos)
        {
            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            GameObject waveObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var echoWave = waveObj.GetComponent<EchoWave>();
            if (echoWave == null)
            {
                Debug.LogWarning("[ProjectileSpawner] Echo core prefab missing EchoWave component, falling back to Matter.");
                var poolRef = waveObj.GetComponent<PoolReference>();
                poolRef?.ReturnToPool();
                SpawnMatterProjectile(track, coreSnap, direction, spawnPos);
                return;
            }

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                waveObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _activeSailRunnerProvider?.Invoke()?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate independent modifier copies for this wave
            var modifiers = InstantiateModifiers(waveObj, coreSnap.TintModifierPrefabs);
            echoWave.Fire(spawnPos, direction, parms, modifiers, coreSnap.Spread);
        }

        /// <summary> Anomaly family: custom behavior entity (e.g., boomerang). </summary>
        private void SpawnAnomalyEntity(WeaponTrack track, CoreSnapshot coreSnap,
                                        Vector2 direction, Vector3 spawnPos)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            var pool = track.GetProjectilePool(coreSnap.ProjectilePrefab);
            if (pool == null) return;

            GameObject bulletObj = pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            // Apply ProjectileSize scaling
            if (!Mathf.Approximately(coreSnap.ProjectileSize, 1f))
                bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize;

            var parms = coreSnap.ToProjectileParams();
            _activeSailRunnerProvider?.Invoke()?.ModifyProjectileParams(ref parms);

            // Runtime-instantiate Tint modifier copies
            var modifiers = InstantiateModifiers(bulletObj, coreSnap.TintModifierPrefabs);

            // Also instantiate Anomaly-specific modifier (e.g. BoomerangModifier)
            if (coreSnap.AnomalyModifierPrefab != null)
            {
                var anomalyPrefabs = new List<GameObject>(1) { coreSnap.AnomalyModifierPrefab };
                var anomalyModifiers = InstantiateModifiers(bulletObj, anomalyPrefabs);
                modifiers.AddRange(anomalyModifiers);
            }

            projectile.Initialize(direction, parms, modifiers);
        }

        // ══════════════════════════════════════════════════════════════
        // Secondary spawn effects (per-core)
        // ══════════════════════════════════════════════════════════════

        internal void SpawnMuzzleFlash(WeaponTrack track, CoreSnapshot coreSnap,
                                       Vector2 direction, Vector3 spawnPos)
        {
            if (coreSnap.MuzzleFlashPrefab == null) return;

            var pool = track.GetMuzzleFlashPool(coreSnap.MuzzleFlashPrefab);
            if (pool == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            pool.Get(spawnPos, Quaternion.Euler(0f, 0f, angle));
        }

        internal void PlayFireSound(CoreSnapshot coreSnap)
        {
            if (coreSnap.FireSound == null) return;

            float pitch = 1f + UnityEngine.Random.Range(
                -coreSnap.FireSoundPitchVariance,
                coreSnap.FireSoundPitchVariance);
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(coreSnap.FireSound);
        }

        // ══════════════════════════════════════════════════════════════
        // Modifier instantiation helper
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Instantiates independent IProjectileModifier copies on the target GameObject
        /// from a list of modifier prefabs. Uses AddComponent + JsonUtility.FromJsonOverwrite
        /// to create a deep copy of each modifier's serialized fields.
        /// </summary>
        /// <param name="targetObj">The projectile/beam/wave GameObject to attach modifiers to.</param>
        /// <param name="prefabs">List of modifier prefab GameObjects (each expected to have IProjectileModifier).</param>
        /// <returns>List of newly instantiated modifier instances (may be empty, never null).</returns>
        private static List<IProjectileModifier> InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)
        {
            var result = new List<IProjectileModifier>();
            if (prefabs == null || prefabs.Count == 0) return result;

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == null) continue;

                var srcModifiers = prefabs[i].GetComponents<IProjectileModifier>();
                for (int m = 0; m < srcModifiers.Length; m++)
                {
                    var srcComponent = srcModifiers[m] as MonoBehaviour;
                    if (srcComponent == null) continue;

                    // AddComponent of the same type, then copy serialized field values
                    var newComponent = targetObj.AddComponent(srcComponent.GetType()) as IProjectileModifier;
                    if (newComponent != null)
                    {
                        var json = JsonUtility.ToJson(srcComponent);
                        JsonUtility.FromJsonOverwrite(json, newComponent as MonoBehaviour);
                        result.Add(newComponent);
                    }
                }
            }

            return result;
        }
    }
}
