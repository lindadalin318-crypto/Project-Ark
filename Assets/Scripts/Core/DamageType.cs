namespace ProjectArk.Core
{
    /// <summary>
    /// Elemental damage types for the damage pipeline.
    /// Maps 1:1 to the resistance fields on EnemyStatsSO.
    /// </summary>
    public enum DamageType
    {
        Physical,  // default — most projectiles
        Fire,      // Tint: burn DoT
        Ice,       // Tint: slow
        Lightning, // Tint: chain
        Void       // Tint: corrosion / defense reduction
    }
}
