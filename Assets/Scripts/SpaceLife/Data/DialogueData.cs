
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    /// <summary>
    /// A single line of dialogue spoken by a character.
    /// All DialogueLines for an NPC are stored flat in NPCDataSO.DialogueNodes;
    /// options reference the next node by index to avoid serialization cycles.
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
    /// <para><see cref="NextLineIndex"/> is the index into the owning NPC's
    /// <c>DialogueNodes</c> list. Use -1 to end the conversation.</para>
    /// </summary>
    [System.Serializable]
    public class DialogueOption
    {
        [TextArea(2, 4)]
        [SerializeField] private string _optionText;

        /// <summary>
        /// Index into NPCDataSO.DialogueNodes. -1 = close dialogue.
        /// </summary>
        [Tooltip("Index into the NPC's DialogueNodes list. Set to -1 to end the conversation.")]
        [SerializeField] private int _nextLineIndex = -1;

        [SerializeField] private int _relationshipChange;

        public string OptionText => _optionText;
        public int NextLineIndex => _nextLineIndex;
        public int RelationshipChange => _relationshipChange;
    }
}

