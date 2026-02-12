namespace ProjectArk.Core
{
    /// <summary>
    /// Implemented by entities that can block incoming damage.
    /// <see cref="DamageCalculator"/> queries this to apply block reduction.
    /// </summary>
    public interface IBlockable
    {
        /// <summary> Whether the entity is currently in a blocking stance. </summary>
        bool IsBlocking { get; }

        /// <summary> Damage reduction multiplier while blocking (0.7 = reduce 70%). </summary>
        float BlockDamageReduction { get; }
    }
}
