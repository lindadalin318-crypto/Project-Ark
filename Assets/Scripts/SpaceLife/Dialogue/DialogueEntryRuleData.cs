using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Entry rule that selects the starting node for a dialogue session.
    /// </summary>
    [Serializable]
    public class DialogueEntryRuleData
    {
        [SerializeField] private string _entryNodeId;
        [SerializeField] private int _priority;
        [SerializeField] private List<DialogueConditionData> _conditions = new();

        public string EntryNodeId => _entryNodeId;
        public int Priority => _priority;
        public IReadOnlyList<DialogueConditionData> Conditions => _conditions;
    }
}
