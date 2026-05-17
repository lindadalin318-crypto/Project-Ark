using System;

namespace ProjectArk.HyperWind
{
    public enum WindPhaseState
    {
        Weak,
        SandWarning,
        AudioWarning,
        Strong
    }

    /// <summary>
    /// Shared read-only access point for HyperWind global rhythm state.
    /// </summary>
    public interface IWindPhaseService
    {
        event Action<WindPhaseState, WindPhaseState> OnPhaseStateChanged;

        WindPhaseState CurrentState { get; }
        float Cycle01 { get; }
        float CurrentWindMultiplier { get; }
        bool IsStrong { get; }
    }
}
