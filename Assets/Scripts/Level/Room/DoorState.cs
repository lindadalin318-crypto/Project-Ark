namespace ProjectArk.Level
{
    /// <summary>
    /// State of a door/passage. Determines whether the player can pass through.
    /// </summary>
    public enum DoorState
    {
        /// <summary> Door is open — player can pass through freely. </summary>
        Open,

        /// <summary> Locked by combat — unlocks when room is cleared. </summary>
        Locked_Combat,

        /// <summary> Locked by key item — requires specific KeyItemSO to unlock. </summary>
        Locked_Key,

        /// <summary> Locked by ability — requires specific ability/light sail to unlock. </summary>
        Locked_Ability,

        /// <summary> Locked by world schedule — controlled by WorldPhaseManager time cycle. </summary>
        Locked_Schedule
    }
}
