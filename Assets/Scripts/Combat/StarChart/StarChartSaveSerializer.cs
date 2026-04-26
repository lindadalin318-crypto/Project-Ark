using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core.Save;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Handles Star Chart save/load serialization. Extracted from
    /// <see cref="StarChartController"/> as part of L3-1 Phase A.
    /// </summary>
    /// <remarks>
    /// Pure C# class — not a MonoBehaviour. Constructed by the owning
    /// <see cref="StarChartController"/> in <c>Awake</c> with references to its
    /// runtime state arrays. Lifetime is tied to the controller.
    ///
    /// <para>Behavior is a direct port of the previous <c>ExportToSaveData</c> /
    /// <c>ImportFromSaveData</c> implementation and must remain byte-for-byte
    /// identical on the save format side — including the four legacy migration
    /// branches documented below.</para>
    ///
    /// <para><b>Legacy migration branches preserved verbatim:</b></para>
    /// <list type="number">
    /// <item>New format: <c>data.Loadouts</c> is non-empty → use directly.</item>
    /// <item>Legacy single-slot: old field group (<c>PrimaryTrack</c> / <c>SecondaryTrack</c>
    /// / <c>LightSailID</c> / <c>SatelliteIDs</c>) is migrated into slot 0.</item>
    /// <item>Legacy slot-level satellites: <c>slotData.SatelliteIDs</c> present →
    /// migrated into the Primary track with a <c>LogWarning</c>.</item>
    /// <item>New per-track satellites: <c>slotData.SatelliteIDs</c> absent →
    /// read <c>TrackSaveData.SatelliteIDs</c>.</item>
    /// </list>
    ///
    /// <para><b>Known carry-over gap (NOT fixed here):</b> SAIL / Core / Prism / SAT
    /// layer <c>Rows</c> are persisted on <see cref="TrackSaveData"/> and
    /// <see cref="LoadoutSlotSaveData"/> but were never written or read by the
    /// original Controller code. This Phase A extraction preserves that behavior
    /// unchanged; fixing the Rows round-trip is tracked separately and is out of
    /// scope for L3-1 Phase A.</para>
    /// </remarks>
    internal sealed class StarChartSaveSerializer
    {
        private readonly LoadoutSlot[] _loadouts;
        private readonly LightSailRunner[] _lightSailRunners;
        private readonly List<SatelliteRunner>[] _primarySatRunners;
        private readonly List<SatelliteRunner>[] _secondarySatRunners;
        private readonly StarChartContext _context;

        // Controller-owned side-effects re-exposed as callbacks. Keeps this
        // serializer a pure C# class without a back-reference to the Controller.
        private readonly Action<int> _disposeSlotRunners;
        private readonly Action _initializeAllPools;

        internal StarChartSaveSerializer(
            LoadoutSlot[] loadouts,
            LightSailRunner[] lightSailRunners,
            List<SatelliteRunner>[] primarySatRunners,
            List<SatelliteRunner>[] secondarySatRunners,
            StarChartContext context,
            Action<int> disposeSlotRunners,
            Action initializeAllPools)
        {
            _loadouts = loadouts ?? throw new ArgumentNullException(nameof(loadouts));
            _lightSailRunners = lightSailRunners ?? throw new ArgumentNullException(nameof(lightSailRunners));
            _primarySatRunners = primarySatRunners ?? throw new ArgumentNullException(nameof(primarySatRunners));
            _secondarySatRunners = secondarySatRunners ?? throw new ArgumentNullException(nameof(secondarySatRunners));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _disposeSlotRunners = disposeSlotRunners ?? throw new ArgumentNullException(nameof(disposeSlotRunners));
            _initializeAllPools = initializeAllPools ?? throw new ArgumentNullException(nameof(initializeAllPools));
        }

        // ══════════════════════════════════════════════════════════════
        // Export
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Export the current Star Chart loadout to a serializable data object.
        /// Serializes all 3 loadout slots.
        /// </summary>
        internal StarChartSaveData Export()
        {
            var data = new StarChartSaveData();
            data.Loadouts = new List<LoadoutSlotSaveData>();

            for (int i = 0; i < _loadouts.Length; i++)
            {
                var slot = _loadouts[i];
                var slotData = new LoadoutSlotSaveData();
                slotData.PrimaryTrack   = ExportTrack(slot.PrimaryTrack);
                slotData.SecondaryTrack = ExportTrack(slot.SecondaryTrack);
                slotData.LightSailID = slot.EquippedLightSailSO != null ? slot.EquippedLightSailSO.DisplayName : "";
                slotData.SailLayerCols = slot.SailLayer.Cols;
                // Satellites are now stored per-track inside TrackSaveData
                data.Loadouts.Add(slotData);
            }

            return data;
        }

        // ══════════════════════════════════════════════════════════════
        // Import
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Import a Star Chart loadout from saved data, using a resolver to look up items by name.
        /// Supports both new multi-slot format and legacy single-slot format (auto-migration).
        /// </summary>
        internal void Import(StarChartSaveData data, IStarChartItemResolver resolver)
        {
            if (data == null || resolver == null) return;

            // Dispose all existing runners across all slots
            for (int i = 0; i < _loadouts.Length; i++)
                _disposeSlotRunners(i);

            // Clear all slot data
            for (int i = 0; i < _loadouts.Length; i++)
                _loadouts[i].Clear();

            List<LoadoutSlotSaveData> slotDataList;

#pragma warning disable CS0618 // Obsolete field access for migration
            if (data.Loadouts != null && data.Loadouts.Count > 0)
            {
                // New format: use Loadouts list directly
                slotDataList = data.Loadouts;
            }
            else
            {
                // Legacy format: migrate single-slot data to slot 0
                slotDataList = new List<LoadoutSlotSaveData>
                {
                    new LoadoutSlotSaveData
                    {
                        PrimaryTrack   = data.PrimaryTrack,
                        SecondaryTrack = data.SecondaryTrack,
                        LightSailID    = data.LightSailID,
                        SatelliteIDs   = data.SatelliteIDs ?? new List<string>()
                    }
                };
            }
#pragma warning restore CS0618

            // Import each slot (pad with empty slots if list is shorter than 3)
            for (int i = 0; i < _loadouts.Length; i++)
            {
                if (i >= slotDataList.Count) break; // remaining slots stay empty

                var slotData = slotDataList[i];
                if (slotData == null) continue;

                var slot = _loadouts[i];
                ImportTrack(slot.PrimaryTrack,   slotData.PrimaryTrack,   resolver);
                ImportTrack(slot.SecondaryTrack, slotData.SecondaryTrack, resolver);

                if (!string.IsNullOrEmpty(slotData.LightSailID))
                {
                    var sail = resolver.FindLightSail(slotData.LightSailID);
                    if (sail != null)
                    {
                        // Restore SAIL layer column count before equipping
                        int sailCols = Math.Max(1, slotData.SailLayerCols);
                        while (slot.SailLayer.Cols < sailCols)
                            slot.SailLayer.TryUnlockColumn();

                        slot.EquippedLightSailSO = sail;
                        _lightSailRunners[i] = new LightSailRunner(sail, _context);
                    }
                }

#pragma warning disable CS0618 // Obsolete field access for legacy migration
                // --- Satellite migration: old saves stored SatelliteIDs at slot level ---
                if (slotData.SatelliteIDs != null && slotData.SatelliteIDs.Count > 0)
                {
                    // Legacy format: migrate all satellites to Primary track
                    Debug.LogWarning("[StarChartSaveSerializer] Migrating legacy slot-level SatelliteIDs to PrimaryTrack.");
                    for (int j = 0; j < slotData.SatelliteIDs.Count; j++)
                    {
                        var sat = resolver.FindSatellite(slotData.SatelliteIDs[j]);
                        if (sat != null)
                        {
                            slot.PrimaryTrack.EquipSatellite(sat);
                            _primarySatRunners[i].Add(new SatelliteRunner(sat, _context));
                        }
                        else
                        {
                            Debug.LogWarning($"[StarChartSaveSerializer] Cannot resolve legacy satellite ID '{slotData.SatelliteIDs[j]}', skipping.");
                        }
                    }
                }
                else
                {
                    // New format: load per-track satellite IDs from TrackSaveData
                    ImportTrackSatellites(slot.PrimaryTrack,   slotData.PrimaryTrack,   _primarySatRunners[i],   resolver);
                    ImportTrackSatellites(slot.SecondaryTrack, slotData.SecondaryTrack, _secondarySatRunners[i], resolver);
                }
#pragma warning restore CS0618
            }

            // Re-initialize pools for the active loadout
            _initializeAllPools();
        }

        // ══════════════════════════════════════════════════════════════
        // Track helpers
        // ══════════════════════════════════════════════════════════════

        private static TrackSaveData ExportTrack(WeaponTrack track)
        {
            var data = new TrackSaveData();

            var cores = track.CoreLayer.Items;
            for (int i = 0; i < cores.Count; i++)
            {
                if (cores[i] != null)
                    data.CoreIDs.Add(cores[i].DisplayName);
            }

            var prisms = track.PrismLayer.Items;
            for (int i = 0; i < prisms.Count; i++)
            {
                if (prisms[i] != null)
                    data.PrismIDs.Add(prisms[i].DisplayName);
            }

            // Persist per-track satellite IDs
            var sats = track.EquippedSatelliteSOs;
            for (int i = 0; i < sats.Count; i++)
            {
                if (sats[i] != null)
                    data.SatelliteIDs.Add(sats[i].DisplayName);
            }

            // Persist unlocked column counts for progressive capacity system
            data.CoreLayerCols  = track.CoreLayer.Cols;
            data.PrismLayerCols = track.PrismLayer.Cols;
            data.SatLayerCols   = track.SatLayer.Cols;

            return data;
        }

        private static void ImportTrack(WeaponTrack track, TrackSaveData data,
                                         IStarChartItemResolver resolver)
        {
            if (data == null) return;

            // Restore unlocked column counts (clamp to ≥1 for old saves where field defaults to 0)
            int coreCols  = Mathf.Max(1, data.CoreLayerCols);
            int prismCols = Mathf.Max(1, data.PrismLayerCols);
            int satCols   = Mathf.Max(1, data.SatLayerCols);
            track.SetLayerCols(coreCols, prismCols, satCols);

            if (data.CoreIDs != null)
            {
                for (int i = 0; i < data.CoreIDs.Count; i++)
                {
                    var core = resolver.FindCore(data.CoreIDs[i]);
                    if (core != null) track.EquipCore(core);
                }
            }

            if (data.PrismIDs != null)
            {
                for (int i = 0; i < data.PrismIDs.Count; i++)
                {
                    var prism = resolver.FindPrism(data.PrismIDs[i]);
                    if (prism != null) track.EquipPrism(prism);
                }
            }
            // Note: Satellite IDs are imported separately via ImportTrackSatellites
            // to allow runner creation alongside data restoration.
        }

        /// <summary>
        /// Import satellite IDs from TrackSaveData into the given WeaponTrack,
        /// and create corresponding SatelliteRunners.
        /// Unresolvable IDs are skipped with a warning (no exception thrown).
        /// </summary>
        private void ImportTrackSatellites(WeaponTrack track, TrackSaveData data,
                                            List<SatelliteRunner> runners,
                                            IStarChartItemResolver resolver)
        {
            if (data?.SatelliteIDs == null) return;
            for (int i = 0; i < data.SatelliteIDs.Count; i++)
            {
                var sat = resolver.FindSatellite(data.SatelliteIDs[i]);
                if (sat != null)
                {
                    track.EquipSatellite(sat);
                    runners.Add(new SatelliteRunner(sat, _context));
                }
                else
                {
                    Debug.LogWarning($"[StarChartSaveSerializer] Cannot resolve satellite ID '{data.SatelliteIDs[i]}', skipping.");
                }
            }
        }
    }
}
