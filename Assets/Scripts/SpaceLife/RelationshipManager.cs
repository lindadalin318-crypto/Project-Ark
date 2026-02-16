
using System;
using System.Collections.Generic;
using ProjectArk.SpaceLife.Data;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance { get; private set; }

        [Serializable]
        public class NPCRelationship
        {
            public NPCDataSO npcData;
            public int relationshipValue;
        }

        [Header("Data")]
        [SerializeField] private List<NPCRelationship> _relationships = new List<NPCRelationship>();

        public event Action<NPCDataSO, int> OnRelationshipChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public int GetRelationship(NPCDataSO npcData)
        {
            foreach (var relationship in _relationships)
            {
                if (relationship.npcData == npcData)
                {
                    return relationship.relationshipValue;
                }
            }

            if (npcData != null)
            {
                return npcData.startingRelationship;
            }

            return 0;
        }

        public void SetRelationship(NPCDataSO npcData, int value)
        {
            if (npcData == null) return;

            value = Mathf.Clamp(value, 0, 100);

            foreach (var relationship in _relationships)
            {
                if (relationship.npcData == npcData)
                {
                    relationship.relationshipValue = value;
                    OnRelationshipChanged?.Invoke(npcData, value);
                    return;
                }
            }

            _relationships.Add(new NPCRelationship
            {
                npcData = npcData,
                relationshipValue = value
            });

            OnRelationshipChanged?.Invoke(npcData, value);
        }

        public void ChangeRelationship(NPCDataSO npcData, int delta)
        {
            int currentValue = GetRelationship(npcData);
            SetRelationship(npcData, currentValue + delta);
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    public enum RelationshipLevel
    {
        Stranger,
        Acquainted,
        Friend,
        BestFriend
    }
}

