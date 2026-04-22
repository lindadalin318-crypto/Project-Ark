using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Authored dialogue node data addressed by stable NodeId.
    /// </summary>
    [Serializable]
    public class DialogueNodeData
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private DialogueNodeType _nodeType = DialogueNodeType.Line;
        [SerializeField] private string _speakerName;
        [SerializeField, TextArea(2, 6)] private string _rawText;
        [SerializeField] private string _nextNodeId;
        [SerializeField] private List<DialogueChoiceData> _choices = new();
        [SerializeField] private List<DialogueEffectData> _effects = new();

        public string NodeId => _nodeId;
        public DialogueNodeType NodeType => _nodeType;
        public string SpeakerName => _speakerName;
        public string RawText => _rawText;
        public string NextNodeId => _nextNodeId;
        public IReadOnlyList<DialogueChoiceData> Choices => _choices;
        public IReadOnlyList<DialogueEffectData> Effects => _effects;
    }
}
