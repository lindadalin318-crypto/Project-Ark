namespace ProjectArk.Level
{
    /// <summary>
    /// Runtime state of a room. Progresses forward: Undiscovered → Entered → Cleared.
    /// Door gating remains owned by Door/Lock/world progress systems, not RoomState.
    /// </summary>
    public enum RoomState
    {
        /// <summary> Player has never entered this room. Hidden on minimap. </summary>
        Undiscovered,

        /// <summary> Player has entered at least once. Enemies may still be alive. </summary>
        Entered,

        /// <summary> All enemies defeated (or room has no encounter). Doors unlocked. </summary>
        Cleared
    }
}
