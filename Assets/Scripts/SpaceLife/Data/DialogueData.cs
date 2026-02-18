
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    /// <summary>
    /// A single line of dialogue spoken by a character.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [SerializeField] private string _speakerName;
        [TextArea(3, 6)]
        [SerializeField] private string _text;
        [SerializeField] private Sprite _speakerAvatar;
        [SerializeField] private List<DialogueOption> _options = new();

        public string SpeakerName => _speakerName;
        public string Text => _text;
        public Sprite SpeakerAvatar => _speakerAvatar;
        public IReadOnlyList<DialogueOption> Options => _options;
    }

    /// <summary>
    /// A selectable option within a dialogue line.
    /// </summary>
    [System.Serializable]
    public class DialogueOption
    {
        [TextArea(2, 4)]
        [SerializeField] private string _optionText;
        [SerializeField] private DialogueLine _nextLine;
        [SerializeField] private int _relationshipChange;

        public string OptionText => _optionText;
        public DialogueLine NextLine => _nextLine;
        public int RelationshipChange => _relationshipChange;
    }
}

