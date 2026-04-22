using System;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Authored condition descriptor for graph entries and choices.
    /// </summary>
    [Serializable]
    public class DialogueConditionData
    {
        [SerializeField] private DialogueConditionType _conditionType;
        [SerializeField] private DialogueCompareOp _compareOp = DialogueCompareOp.GreaterOrEqual;
        [SerializeField] private int _intValue;
        [SerializeField] private string _flagKey;

        public DialogueConditionType ConditionType => _conditionType;
        public DialogueCompareOp CompareOp => _compareOp;
        public int IntValue => _intValue;
        public string FlagKey => _flagKey;
    }
}
