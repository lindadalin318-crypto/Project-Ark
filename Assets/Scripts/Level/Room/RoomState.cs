namespace ProjectArk.Level
{
    /// <summary>
    /// Runtime state of a room. Progresses forward: Undiscovered → Entered → Cleared.
    /// Locked is a special state set by external systems (e.g., LockKeySystem).
    /// </summary>
    public enum RoomState
    {
        /// <summary> Player has never entered this room. Hidden on minimap. </summary>
        Undiscovered,

        /// <summary> Player has entered at least once. Enemies may still be alive. </summary>
        Entered,

        /// <summary> All enemies defeated (or room has no encounter). Doors unlocked. </summary>
        Cleared,

        /// <summary> Room is locked by external condition (key/ability/schedule). </summary>
        Locked
    }
}
