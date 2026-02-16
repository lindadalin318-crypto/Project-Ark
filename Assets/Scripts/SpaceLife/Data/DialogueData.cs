
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife.Data
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 6)] public string text;
        public Sprite speakerAvatar;
        public List<DialogueOption> options = new List<DialogueOption>();
    }

    [System.Serializable]
    public class DialogueOption
    {
        [TextArea(2, 4)] public string optionText;
        public DialogueLine nextLine;
        public int relationshipChange;
    }
}

