using UnityEngine;

namespace ProjectArk.HyperWind
{
    /// <summary>
    /// Immutable result of sampling the HyperWind vector field at a world position.
    /// Direction is normalized when possible; Velocity already includes phase multiplier.
    /// </summary>
    public readonly struct WindSample
    {
        public static readonly WindSample Calm = new(Vector2.right, 0f, 1f);

        public WindSample(Vector2 direction, float baseSpeed, float phaseMultiplier)
        {
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            BaseSpeed = Mathf.Max(0f, baseSpeed);
            PhaseMultiplier = Mathf.Max(0f, phaseMultiplier);
        }

        public Vector2 Direction { get; }
        public float BaseSpeed { get; }
        public float PhaseMultiplier { get; }
        public float Speed => BaseSpeed * PhaseMultiplier;
        public Vector2 Velocity => Direction * Speed;
        public bool HasWind => Speed > 0.001f;
    }
}
