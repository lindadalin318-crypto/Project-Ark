using UnityEngine;

namespace ProjectArk.Combat.HyperWind
{
    /// <summary>
    /// Adapter contract for projectile-like runtime objects that can be captured by a HyperWind ground cyclone.
    /// Implementations preserve their original ownership and damage rules while the cyclone owns movement.
    /// </summary>
    public interface ICycloneCaptureTarget
    {
        Transform CapturableTransform { get; }
        GameObject CapturableGameObject { get; }
        bool CanBeCapturedByCyclone { get; }
        float CycloneBaseSpeed { get; }

        void CaptureByCyclone();
        void ReleaseFromCyclone(Vector2 direction, float speedMultiplier, float damageMultiplier);
        void DiscardByCyclone();
    }
}
