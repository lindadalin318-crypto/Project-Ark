using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Data asset for ship visual juice / feedback parameters.
    /// Controls tilt, squash-stretch, dash after-images, engine particles,
    /// boost trail particles, trail renderer, and thruster pulse animation.
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

        [Tooltip("Tint color applied to dash after-image ghosts. Alpha is overridden by AfterImageAlpha.")]
        [SerializeField] private Color _afterImageColor = new Color(0.28f, 0.43f, 0.43f, 1f);

        // ══════════════════════════════════════════════════════════════
        // Boost Visual
        // ══════════════════════════════════════════════════════════════

        [Header("Boost Visual")]
        [Tooltip("How much to multiply the liquid/glow layer brightness when Boost starts (e.g. 1.5 = 50% brighter).")]
        [Min(1f)]
        [SerializeField] private float _boostGlowBrightnessMultiplier = 1.5f;

        [Tooltip("Duration (seconds) for the glow layer to ramp UP to boost brightness.")]
        [Min(0.01f)]
        [SerializeField] private float _boostGlowRampUpDuration = 0.1f;

        [Tooltip("Duration (seconds) for the glow layer to ramp DOWN back to normal after boost ends.")]
        [Min(0.01f)]
        [SerializeField] private float _boostGlowRampDownDuration = 0.3f;

        // ══════════════════════════════════════════════════════════════
        // Dash Invincibility Flash
        // ══════════════════════════════════════════════════════════════

        [Header("Dash Invincibility Flash")]
        [Tooltip("Alpha value for the 'dim' phase of the invincibility flash (0 = invisible, 1 = fully visible).")]
        [Range(0f, 1f)]
        [SerializeField] private float _iFrameFlashAlpha = 0.3f;

        [Tooltip("Interval (seconds) between each alpha toggle during invincibility frames.")]
        [Min(0.01f)]
        [SerializeField] private float _iFrameFlashInterval = 0.05f;

        // ══════════════════════════════════════════════════════════════
        // Engine Particles — Precise Parameters
        // ══════════════════════════════════════════════════════════════

        [Header("Engine Particles — Precise")]
        [Tooltip("Color of engine exhaust particles (teal/cyan, fades to transparent over lifetime).")]
        [SerializeField] private Color _engineParticleColor = new Color(0.28f, 0.43f, 0.43f, 1f);

        [Tooltip("Normalized speed threshold below which engine uses idle emission rate.")]
        [Range(0f, 1f)]
        [SerializeField] private float _engineParticleMinSpeed = 0.1f;

        [Tooltip("Emission rate when ship is idle (thruster presence).")]
        [Min(0f)]
        [SerializeField] private float _engineIdleEmissionRate = 5f;

        [Tooltip("Maximum emission rate at full speed.")]
        [Min(1f)]
        [SerializeField] private float _engineMaxEmissionRate = 40f;

        [Tooltip("Emission rate during Dash.")]
        [Min(1f)]
        [SerializeField] private float _engineDashEmissionRate = 120f;

        [Tooltip("Minimum start size of engine particles.")]
        [Min(0.001f)]
        [SerializeField] private float _engineStartSizeMin = 0.04f;

        [Tooltip("Maximum start size of engine particles.")]
        [Min(0.001f)]
        [SerializeField] private float _engineStartSizeMax = 0.08f;

        // ══════════════════════════════════════════════════════════════
        // Boost Trail Particles
        // ══════════════════════════════════════════════════════════════

        [Header("Boost Trail Particles")]
        [Tooltip("Emission rate of the dedicated Boost trail particle system.")]
        [Min(1f)]
        [SerializeField] private float _boostTrailEmissionRate = 80f;

        [Tooltip("Lifetime of each boost trail particle (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _boostTrailLifetime = 0.3f;

        [Tooltip("Start speed of boost trail particles (in local -Y direction).")]
        [Min(0f)]
        [SerializeField] private float _boostTrailStartSpeed = 3.0f;

        [Tooltip("Minimum start size of boost trail particles.")]
        [Min(0.001f)]
        [SerializeField] private float _boostTrailStartSizeMin = 0.08f;

        [Tooltip("Maximum start size of boost trail particles.")]
        [Min(0.001f)]
        [SerializeField] private float _boostTrailStartSizeMax = 0.15f;

        [Tooltip("Color of boost trail particles (teal/cyan, fades to transparent).")]
        [SerializeField] private Color _boostTrailColor = new Color(0.28f, 0.43f, 0.43f, 1f);

        // ══════════════════════════════════════════════════════════════
        // Boost TrailRenderer
        // ══════════════════════════════════════════════════════════════

        [Header("Boost TrailRenderer")]
        [Tooltip("Time width of the boost trail (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _boostTrailTime = 0.25f;

        [Tooltip("Start width of the boost trail.")]
        [Min(0.001f)]
        [SerializeField] private float _boostTrailStartWidth = 0.12f;

        [Tooltip("Minimum vertex distance for the boost trail (prevents jitter).")]
        [Min(0.001f)]
        [SerializeField] private float _boostTrailMinVertexDistance = 0.05f;

        [Tooltip("Color of the boost trail renderer (teal/cyan, fades to transparent).")]
        [SerializeField] private Color _boostTrailRendererColor = new Color(0.28f, 0.43f, 0.43f, 0.8f);

        // ══════════════════════════════════════════════════════════════
        // Thruster Pulse (Ship_Sprite_Back)
        // ══════════════════════════════════════════════════════════════

        [Header("Thruster Pulse")]
        [Tooltip("Peak scale of the thruster on Boost entry pulse.")]
        [Min(1f)]
        [SerializeField] private float _boostBackScalePeak = 1.3f;

        [Tooltip("Duration of the entry pulse (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _boostBackEntryPulseDuration = 0.15f;

        [Tooltip("Period of the continuous loop pulse during Boost (seconds).")]
        [Min(0.05f)]
        [SerializeField] private float _boostBackPulsePeriod = 0.3f;

        [Tooltip("Scale amplitude of the continuous loop pulse (e.g. 1.1 = 10% larger).")]
        [Min(1f)]
        [SerializeField] private float _boostBackPulseScale = 1.1f;

        [Tooltip("Duration to restore thruster scale to 1.0 after Boost ends (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _boostBackRestoreDuration = 0.1f;

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
        public Color AfterImageColor => _afterImageColor;

        // Boost Visual
        public float BoostGlowBrightnessMultiplier => _boostGlowBrightnessMultiplier;
        public float BoostGlowRampUpDuration => _boostGlowRampUpDuration;
        public float BoostGlowRampDownDuration => _boostGlowRampDownDuration;

        // Dash Flash
        public float IFrameFlashAlpha => _iFrameFlashAlpha;
        public float IFrameFlashInterval => _iFrameFlashInterval;

        // Engine — Precise
        public Color EngineParticleColor => _engineParticleColor;
        public float EngineParticleMinSpeed => _engineParticleMinSpeed;
        public float EngineIdleEmissionRate => _engineIdleEmissionRate;
        public float EngineMaxEmissionRate => _engineMaxEmissionRate;
        public float EngineDashEmissionRate => _engineDashEmissionRate;
        public float EngineStartSizeMin => _engineStartSizeMin;
        public float EngineStartSizeMax => _engineStartSizeMax;

        // Boost Trail Particles
        public float BoostTrailEmissionRate => _boostTrailEmissionRate;
        public float BoostTrailLifetime => _boostTrailLifetime;
        public float BoostTrailStartSpeed => _boostTrailStartSpeed;
        public float BoostTrailStartSizeMin => _boostTrailStartSizeMin;
        public float BoostTrailStartSizeMax => _boostTrailStartSizeMax;
        public Color BoostTrailColor => _boostTrailColor;

        // Boost TrailRenderer
        public float BoostTrailTime => _boostTrailTime;
        public float BoostTrailStartWidth => _boostTrailStartWidth;
        public float BoostTrailMinVertexDistance => _boostTrailMinVertexDistance;
        public Color BoostTrailRendererColor => _boostTrailRendererColor;

        // Thruster Pulse
        public float BoostBackScalePeak => _boostBackScalePeak;
        public float BoostBackEntryPulseDuration => _boostBackEntryPulseDuration;
        public float BoostBackPulsePeriod => _boostBackPulsePeriod;
        public float BoostBackPulseScale => _boostBackPulseScale;
        public float BoostBackRestoreDuration => _boostBackRestoreDuration;
    }
}
