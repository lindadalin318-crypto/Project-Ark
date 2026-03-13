namespace ProjectArk.Level
{
    /// <summary>
    /// Determines how an encounter is triggered and resolved.
    /// Mirrors Minishoot's two encounter paradigms.
    /// </summary>
    public enum EncounterMode
    {
        /// <summary>
        /// Closed encounter (封门清算): doors lock on entry, player must clear all waves to proceed.
        /// Used by Arena/Boss rooms via ArenaController.
        /// </summary>
        Closed,

        /// <summary>
        /// Open encounter (开放骚扰): enemies activate when player enters the trigger zone,
        /// but doors remain open. Player can disengage by leaving the area.
        /// Enemies despawn when player exits + cooldown, or are fully cleared.
        /// </summary>
        Open
    }
}
