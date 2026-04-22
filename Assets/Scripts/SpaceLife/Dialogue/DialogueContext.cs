using System;
using System.Collections.Generic;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Runtime dialogue context snapshot used by pure logic systems.
    /// </summary>
    public sealed class DialogueContext
    {
        private readonly HashSet<string> _flags;

        public DialogueContext(string ownerId, int worldStage, int relationshipValue, IEnumerable<string> flags = null)
        {
            OwnerId = ownerId;
            WorldStage = worldStage;
            RelationshipValue = ClampRelationship(relationshipValue);
            _flags = flags != null
                ? new HashSet<string>(flags, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        public string OwnerId { get; }
        public int WorldStage { get; }
        public int RelationshipValue { get; private set; }
        public IReadOnlyCollection<string> Flags => _flags;

        public bool HasFlag(string flagKey)
        {
            return !string.IsNullOrWhiteSpace(flagKey) && _flags.Contains(flagKey);
        }

        public void SetFlag(string flagKey)
        {
            if (string.IsNullOrWhiteSpace(flagKey))
            {
                return;
            }

            _flags.Add(flagKey);
        }

        public void ClearFlag(string flagKey)
        {
            if (string.IsNullOrWhiteSpace(flagKey))
            {
                return;
            }

            _flags.Remove(flagKey);
        }

        public void AddRelationship(int delta)
        {
            RelationshipValue = ClampRelationship(RelationshipValue + delta);
        }

        public IReadOnlyCollection<string> GetActiveFlagKeys()
        {
            return _flags;
        }

        private static int ClampRelationship(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
