namespace ProjectArk.Combat
{
    /// <summary>
    /// Abstraction for resolving Star Chart item names to ScriptableObjects.
    /// Allows the save/load system in the Combat assembly to look up items
    /// without depending on the UI assembly where the concrete inventory lives.
    /// </summary>
    public interface IStarChartItemResolver
    {
        StarCoreSO FindCore(string displayName);
        PrismSO FindPrism(string displayName);
        LightSailSO FindLightSail(string displayName);
        SatelliteSO FindSatellite(string displayName);
    }
}
