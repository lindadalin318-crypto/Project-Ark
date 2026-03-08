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
        private readonly SlotLayer<SatelliteSO> _satLayer;

        private float _fireCooldownTimer;
        private TrackFiringSnapshot _cachedSnapshot;
        private bool _snapshotDirty;

        /// <summary> Fires when this track's loadout changes (equip/unequip). </summary>
        public event Action OnLoadoutChanged;

        /// <summary>
        /// Notify subscribers that this track's loadout has changed.
        /// Called by StarChartController when equipping/unequipping satellites externally.
        /// </summary>
        public void NotifyLoadoutChanged() => OnLoadoutChanged?.Invoke();

        /// <summary>
        /// Satellites equipped on this track, backed by SatLayer.
        /// Read-only view for compatibility with existing code.
        /// </summary>
        public IReadOnlyList<SatelliteSO> EquippedSatelliteSOs => _satLayer.Items;

        public TrackId Id => _id;
        public SlotLayer<StarCoreSO> CoreLayer => _coreLayer;
        public SlotLayer<PrismSO> PrismLayer => _prismLayer;
        public SlotLayer<SatelliteSO> SatLayer => _satLayer;

        /// <summary> True if the track has cores and cooldown is ready. </summary>
        public bool CanFire => !_coreLayer.IsEmpty && _fireCooldownTimer <= 0f;

        public WeaponTrack(TrackId id)
        {
            _id = id;
            // All layers start with 2 cols × 1 row = 2 cells (horizontal layout).
            _coreLayer  = new SlotLayer<StarCoreSO>(initialCols: 2, initialRows: 1);
            _prismLayer = new SlotLayer<PrismSO>(initialCols: 2, initialRows: 1);
            _satLayer   = new SlotLayer<SatelliteSO>(initialCols: 2, initialRows: 1);
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

        /// <summary>
        /// Equip a star core at a specific anchor position.
        /// Uses TryPlace which respects the exact anchor and evicts blocking items.
        /// </summary>
        public bool EquipCore(StarCoreSO core, int anchorCol, int anchorRow)
        {
            bool ok = _coreLayer.TryPlace(core, anchorCol, anchorRow);
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

        /// <summary>
        /// Equip a prism at a specific anchor position.
        /// Uses TryPlace which respects the exact anchor and evicts blocking items.
        /// </summary>
        public bool EquipPrism(PrismSO prism, int anchorCol, int anchorRow)
        {
            bool ok = _prismLayer.TryPlace(prism, anchorCol, anchorRow);
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

        /// <summary>
        /// Equip a satellite at a specific anchor position in the SAT layer.
        /// Uses TryPlace which respects the exact anchor.
        /// </summary>
        public bool EquipSatellite(SatelliteSO sat, int anchorCol, int anchorRow)
        {
            bool ok = _satLayer.TryPlace(sat, anchorCol, anchorRow);
            if (ok) NotifyLoadoutChanged();
            return ok;
        }

        /// <summary>
        /// Legacy: equip a satellite by finding the first available slot.
        /// </summary>
        public bool EquipSatellite(SatelliteSO sat)
        {
            bool ok = _satLayer.TryEquip(sat);
            if (ok) NotifyLoadoutChanged();
            return ok;
        }

        /// <summary> Remove a satellite from the SAT layer. </summary>
        public bool UnequipSatellite(SatelliteSO sat)
        {
            bool ok = _satLayer.Unequip(sat);
            if (ok) NotifyLoadoutChanged();
            return ok;
        }

        /// <summary>
        /// Restores the unlocked column counts for Core, Prism, and SAT layers from save data.
        /// Called during ImportTrack before re-equipping items.
        /// </summary>
        public void SetLayerCols(int coreCols, int prismCols, int satCols = 2)
        {
            // Clamp to valid range without UnityEngine.Mathf (WeaponTrack is pure C#)
            // Minimum is 2 (initial state = 2 cols × 1 row)
            int targetCore  = coreCols  < 2 ? 2 : coreCols  > SlotLayer<StarCoreSO>.MAX_COLS ? SlotLayer<StarCoreSO>.MAX_COLS : coreCols;
            int targetPrism = prismCols < 2 ? 2 : prismCols > SlotLayer<PrismSO>.MAX_COLS    ? SlotLayer<PrismSO>.MAX_COLS    : prismCols;
            int targetSat   = satCols   < 2 ? 2 : satCols   > SlotLayer<SatelliteSO>.MAX_COLS ? SlotLayer<SatelliteSO>.MAX_COLS : satCols;

            // Unlock columns one by one until we reach the saved count
            while (_coreLayer.Cols < targetCore)
                _coreLayer.TryUnlockColumn();
            while (_prismLayer.Cols < targetPrism)
                _prismLayer.TryUnlockColumn();
            while (_satLayer.Cols < targetSat)
                _satLayer.TryUnlockColumn();
        }

        /// <summary>
        /// Restores the unlocked row counts for Core, Prism, and SAT layers from save data.
        /// Called during ImportTrack before re-equipping items.
        /// </summary>
        public void SetLayerRows(int coreRows, int prismRows, int satRows = 1)
        {
            // Clamp to valid range without UnityEngine.Mathf (WeaponTrack is pure C#)
            int targetCore  = coreRows  < 1 ? 1 : coreRows  > SlotLayer<StarCoreSO>.MAX_ROWS ? SlotLayer<StarCoreSO>.MAX_ROWS : coreRows;
            int targetPrism = prismRows < 1 ? 1 : prismRows > SlotLayer<PrismSO>.MAX_ROWS    ? SlotLayer<PrismSO>.MAX_ROWS    : prismRows;
            int targetSat   = satRows   < 1 ? 1 : satRows   > SlotLayer<SatelliteSO>.MAX_ROWS ? SlotLayer<SatelliteSO>.MAX_ROWS : satRows;

            // Unlock rows one by one until we reach the saved count
            while (_coreLayer.Rows < targetCore)
                _coreLayer.TryUnlockRow();
            while (_prismLayer.Rows < targetPrism)
                _prismLayer.TryUnlockRow();
            while (_satLayer.Rows < targetSat)
                _satLayer.TryUnlockRow();
        }

        /// <summary> Clear all equipped items from both layers and the satellite layer. </summary>
        public void ClearAll()
        {
            _coreLayer.Clear();
            _prismLayer.Clear();
            _satLayer.Clear();
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
