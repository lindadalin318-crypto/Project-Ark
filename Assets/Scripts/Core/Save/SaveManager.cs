using System;
using System.IO;
using UnityEngine;

namespace ProjectArk.Core.Save
{
    /// <summary>
    /// Static utility for serializing/deserializing <see cref="PlayerSaveData"/>
    /// to JSON files in Application.persistentDataPath.
    /// Supports multiple save slots and automatic backup.
    /// </summary>
    public static class SaveManager
    {
        private const string SAVE_FOLDER = "Saves";
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string SAVE_EXTENSION = ".json";
        private const string BACKUP_EXTENSION = ".bak";

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Save player data to a specific slot.
        /// Creates a backup of the previous save before overwriting.
        /// </summary>
        public static void Save(PlayerSaveData data, int slotIndex)
        {
            if (data == null)
            {
                Debug.LogError("[SaveManager] Cannot save null data.");
                return;
            }

            // Stamp the save time
            data.Timestamp = DateTime.UtcNow.ToString("o");

            string path = GetSavePath(slotIndex);
            string directory = Path.GetDirectoryName(path);

            // Ensure directory exists
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Create backup of existing save
            if (File.Exists(path))
            {
                string backupPath = path + BACKUP_EXTENSION;
                try
                {
                    File.Copy(path, backupPath, overwrite: true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Failed to create backup: {e.Message}");
                }
            }

            // Serialize and write
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Saved to slot {slotIndex}: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load player data from a specific slot.
        /// Returns null if the save file does not exist or is corrupted.
        /// </summary>
        public static PlayerSaveData Load(int slotIndex)
        {
            string path = GetSavePath(slotIndex);

            if (!File.Exists(path))
            {
                Debug.Log($"[SaveManager] No save found at slot {slotIndex}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log($"[SaveManager] Loaded slot {slotIndex} (version: {data.SaveVersion})");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load slot {slotIndex}: {e.Message}");

                // Try loading from backup
                return TryLoadBackup(slotIndex);
            }
        }

        /// <summary>
        /// Delete a save slot (and its backup).
        /// </summary>
        public static void Delete(int slotIndex)
        {
            string path = GetSavePath(slotIndex);
            string backupPath = path + BACKUP_EXTENSION;

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                Debug.Log($"[SaveManager] Deleted slot {slotIndex}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to delete slot {slotIndex}: {e.Message}");
            }
        }

        /// <summary>
        /// Check if a save file exists for the given slot.
        /// </summary>
        public static bool HasSave(int slotIndex)
        {
            return File.Exists(GetSavePath(slotIndex));
        }

        // ──────────────────── Internals ────────────────────

        private static string GetSavePath(int slotIndex)
        {
            return Path.Combine(
                Application.persistentDataPath,
                SAVE_FOLDER,
                $"{SAVE_FILE_PREFIX}{slotIndex}{SAVE_EXTENSION}");
        }

        private static PlayerSaveData TryLoadBackup(int slotIndex)
        {
            string backupPath = GetSavePath(slotIndex) + BACKUP_EXTENSION;

            if (!File.Exists(backupPath))
            {
                Debug.LogWarning($"[SaveManager] No backup found for slot {slotIndex}.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(backupPath);
                var data = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log($"[SaveManager] Recovered from backup for slot {slotIndex}.");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Backup also corrupted for slot {slotIndex}: {e.Message}");
                return null;
            }
        }
    }
}
