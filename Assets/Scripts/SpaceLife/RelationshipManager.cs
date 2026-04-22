
using System;
using System.Collections.Generic;
using ProjectArk.Core;
using ProjectArk.Core.Save;
using ProjectArk.SpaceLife.Data;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class RelationshipManager : MonoBehaviour
    {
        [Serializable]
        public class NPCRelationship
        {
            public string NpcId;
            public int RelationshipValue;
        }

        [Header("Save")]
        [SerializeField] private int _saveSlot = 0;

        [Header("Data")]
        [SerializeField] private List<NPCRelationship> _relationships = new();

        public event Action<NPCDataSO, int> OnRelationshipChanged;

        private readonly Dictionary<string, NPCRelationship> _relationshipsByNpcId = new(StringComparer.Ordinal);
        private bool _hasLoadedFromSave;

        private void Awake()
        {
            RebuildLookupFromList();
            ServiceLocator.Register(this);
        }

        public bool HasRelationshipRecord(NPCDataSO npcData)
        {
            EnsureLoadedFromSave();
            string npcId = ResolveNpcId(npcData);
            return !string.IsNullOrWhiteSpace(npcId) && _relationshipsByNpcId.ContainsKey(npcId);
        }

        public int GetRelationship(NPCDataSO npcData)
        {
            EnsureLoadedFromSave();
            string npcId = ResolveNpcId(npcData);
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return 0;
            }

            if (_relationshipsByNpcId.TryGetValue(npcId, out NPCRelationship relationship))
            {
                return relationship.RelationshipValue;
            }

            return npcData != null ? npcData.StartingRelationship : 0;
        }

        public void SetRelationship(NPCDataSO npcData, int value)
        {
            EnsureLoadedFromSave();
            string npcId = ResolveNpcId(npcData);
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return;
            }

            value = Mathf.Clamp(value, 0, 100);
            GetOrCreateRelationship(npcId).RelationshipValue = value;

            PersistCurrentRelationships();
            OnRelationshipChanged?.Invoke(npcData, value);
        }

        public void ChangeRelationship(NPCDataSO npcData, int delta)
        {
            int currentValue = GetRelationship(npcData);
            SetRelationship(npcData, currentValue + delta);
        }

        public void LoadFromSave()
        {
            LoadFromSaveData(SaveManager.Load(_saveSlot));
        }

        public void LoadFromSaveData(PlayerSaveData data)
        {
            _relationships.Clear();
            _relationshipsByNpcId.Clear();

            List<RelationshipValueSaveData> savedValues = data?.Progress?.RelationshipValues;
            if (savedValues != null)
            {
                for (int i = 0; i < savedValues.Count; i++)
                {
                    RelationshipValueSaveData entry = savedValues[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.NpcId))
                    {
                        continue;
                    }

                    GetOrCreateRelationship(entry.NpcId).RelationshipValue = Mathf.Clamp(entry.Value, 0, 100);
                }
            }

            _hasLoadedFromSave = true;
        }

        public void SaveToSaveData(PlayerSaveData data)
        {
            EnsureLoadedFromSave();
            if (data == null)
            {
                return;
            }

            data.Progress ??= new ProgressSaveData();
            data.Progress.RelationshipValues ??= new List<RelationshipValueSaveData>();
            data.Progress.RelationshipValues.Clear();

            for (int i = 0; i < _relationships.Count; i++)
            {
                NPCRelationship relationship = _relationships[i];
                if (relationship == null || string.IsNullOrWhiteSpace(relationship.NpcId))
                {
                    continue;
                }

                data.Progress.RelationshipValues.Add(new RelationshipValueSaveData(
                    relationship.NpcId,
                    Mathf.Clamp(relationship.RelationshipValue, 0, 100)));
            }
        }

        public RelationshipLevel GetRelationshipLevel(NPCDataSO npcData)
        {
            int value = GetRelationship(npcData);

            if (value >= 80)
                return RelationshipLevel.BestFriend;
            if (value >= 50)
                return RelationshipLevel.Friend;
            if (value >= 20)
                return RelationshipLevel.Acquainted;

            return RelationshipLevel.Stranger;
        }

        public string GetRelationshipLevelName(NPCDataSO npcData)
        {
            return GetRelationshipLevel(npcData).ToString();
        }

        public float GetRelationshipProgress(NPCDataSO npcData)
        {
            int value = GetRelationship(npcData);
            RelationshipLevel level = GetRelationshipLevel(npcData);

            int minValue, maxValue;

            switch (level)
            {
                case RelationshipLevel.Stranger:
                    minValue = 0;
                    maxValue = 20;
                    break;
                case RelationshipLevel.Acquainted:
                    minValue = 20;
                    maxValue = 50;
                    break;
                case RelationshipLevel.Friend:
                    minValue = 50;
                    maxValue = 80;
                    break;
                case RelationshipLevel.BestFriend:
                    minValue = 80;
                    maxValue = 100;
                    break;
                default:
                    return 0f;
            }

            return Mathf.InverseLerp(minValue, maxValue, value);
        }

        private void EnsureLoadedFromSave()
        {
            if (_hasLoadedFromSave)
            {
                return;
            }

            LoadFromSave();
        }

        private void PersistCurrentRelationships()
        {
            var data = SaveManager.Load(_saveSlot) ?? new PlayerSaveData();
            SaveToSaveData(data);
            SaveManager.Save(data, _saveSlot);
        }

        private NPCRelationship GetOrCreateRelationship(string npcId)
        {
            if (_relationshipsByNpcId.TryGetValue(npcId, out NPCRelationship relationship))
            {
                return relationship;
            }

            relationship = new NPCRelationship
            {
                NpcId = npcId,
                RelationshipValue = 0,
            };

            _relationships.Add(relationship);
            _relationshipsByNpcId.Add(npcId, relationship);
            return relationship;
        }

        private void RebuildLookupFromList()
        {
            _relationshipsByNpcId.Clear();

            var normalized = new List<NPCRelationship>();
            for (int i = 0; i < _relationships.Count; i++)
            {
                NPCRelationship relationship = _relationships[i];
                if (relationship == null || string.IsNullOrWhiteSpace(relationship.NpcId))
                {
                    continue;
                }

                if (_relationshipsByNpcId.TryGetValue(relationship.NpcId, out NPCRelationship existing))
                {
                    existing.RelationshipValue = Mathf.Clamp(relationship.RelationshipValue, 0, 100);
                    continue;
                }

                relationship.RelationshipValue = Mathf.Clamp(relationship.RelationshipValue, 0, 100);
                _relationshipsByNpcId.Add(relationship.NpcId, relationship);
                normalized.Add(relationship);
            }

            _relationships.Clear();
            _relationships.AddRange(normalized);
        }

        private static string ResolveNpcId(NPCDataSO npcData)
        {
            if (npcData == null)
            {
                return string.Empty;
            }

            string npcId = npcData.NpcId;
            if (string.IsNullOrWhiteSpace(npcId))
            {
                Debug.LogError($"[RelationshipManager] NPC '{npcData.name}' is missing NpcId.", npcData);
                return string.Empty;
            }

            return npcId;
        }

        private void OnDestroy()
        {
            OnRelationshipChanged = null;
            ServiceLocator.Unregister(this);
        }
    }
}

