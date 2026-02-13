using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Save;

namespace ProjectArk.Level
{
    /// <summary>
    /// Tracks which key items the player currently holds.
    /// Registers with ServiceLocator. Persists keys to SaveManager via ProgressSaveData.Flags.
    /// </summary>
    public class KeyInventory : MonoBehaviour
    {
        // ──────────────────── Constants ────────────────────

        private const string KEY_FLAG_PREFIX = "key_";

        // ──────────────────── Runtime State ────────────────────

        private readonly HashSet<string> _ownedKeyIDs = new();

        // ──────────────────── Events ────────────────────

        /// <summary> Fired when a key is added. Param: keyID. </summary>
        public event Action<string> OnKeyAdded;

        /// <summary> Fired when a key is removed (consumed by a lock). Param: keyID. </summary>
        public event Action<string> OnKeyRemoved;

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
        /// Add a key to the inventory.
        /// </summary>
        public void AddKey(string keyID)
        {
            if (string.IsNullOrEmpty(keyID)) return;

            if (_ownedKeyIDs.Add(keyID))
            {
                OnKeyAdded?.Invoke(keyID);
                Debug.Log($"[KeyInventory] Added key: {keyID}");
            }
        }

        /// <summary>
        /// Check if the player owns the specified key.
        /// </summary>
        public bool HasKey(string keyID)
        {
            return !string.IsNullOrEmpty(keyID) && _ownedKeyIDs.Contains(keyID);
        }

        /// <summary>
        /// Remove a key from the inventory (e.g., consumed when unlocking a door).
        /// </summary>
        public void RemoveKey(string keyID)
        {
            if (string.IsNullOrEmpty(keyID)) return;

            if (_ownedKeyIDs.Remove(keyID))
            {
                OnKeyRemoved?.Invoke(keyID);
                Debug.Log($"[KeyInventory] Removed key: {keyID}");
            }
        }

        /// <summary>
        /// Get a read-only view of all owned key IDs.
        /// </summary>
        public IReadOnlyCollection<string> GetOwnedKeys() => _ownedKeyIDs;

        // ──────────────────── Save/Load ────────────────────

        /// <summary>
        /// Write owned keys into save data flags.
        /// Call before SaveManager.Save().
        /// </summary>
        public void WriteToSaveData(ProgressSaveData progress)
        {
            if (progress == null) return;

            // Remove old key flags
            progress.Flags.RemoveAll(f => f.Key.StartsWith(KEY_FLAG_PREFIX));

            // Add current keys
            foreach (var keyID in _ownedKeyIDs)
            {
                progress.Flags.Add(new SaveFlag($"{KEY_FLAG_PREFIX}{keyID}", true));
            }
        }

        /// <summary>
        /// Load owned keys from save data flags.
        /// Call after SaveManager.Load().
        /// </summary>
        public void ReadFromSaveData(ProgressSaveData progress)
        {
            _ownedKeyIDs.Clear();

            if (progress == null) return;

            foreach (var flag in progress.Flags)
            {
                if (flag.Key.StartsWith(KEY_FLAG_PREFIX) && flag.Value)
                {
                    string keyID = flag.Key.Substring(KEY_FLAG_PREFIX.Length);
                    _ownedKeyIDs.Add(keyID);
                }
            }

            Debug.Log($"[KeyInventory] Loaded {_ownedKeyIDs.Count} keys from save data.");
        }
    }
}
