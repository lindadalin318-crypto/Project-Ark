namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Unified result object emitted by dialogue choices and effects.
    /// </summary>
    public sealed class DialogueServiceExit
    {
        public static DialogueServiceExit None { get; } = new(DialogueServiceExitType.None, string.Empty, shouldEndDialogue: false);

        public DialogueServiceExit(DialogueServiceExitType exitType, string payload = "", bool shouldEndDialogue = false)
        {
            ExitType = exitType;
            Payload = payload ?? string.Empty;
            ShouldEndDialogue = shouldEndDialogue;
        }

        public DialogueServiceExitType ExitType { get; }
        public string Payload { get; }
        public bool ShouldEndDialogue { get; }
        public bool HasExit => ExitType != DialogueServiceExitType.None;
    }
}
