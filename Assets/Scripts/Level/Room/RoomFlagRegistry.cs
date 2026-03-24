using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Generic room-level persistent flag system.
    /// Tracks fine-grained spatial state: "this hidden area was discovered",
    /// "this torch was lit", "this destroyable was smashed",
    /// "this specific door was permanently opened".
    ///
    /// Persisted via <see cref="SaveBridge"/> ↔ <see cref="ProgressSaveData.Flags"/>
    /// using key format: room_{roomID}_{flagKey}.
    ///
    /// Registers with <see cref="ServiceLocator"/> in Awake.
    /// </summary>
    public class RoomFlagRegistry : MonoBehaviour
    {
        // ──────────────────── Constants ────────────────────

        private const string FLAG_PREFIX = "room_";

        // ──────────────────── Runtime State ────────────────────

        // Nested dictionary: roomID → { flagKey → value }
        private readonly Dictionary<string, Dictionary<string, bool>> _flags = new();

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Set a persistent flag for a room. Broadcasts <see cref="LevelEvents.OnRoomFlagChanged"/>.
        /// </summary>
        /// <param name="roomID">Room identifier (from RoomSO.RoomID).</param>
        /// <param name="flagKey">Flag name within the room (e.g., "crystal_wall_01").</param>
        /// <param name="value">True to set, false to clear.</param>
        public void SetFlag(string roomID, string flagKey, bool value = true)
        {
            if (string.IsNullOrEmpty(roomID) || string.IsNullOrEmpty(flagKey))
            {
                Debug.LogError("[RoomFlagRegistry] SetFlag called with null/empty roomID or flagKey.");
                return;
            }

            if (!_flags.TryGetValue(roomID, out var roomFlags))
            {
                roomFlags = new Dictionary<string, bool>();
                _flags[roomID] = roomFlags;
            }

            // Only broadcast if value actually changed
            bool oldValue = roomFlags.TryGetValue(flagKey, out bool existing) && existing;
            if (oldValue == value) return;

            roomFlags[flagKey] = value;

            LevelEvents.RaiseRoomFlagChanged(roomID, flagKey, value);

            Debug.Log($"[RoomFlagRegistry] {roomID}/{flagKey} = {value}");
        }

        /// <summary>
        /// Query a flag value. Returns false if flag was never set.
        /// </summary>
        public bool GetFlag(string roomID, string flagKey)
        {
            if (string.IsNullOrEmpty(roomID) || string.IsNullOrEmpty(flagKey)) return false;

            if (_flags.TryGetValue(roomID, out var roomFlags))
            {
                return roomFlags.TryGetValue(flagKey, out bool value) && value;
            }

            return false;
        }

        /// <summary>
        /// Get all flags set for a specific room. Returns empty dictionary if none.
        /// </summary>
        public IReadOnlyDictionary<string, bool> GetRoomFlags(string roomID)
        {
            if (!string.IsNullOrEmpty(roomID) && _flags.TryGetValue(roomID, out var roomFlags))
            {
                return roomFlags;
            }

            return _emptyFlags;
        }

        private static readonly Dictionary<string, bool> _emptyFlags = new();

        // ──────────────────── Save / Load ────────────────────

        /// <summary>
        /// Write all room flags into <see cref="ProgressSaveData.Flags"/>.
        /// Key format: room_{roomID}_{flagKey}.
        /// Called by <see cref="SaveBridge.CollectProgressData"/>.
        /// </summary>
        public void WriteToSaveData(ProgressSaveData data)
        {
            if (data == null) return;

            // Remove old room flags
            data.Flags.RemoveAll(f => f.Key.StartsWith(FLAG_PREFIX));

            // Write current flags
            foreach (var (roomID, roomFlags) in _flags)
            {
                foreach (var (flagKey, value) in roomFlags)
                {
                    if (value) // Only persist true flags to save space
                    {
                        data.Flags.Add(new SaveFlag($"{FLAG_PREFIX}{roomID}_{flagKey}", true));
                    }
                }
            }
        }

        /// <summary>
        /// Restore room flags from <see cref="ProgressSaveData.Flags"/>.
        /// Called by <see cref="SaveBridge.DistributeProgressData"/>.
        /// </summary>
        public void ReadFromSaveData(ProgressSaveData data)
        {
            _flags.Clear();

            if (data == null) return;

            int count = 0;
            foreach (var flag in data.Flags)
            {
                if (!flag.Key.StartsWith(FLAG_PREFIX) || !flag.Value) continue;

                // Parse: "room_{roomID}_{flagKey}"
                // We need to split after the prefix to get roomID and flagKey.
                // Format: room_SH-R01_crystal_wall_01
                // Strategy: roomID is up to the first '_' after "room_",
                // but roomID itself can contain '-'. flagKey is everything after roomID + '_'.
                // Since roomIDs are well-defined (e.g., "SH-R01") and don't contain '_',
                // we split on the SECOND '_' after prefix removal.
                string remainder = flag.Key.Substring(FLAG_PREFIX.Length); // "SH-R01_crystal_wall_01"
                int separatorIndex = remainder.IndexOf('_');
                if (separatorIndex <= 0 || separatorIndex >= remainder.Length - 1) continue;

                string roomID = remainder.Substring(0, separatorIndex);
                string flagKey = remainder.Substring(separatorIndex + 1);

                if (!_flags.TryGetValue(roomID, out var roomFlags))
                {
                    roomFlags = new Dictionary<string, bool>();
                    _flags[roomID] = roomFlags;
                }

                roomFlags[flagKey] = true;
                count++;
            }

            Debug.Log($"[RoomFlagRegistry] Loaded {count} room flag(s) from save data.");
        }
    }
}
