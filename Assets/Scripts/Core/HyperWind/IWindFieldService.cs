using UnityEngine;

namespace ProjectArk.HyperWind
{
    /// <summary>
    /// Shared read-only access point for HyperWind vector field sampling.
    /// Consumers should cache this service or use ServiceLocator.TryGet to avoid per-frame logs.
    /// </summary>
    public interface IWindFieldService
    {
        WindSample Sample(Vector2 worldPosition);
    }
}
