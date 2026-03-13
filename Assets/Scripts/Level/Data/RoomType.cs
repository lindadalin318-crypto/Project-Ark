namespace ProjectArk.Level
{
    /// <summary>
    /// Categorizes rooms for gameplay logic (door locking, encounter triggering, map display).
    /// See also: ShebaRoomGrammar.md § 2.2 for the 8-type design-side taxonomy.
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
        Safe,

        /// <summary> Narrow connecting passage between rooms. </summary>
        Corridor,

        /// <summary> Merchant room — player can purchase items or upgrades. </summary>
        Shop,

        /// <summary> Navigation hub — multi-exit safe zone with checkpoint, map anchor, route decisions. </summary>
        Hub,

        /// <summary> Chapter gate — visible but locked threshold, promises future content. </summary>
        Gate
    }
}
