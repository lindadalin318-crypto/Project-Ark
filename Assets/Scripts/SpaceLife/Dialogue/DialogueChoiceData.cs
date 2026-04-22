using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Authored player choice data.
    /// </summary>
    [Serializable]
    public class DialogueChoiceData
    {
        [SerializeField] private string _choiceId;
        [SerializeField, TextArea(1, 4)] private string _rawText;
        [SerializeField] private List<DialogueConditionData> _conditions = new();
        [SerializeField] private string _nextNodeId;
        [SerializeField] private List<DialogueEffectData> _effects = new();
        [SerializeField] private DialogueServiceExitType _exitType;
        [SerializeField] private string _exitPayload;

        public string ChoiceId => _choiceId;
        public string RawText => _rawText;
        public IReadOnlyList<DialogueConditionData> Conditions => _conditions;
        public string NextNodeId => _nextNodeId;
        public IReadOnlyList<DialogueEffectData> Effects => _effects;
        public DialogueServiceExitType ExitType => _exitType;
        public string ExitPayload => _exitPayload;
    }
}
