namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Identifies which hub-side owner a dialogue graph belongs to.
    /// </summary>
    public enum DialogueOwnerType
    {
        Npc = 0,
        Terminal = 1,
    }

    /// <summary>
    /// Describes how a dialogue node should be presented.
    /// </summary>
    public enum DialogueNodeType
    {
        Line = 0,
        System = 1,
    }

    /// <summary>
    /// Supported condition kinds for the MVP authored data.
    /// </summary>
    public enum DialogueConditionType
    {
        WorldStage = 0,
        RelationshipValue = 1,
        FlagPresent = 2,
        FlagAbsent = 3,
    }

    /// <summary>
    /// Supported numeric comparison operators.
    /// </summary>
    public enum DialogueCompareOp
    {
        Equal = 0,
        NotEqual = 1,
        GreaterThan = 2,
        GreaterOrEqual = 3,
        LessThan = 4,
        LessOrEqual = 5,
    }

    /// <summary>
    /// Supported side effects for the MVP dialogue runtime.
    /// </summary>
    public enum DialogueEffectType
    {
        SetFlag = 0,
        ClearFlag = 1,
        AddRelationship = 2,
        EmitServiceExit = 3,
        EndDialogue = 4,
    }

    /// <summary>
    /// Unified service exits that can be emitted by dialogue choices or effects.
    /// </summary>
    public enum DialogueServiceExitType
    {
        None = 0,
        OpenGift = 1,
        OpenUpgrade = 2,
        OpenIntel = 3,
        TriggerRelationshipEvent = 4,
    }
}
