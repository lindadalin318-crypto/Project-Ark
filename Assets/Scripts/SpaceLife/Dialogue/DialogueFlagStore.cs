using System;
using System.Collections.Generic;
using ProjectArk.Core.Save;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Wrapper around save-data flags used by the dialogue runtime.
    /// </summary>
    public sealed class DialogueFlagStore
    {
        private readonly PlayerSaveData _saveData;

        public DialogueFlagStore(PlayerSaveData saveData)
        {
            _saveData = saveData ?? new PlayerSaveData();
            _saveData.Progress ??= new ProgressSaveData();
            _saveData.Progress.Flags ??= new List<SaveFlag>();
        }

        public PlayerSaveData SaveData => _saveData;

        public bool Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            List<SaveFlag> flags = _saveData.Progress.Flags;
            for (int i = 0; i < flags.Count; i++)
            {
                if (string.Equals(flags[i].Key, key, StringComparison.Ordinal))
                {
                    return flags[i].Value;
                }
            }

            return false;
        }

        public void Set(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            List<SaveFlag> flags = _saveData.Progress.Flags;
            int foundIndex = -1;
            for (int i = flags.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(flags[i].Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                if (foundIndex < 0)
                {
                    foundIndex = i;
                    flags[i].Value = true;
                    continue;
                }

                flags.RemoveAt(i);
            }

            if (foundIndex >= 0)
            {
                return;
            }

            flags.Add(new SaveFlag(key, true));
        }

        public void Clear(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            List<SaveFlag> flags = _saveData.Progress.Flags;
            for (int i = flags.Count - 1; i >= 0; i--)
            {
                if (string.Equals(flags[i].Key, key, StringComparison.Ordinal))
                {
                    flags.RemoveAt(i);
                }
            }
        }

        public IReadOnlyCollection<string> GetActiveFlagKeys()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            List<SaveFlag> flags = _saveData.Progress.Flags;
            for (int i = 0; i < flags.Count; i++)
            {
                SaveFlag flag = flags[i];
                if (flag != null && flag.Value && !string.IsNullOrWhiteSpace(flag.Key))
                {
                    keys.Add(flag.Key);
                }
            }

            return keys;
        }
    }
}
