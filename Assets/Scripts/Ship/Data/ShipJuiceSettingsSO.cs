using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Data asset for ship visual juice / feedback parameters.
    /// Controls tilt, squash-stretch, dash after-images, and engine particles.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShipJuiceSettings", menuName = "ProjectArk/Ship/Ship Juice Settings")]
    public class ShipJuiceSettingsSO : ScriptableObject
    {
        // ══════════════════════════════════════════════════════════════
        // Movement Tilt
        // ══════════════════════════════════════════════════════════════

        [Header("Movement Tilt")]
        [Tooltip("Maximum tilt angle (degrees) when the ship moves laterally.")]
        [Range(0f, 45f)]
        [SerializeField] private float _moveTiltMaxAngle = 15f;

        [Tooltip("Smoothing speed for tilt transitions (higher = snappier).")]
        [Min(1f)]
        [SerializeField] private float _tiltSmoothSpeed = 10f;

        // ══════════════════════════════════════════════════════════════
        // Squash & Stretch
        // ══════════════════════════════════════════════════════════════

        [Header("Squash & Stretch")]
        [Tooltip("Intensity of the squash/stretch effect (0 = none).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _squashStretchIntensity = 0.15f;

        [Tooltip("Duration of a single squash/stretch pulse (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _squashStretchDuration = 0.1f;

        // ══════════════════════════════════════════════════════════════
        // Dash After-Image
        // ══════════════════════════════════════════════════════════════

        [Header("Dash After-Image")]
        [Tooltip("Number of after-image ghosts spawned during a dash.")]
        [Range(1, 10)]
        [SerializeField] private int _dashAfterImageCount = 3;

        [Tooltip("Duration for each after-image to fade out (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _afterImageFadeDuration = 0.15f;

        [Tooltip("Starting alpha of each after-image ghost.")]
        [Range(0f, 1f)]
        [SerializeField] private float _afterImageAlpha = 0.4f;

        // ══════════════════════════════════════════════════════════════
        // Engine Particles
        // ══════════════════════════════════════════════════════════════

        [Header("Engine Particles")]
        [Tooltip("Normalized speed threshold below which engine particles stop emitting.")]
        [Range(0f, 1f)]
        [SerializeField] private float _engineParticleMinSpeed = 0.1f;

        [Tooltip("Base emission rate at full speed.")]
        [Min(1f)]
        [SerializeField] private float _engineBaseEmissionRate = 20f;

        [Tooltip("Emission rate multiplier during dash.")]
        [Min(1f)]
        [SerializeField] private float _engineDashEmissionMultiplier = 3f;

        // ══════════════════════════════════════════════════════════════
        // Public Getters
        // ══════════════════════════════════════════════════════════════

        // Tilt
        public float MoveTiltMaxAngle => _moveTiltMaxAngle;
        public float TiltSmoothSpeed => _tiltSmoothSpeed;

        // Squash & Stretch
        public float SquashStretchIntensity => _squashStretchIntensity;
        public float SquashStretchDuration => _squashStretchDuration;

        // After-Image
        public int DashAfterImageCount => _dashAfterImageCount;
        public float AfterImageFadeDuration => _afterImageFadeDuration;
        public float AfterImageAlpha => _afterImageAlpha;

        // Engine
        public float EngineParticleMinSpeed => _engineParticleMinSpeed;
        public float EngineBaseEmissionRate => _engineBaseEmissionRate;
        public float EngineDashEmissionMultiplier => _engineDashEmissionMultiplier;
    }
}
