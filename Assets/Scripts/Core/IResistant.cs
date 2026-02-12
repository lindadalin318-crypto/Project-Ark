namespace ProjectArk.Core
{
    /// <summary>
    /// Implemented by entities that have elemental damage resistances.
    /// <see cref="DamageCalculator"/> queries this to reduce incoming damage.
    /// </summary>
    public interface IResistant
    {
        /// <summary>
        /// Returns the resistance value for a given damage type (0 = none, 1 = immune).
        /// </summary>
        float GetResistance(DamageType type);
    }
}
