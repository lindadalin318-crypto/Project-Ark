namespace ProjectArk.Level
{
    /// <summary>
    /// Categorizes rooms for gameplay logic (door locking, encounter triggering, map display).
    /// </summary>
    public enum RoomType
    {
        /// <summary> Standard room with optional enemies. Can leave freely. </summary>
        Normal,

        /// <summary> Combat arena — doors lock on entry, unlock after all waves cleared. </summary>
        Arena,

        /// <summary> Boss encounter room — doors lock during fight, special rewards on clear. </summary>
        Boss,

        /// <summary> Safe zone — no enemies, may contain checkpoint/shop/NPC. </summary>
        Safe
    }
}
