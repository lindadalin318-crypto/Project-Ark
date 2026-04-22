using System;
using System.Collections.Generic;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Read-only node view model consumed by dialogue presenters.
    /// </summary>
    public sealed class DialogueNodeViewModel
    {
        public DialogueNodeViewModel(
            string ownerId,
            string nodeId,
            DialogueNodeType nodeType,
            string speakerName,
            string text,
            IReadOnlyList<ChoiceViewModel> choices)
        {
            OwnerId = ownerId;
            NodeId = nodeId;
            NodeType = nodeType;
            SpeakerName = speakerName;
            Text = text;
            Choices = choices ?? Array.Empty<ChoiceViewModel>();
        }

        public string OwnerId { get; }
        public string NodeId { get; }
        public DialogueNodeType NodeType { get; }
        public string SpeakerName { get; }
        public string Text { get; }
        public IReadOnlyList<ChoiceViewModel> Choices { get; }

        /// <summary>
        /// Read-only choice view model exposed to presenters.
        /// </summary>
        public sealed class ChoiceViewModel
        {
            public ChoiceViewModel(string choiceId, string text, DialogueServiceExitType exitType, string exitPayload)
            {
                ChoiceId = choiceId;
                Text = text;
                ExitType = exitType;
                ExitPayload = exitPayload;
            }

            public string ChoiceId { get; }
            public string Text { get; }
            public DialogueServiceExitType ExitType { get; }
            public string ExitPayload { get; }
        }
    }
}
