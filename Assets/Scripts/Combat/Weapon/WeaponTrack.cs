using System;
using System.Collections.Generic;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Represents one weapon track (Primary or Secondary).
    /// Pure C# class — not a MonoBehaviour.
    /// Owns two SlotLayers (upper=Prism, lower=Core), manages fire cooldown,
    /// and caches a <see cref="TrackFiringSnapshot"/> that is rebuilt on loadout change.
    /// </summary>
    public class WeaponTrack
    {
        public enum TrackId { Primary, Secondary }

        private readonly TrackId _id;
        private readonly SlotLayer<StarCoreSO> _coreLayer;
        private readonly SlotLayer<PrismSO> _prismLayer;

        private float _fireCooldownTimer;
        private TrackFiringSnapshot _cachedSnapshot;
        private bool _snapshotDirty;

        /// <summary> Fires when this track's loadout changes (equip/unequip). </summary>
        public event Action OnLoadoutChanged;

        public TrackId Id => _id;
        public SlotLayer<StarCoreSO> CoreLayer => _coreLayer;
        public SlotLayer<PrismSO> PrismLayer => _prismLayer;

        /// <summary> True if the track has cores and cooldown is ready. </summary>
        public bool CanFire => !_coreLayer.IsEmpty && _fireCooldownTimer <= 0f;

        public WeaponTrack(TrackId id)
        {
            _id = id;
            _coreLayer = new SlotLayer<StarCoreSO>();
            _prismLayer = new SlotLayer<PrismSO>();
            _snapshotDirty = true;
        }

        /// <summary>
        /// Immediately resets the fire cooldown, allowing the next shot without delay.
        /// Used by Light Sail / Satellite abilities (e.g., Graze Engine).
        /// </summary>
        public void ResetCooldown()
        {
            _fireCooldownTimer = 0f;
        }

        /// <summary> Called by StarChartController each frame to update cooldown. </summary>
        public void Tick(float deltaTime)
        {
            if (_fireCooldownTimer > 0f)
                _fireCooldownTimer -= deltaTime;
        }

        /// <summary>
        /// Attempts to fire this track. Returns a snapshot if successful, null otherwise.
        /// Sets cooldown timer based on the snapshot's fire interval.
        /// </summary>
        public TrackFiringSnapshot TryFire()
        {
            if (!CanFire) return null;

            if (_snapshotDirty)
                RebuildSnapshot();

            if (_cachedSnapshot == null) return null;

            _fireCooldownTimer = _cachedSnapshot.TrackFireInterval;
            return _cachedSnapshot;
        }

        /// <summary> Equip a star core into the lower layer. </summary>
        public bool EquipCore(StarCoreSO core)
        {
            bool ok = _coreLayer.TryEquip(core);
            if (ok) MarkDirty();
            return ok;
        }

        /// <summary> Remove a star core from the lower layer. </summary>
        public bool UnequipCore(StarCoreSO core)
        {
            bool ok = _coreLayer.Unequip(core);
            if (ok) MarkDirty();
            return ok;
        }

        /// <summary> Equip a prism into the upper layer. </summary>
        public bool EquipPrism(PrismSO prism)
        {
            bool ok = _prismLayer.TryEquip(prism);
            if (ok) MarkDirty();
            return ok;
        }

        /// <summary> Remove a prism from the upper layer. </summary>
        public bool UnequipPrism(PrismSO prism)
        {
            bool ok = _prismLayer.Unequip(prism);
            if (ok) MarkDirty();
            return ok;
        }

        /// <summary> Clear all equipped items from both layers. </summary>
        public void ClearAll()
        {
            _coreLayer.Clear();
            _prismLayer.Clear();
            MarkDirty();
        }

        /// <summary>
        /// Pre-warms object pools for all equipped cores' prefabs.
        /// Call after loadout change or during initialization.
        /// Pool sizes are tuned per-family for optimal memory usage.
        /// </summary>
        public void InitializePools()
        {
            if (PoolManager.Instance == null) return;

            var cores = _coreLayer.Items;
            for (int i = 0; i < cores.Count; i++)
            {
                var core = cores[i];

                // Pre-warm projectile / entity pool based on core family
                if (core.ProjectilePrefab != null)
                {
                    switch (core.Family)
                    {
                        case CoreFamily.Matter:
                            // Physical bullets — high volume
                            PoolManager.Instance.GetPool(core.ProjectilePrefab, 20, 50);
                            break;

                        case CoreFamily.Light:
                            // Laser beams — short-lived LineRenderer objects, lower volume
                            PoolManager.Instance.GetPool(core.ProjectilePrefab, 5, 20);
                            break;

                        case CoreFamily.Echo:
                            // Shockwaves — typically fewer concurrent instances
                            PoolManager.Instance.GetPool(core.ProjectilePrefab, 5, 15);
                            break;

                        case CoreFamily.Anomaly:
                            // Anomaly entities reuse Projectile prefab
                            PoolManager.Instance.GetPool(core.ProjectilePrefab, 10, 30);

                            // Also pre-warm the modifier prefab pool (e.g. BoomerangModifier)
                            if (core.AnomalyModifierPrefab != null)
                                PoolManager.Instance.GetPool(core.AnomalyModifierPrefab, 10, 30);
                            break;

                        default:
                            // Fallback — treat as Matter
                            PoolManager.Instance.GetPool(core.ProjectilePrefab, 20, 50);
                            break;
                    }
                }

                // Muzzle flash — shared across all families
                if (core.MuzzleFlashPrefab != null)
                    PoolManager.Instance.GetPool(core.MuzzleFlashPrefab, 5, 20);
            }
        }

        /// <summary>
        /// Gets the projectile pool for a given prefab.
        /// Delegates to PoolManager (lazy creation if needed).
        /// </summary>
        public GameObjectPool GetProjectilePool(UnityEngine.GameObject prefab)
        {
            if (prefab == null || PoolManager.Instance == null) return null;
            return PoolManager.Instance.GetPool(prefab, 20, 50);
        }

        /// <summary>
        /// Gets the muzzle flash pool for a given prefab.
        /// </summary>
        public GameObjectPool GetMuzzleFlashPool(UnityEngine.GameObject prefab)
        {
            if (prefab == null || PoolManager.Instance == null) return null;
            return PoolManager.Instance.GetPool(prefab, 5, 20);
        }

        private void RebuildSnapshot()
        {
            _cachedSnapshot = SnapshotBuilder.Build(_coreLayer.Items, _prismLayer.Items);
            _snapshotDirty = false;
        }

        private void MarkDirty()
        {
            _snapshotDirty = true;
            OnLoadoutChanged?.Invoke();
        }
    }
}
