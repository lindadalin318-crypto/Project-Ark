namespace ProjectArk.Ship
{
    /// <summary>
    /// Defines all possible ship states, aligned with GG's PlayerShipState enum values.
    /// Normal=0 / Dash=2 / Boost=3 / MainAttack=6 / MainAttackFire=10
    /// </summary>
    public enum ShipShipState
    {
        /// <summary>Default flight state (GG: Blue=0).</summary>
        Normal = 0,

        /// <summary>Dodge i-frame state (GG: Dodge=2).</summary>
        Dash = 2,

        /// <summary>Boost acceleration state (GG: Boost=3).</summary>
        Boost = 3,

        /// <summary>Weapon charge/attack state (GG: MainAttack=6). Reserved for future use.</summary>
        MainAttack = 6,

        /// <summary>Weapon fire instant state (GG: MainAttackFire=10). Reserved for future use.</summary>
        MainAttackFire = 10,
    }
}
