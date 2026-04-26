using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Owns the Star Chart runtime loadout state: 3 independent <see cref="LoadoutSlot"/>
    /// instances, their per-slot <see cref="LightSailRunner"/> and
    /// <see cref="SatelliteRunner"/> lists, and the active slot index.
    /// Extracted from <see cref="StarChartController"/> as part of L3-1 Phase C.
    /// </summary>
    /// <remarks>
    /// Pure C# class — not a MonoBehaviour. Constructed by the owning
    /// <see cref="StarChartController"/> in <c>Awake</c>. This class is the
    /// <b>single owner</b> of the loadout state arrays: the Controller, the
    /// <see cref="StarChartSaveSerializer"/>, and the <see cref="ProjectileSpawner"/>
    /// all read these arrays through the read-only properties / accessors below
    /// rather than holding their own references.
    ///
    /// <para>Public equip/unequip/switch semantics mirror the original Controller
    /// API exactly — every behavior (first-available fallback on <c>TryPlace</c>,
    /// auto-eviction when <c>FreeSpace &lt;= 0</c>, reference-equals no-op when the
    /// target cell already holds the same sail, dispose ordering on switch) is
    /// preserved verbatim.</para>
    ///
    /// <para>Events <see cref="OnLightSailChanged"/> / <see cref="OnSatellitesChanged"/>
    /// are forwarded to the Controller which re-exposes them to UI consumers.</para>
    /// </remarks>
    internal sealed class LoadoutManager
    {
        private const int SLOT_COUNT = 3;

        private readonly LoadoutSlot[] _loadouts;
        private readonly LightSailRunner[] _lightSailRunners;
        private readonly List<SatelliteRunner>[] _primarySatRunners;
        private readonly List<SatelliteRunner>[] _secondarySatRunners;
        private readonly StarChartContext _context;

        private int _activeLoadoutIndex;

        // Called by the Controller so that InitializeAllPools runs when
        // SwitchLoadout promotes a new slot to active (pool warm-up is a
        // Controller-side concern; Manager only signals the transition).
        private readonly Action _initializeAllPools;

        // --- Events (forwarded by Controller) ---

        internal event Action OnLightSailChanged;
        internal event Action OnSatellitesChanged;

        internal LoadoutManager(StarChartContext context, Action initializeAllPools)
        {
            _context = context;
            _initializeAllPools = initializeAllPools;

            _loadouts = new LoadoutSlot[SLOT_COUNT];
            _lightSailRunners = new LightSailRunner[SLOT_COUNT];
            _primarySatRunners = new List<SatelliteRunner>[SLOT_COUNT];
            _secondarySatRunners = new List<SatelliteRunner>[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                _loadouts[i] = new LoadoutSlot();
                _primarySatRunners[i] = new List<SatelliteRunner>();
                _secondarySatRunners[i] = new List<SatelliteRunner>();
            }
            _activeLoadoutIndex = 0;
        }

        // ══════════════════════════════════════════════════════════════
        // State exposure (read-only references shared with Serializer / Spawner)
        // ══════════════════════════════════════════════════════════════

        /// <summary> Raw slot array (shared with <see cref="StarChartSaveSerializer"/>). </summary>
        internal LoadoutSlot[] Loadouts => _loadouts;

        /// <summary> Per-slot sail runner array (shared with Serializer). </summary>
        internal LightSailRunner[] LightSailRunners => _lightSailRunners;

        /// <summary> Per-slot primary-track satellite runners (shared with Serializer). </summary>
        internal List<SatelliteRunner>[] PrimarySatRunners => _primarySatRunners;

        /// <summary> Per-slot secondary-track satellite runners (shared with Serializer). </summary>
        internal List<SatelliteRunner>[] SecondarySatRunners => _secondarySatRunners;

        /// <summary> Current active loadout index (0-based). </summary>
        internal int ActiveLoadoutIndex => _activeLoadoutIndex;

        /// <summary> The active loadout slot. </summary>
        internal LoadoutSlot ActiveSlot => _loadouts[_activeLoadoutIndex];

        /// <summary> Sail runner for the active slot (used by ProjectileSpawner.Func). </summary>
        internal LightSailRunner ActiveLightSailRunner => _lightSailRunners[_activeLoadoutIndex];

        /// <summary> Primary-track satellite runners for the active slot. </summary>
        internal List<SatelliteRunner> ActivePrimarySatRunners => _primarySatRunners[_activeLoadoutIndex];

        /// <summary> Secondary-track satellite runners for the active slot. </summary>
        internal List<SatelliteRunner> ActiveSecondarySatRunners => _secondarySatRunners[_activeLoadoutIndex];

        internal WeaponTrack PrimaryTrack => ActiveSlot.PrimaryTrack;
        internal WeaponTrack SecondaryTrack => ActiveSlot.SecondaryTrack;
        internal SlotLayer<LightSailSO> SailLayer => ActiveSlot.SailLayer;

        // ══════════════════════════════════════════════════════════════
        // Equip / Unequip — Light Sail
        // ══════════════════════════════════════════════════════════════

        internal LightSailSO GetEquippedLightSail() => ActiveSlot.EquippedLightSailSO;

        /// <summary>
        /// Set the SAIL layer column count. Supports BOTH expansion and shrinking.
        /// When shrinking, any sails whose footprint would fall outside the new grid
        /// are evicted from the layer. Valid range: [1, MAX_COLS].
        /// Used by save restore and by debug overrides (StarChartPanel debug field).
        /// </summary>
        internal void SetSailLayerCols(int cols)
        {
            var layer = ActiveSlot.SailLayer;
            int target = cols < 1 ? 1 : cols > SlotLayer<LightSailSO>.MAX_COLS ? SlotLayer<LightSailSO>.MAX_COLS : cols;
            while (layer.Cols < target)
                if (!layer.TryUnlockColumn()) break;
            while (layer.Cols > target)
                if (!layer.TryShrinkColumn()) break;
        }

        /// <summary>
        /// Equip a Light Sail at runtime. Disposes the previous one if any.
        /// Supports optional anchor position for multi-slot SAIL layer.
        /// </summary>
        internal void EquipLightSail(LightSailSO sail, int anchorCol = 0, int anchorRow = 0)
        {
            if (sail == null) return;

            // If the target cell is already occupied by this sail, no-op
            var existing = ActiveSlot.SailLayer.GetAt(anchorCol, anchorRow);
            if (existing != null && !ReferenceEquals(existing, sail))
            {
                // Evict the occupant first
                UnequipLightSail(existing);
            }
            else if (ReferenceEquals(existing, sail))
            {
                return; // already placed here
            }

            // If no free space, evict the first sail
            if (ActiveSlot.SailLayer.FreeSpace <= 0)
                UnequipLightSail();

            bool placed = ActiveSlot.SailLayer.TryPlace(sail, anchorCol, anchorRow);
            if (!placed)
                placed = ActiveSlot.SailLayer.TryEquip(sail); // fallback: first available slot

            if (placed)
            {
                _lightSailRunners[_activeLoadoutIndex] = new LightSailRunner(sail, _context);
                OnLightSailChanged?.Invoke();
            }
        }

        /// <summary> Unequip the current Light Sail. </summary>
        internal void UnequipLightSail()
        {
            var sail = ActiveSlot.EquippedLightSailSO;
            if (sail != null)
                UnequipLightSail(sail);
        }

        /// <summary> Unequip a specific Light Sail from the active loadout. </summary>
        internal void UnequipLightSail(LightSailSO sail)
        {
            if (sail == null) return;
            ActiveSlot.SailLayer.Unequip(sail);
            ref var runner = ref _lightSailRunners[_activeLoadoutIndex];
            if (runner != null)
            {
                runner.Dispose();
                runner = null;
            }
            OnLightSailChanged?.Invoke();
        }

        // ══════════════════════════════════════════════════════════════
        // Equip / Unequip — Satellite
        // ══════════════════════════════════════════════════════════════

        /// <summary> Get the currently equipped Satellite SOs for the specified track. </summary>
        internal IReadOnlyList<SatelliteSO> GetEquippedSatellites(WeaponTrack.TrackId trackId)
        {
            var track = trackId == WeaponTrack.TrackId.Primary
                ? ActiveSlot.PrimaryTrack
                : ActiveSlot.SecondaryTrack;
            return track.EquippedSatelliteSOs;
        }

        /// <summary>
        /// Equip a Satellite at runtime to the specified track. Creates a new SatelliteRunner.
        /// </summary>
        internal void EquipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId)
        {
            if (sat == null) return;

            var track = trackId == WeaponTrack.TrackId.Primary ? ActiveSlot.PrimaryTrack : ActiveSlot.SecondaryTrack;
            var runners = trackId == WeaponTrack.TrackId.Primary
                ? _primarySatRunners[_activeLoadoutIndex]
                : _secondarySatRunners[_activeLoadoutIndex];

            var runner = new SatelliteRunner(sat, _context);
            runners.Add(runner);
            track.EquipSatellite(sat); // uses TryEquip (first available slot)
            OnSatellitesChanged?.Invoke();
        }

        /// <summary>
        /// Equip a Satellite at a specific anchor position in the SAT layer.
        /// </summary>
        internal void EquipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId, int anchorCol, int anchorRow)
        {
            if (sat == null) return;

            var track = trackId == WeaponTrack.TrackId.Primary ? ActiveSlot.PrimaryTrack : ActiveSlot.SecondaryTrack;
            var runners = trackId == WeaponTrack.TrackId.Primary
                ? _primarySatRunners[_activeLoadoutIndex]
                : _secondarySatRunners[_activeLoadoutIndex];

            var runner = new SatelliteRunner(sat, _context);
            runners.Add(runner);
            track.EquipSatellite(sat, anchorCol, anchorRow);
            OnSatellitesChanged?.Invoke();
        }

        /// <summary>
        /// Unequip a specific Satellite at runtime from the specified track.
        /// </summary>
        internal void UnequipSatellite(SatelliteSO sat, WeaponTrack.TrackId trackId)
        {
            if (sat == null) return;

            var track = trackId == WeaponTrack.TrackId.Primary ? ActiveSlot.PrimaryTrack : ActiveSlot.SecondaryTrack;
            var runners = trackId == WeaponTrack.TrackId.Primary
                ? _primarySatRunners[_activeLoadoutIndex]
                : _secondarySatRunners[_activeLoadoutIndex];

            for (int i = runners.Count - 1; i >= 0; i--)
            {
                if (runners[i].Data == sat)
                {
                    runners[i].Dispose();
                    runners.RemoveAt(i);
                    break;
                }
            }

            track.UnequipSatellite(sat);
            OnSatellitesChanged?.Invoke();
        }

        // ══════════════════════════════════════════════════════════════
        // Slot switching
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Switch to a different loadout slot. Disposes old slot's runners and
        /// re-initializes pools for the new slot's tracks.
        /// </summary>
        internal void SwitchLoadout(int index)
        {
            if (index < 0 || index >= _loadouts.Length) return;
            if (index == _activeLoadoutIndex) return;

            // Dispose runners of the current slot
            DisposeSlotRunners(_activeLoadoutIndex);

            _activeLoadoutIndex = index;

            // Re-create runners for the new slot
            RebuildSlotRunners(_activeLoadoutIndex);

            // Ensure object pools are ready for the new slot's tracks
            _initializeAllPools?.Invoke();

            OnLightSailChanged?.Invoke();
            OnSatellitesChanged?.Invoke();
        }

        /// <summary> Dispose all runners for a given slot index. </summary>
        internal void DisposeSlotRunners(int slotIndex)
        {
            ref var sailRunner = ref _lightSailRunners[slotIndex];
            if (sailRunner != null)
            {
                sailRunner.Dispose();
                sailRunner = null;
            }

            var primaryRunners = _primarySatRunners[slotIndex];
            for (int i = primaryRunners.Count - 1; i >= 0; i--)
                primaryRunners[i].Dispose();
            primaryRunners.Clear();

            var secondaryRunners = _secondarySatRunners[slotIndex];
            for (int i = secondaryRunners.Count - 1; i >= 0; i--)
                secondaryRunners[i].Dispose();
            secondaryRunners.Clear();
        }

        /// <summary>
        /// Re-create LightSailRunner and SatelliteRunners for a slot
        /// based on its current equipped SO data.
        /// </summary>
        internal void RebuildSlotRunners(int slotIndex)
        {
            var slot = _loadouts[slotIndex];

            // Light Sail — rebuild runner for the first equipped sail (single runner model)
            var sail = slot.SailLayer.Items.Count > 0 ? slot.SailLayer.Items[0] : null;
            if (sail != null)
                _lightSailRunners[slotIndex] = new LightSailRunner(sail, _context);

            // Primary track satellites
            var primaryRunners = _primarySatRunners[slotIndex];
            for (int i = 0; i < slot.PrimaryTrack.EquippedSatelliteSOs.Count; i++)
            {
                var sat = slot.PrimaryTrack.EquippedSatelliteSOs[i];
                if (sat != null)
                    primaryRunners.Add(new SatelliteRunner(sat, _context));
            }

            // Secondary track satellites
            var secondaryRunners = _secondarySatRunners[slotIndex];
            for (int i = 0; i < slot.SecondaryTrack.EquippedSatelliteSOs.Count; i++)
            {
                var sat = slot.SecondaryTrack.EquippedSatelliteSOs[i];
                if (sat != null)
                    secondaryRunners.Add(new SatelliteRunner(sat, _context));
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Tick / Dispose
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tick the active slot's tracks and runners. Called once per frame by
        /// <see cref="StarChartController.Update"/> after input guard passes.
        /// </summary>
        internal void TickActive(float dt)
        {
            ActiveSlot.PrimaryTrack.Tick(dt);
            ActiveSlot.SecondaryTrack.Tick(dt);

            _lightSailRunners[_activeLoadoutIndex]?.Tick(dt);

            var activePrimaryRunners = _primarySatRunners[_activeLoadoutIndex];
            for (int i = 0; i < activePrimaryRunners.Count; i++)
                activePrimaryRunners[i]?.Tick(dt);

            var activeSecondaryRunners = _secondarySatRunners[_activeLoadoutIndex];
            for (int i = 0; i < activeSecondaryRunners.Count; i++)
                activeSecondaryRunners[i]?.Tick(dt);
        }

        /// <summary>
        /// Dispose all runners across all slots. Called by
        /// <see cref="StarChartController.OnDestroy"/>.
        /// </summary>
        internal void Dispose()
        {
            OnLightSailChanged = null;
            OnSatellitesChanged = null;

            for (int i = 0; i < _lightSailRunners.Length; i++)
                _lightSailRunners[i]?.Dispose();

            for (int i = 0; i < _primarySatRunners.Length; i++)
            {
                if (_primarySatRunners[i] == null) continue;
                for (int j = 0; j < _primarySatRunners[i].Count; j++)
                    _primarySatRunners[i][j].Dispose();
                _primarySatRunners[i].Clear();
            }

            for (int i = 0; i < _secondarySatRunners.Length; i++)
            {
                if (_secondarySatRunners[i] == null) continue;
                for (int j = 0; j < _secondarySatRunners[i].Count; j++)
                    _secondarySatRunners[i][j].Dispose();
                _secondarySatRunners[i].Clear();
            }
        }
    }
}
