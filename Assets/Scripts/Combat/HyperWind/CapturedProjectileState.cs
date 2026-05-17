namespace ProjectArk.Combat.HyperWind
{
    /// <summary>
    /// Runtime-only state for a projectile currently orbiting inside a ground cyclone.
    /// </summary>
    internal sealed class CapturedProjectileState
    {
        public CapturedProjectileState(ICycloneCaptureTarget target, float angleDegrees)
        {
            Target = target;
            AngleDegrees = angleDegrees;
        }

        public ICycloneCaptureTarget Target { get; }
        public float AngleDegrees { get; set; }
        public float CompletedTurns { get; set; }
    }
}
