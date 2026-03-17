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

        [Tooltip("Peak brightness multiplier reached during the initial Boost ignition punch before settling.")]
        [Min(1f)]
        [SerializeField] private float _boostGlowEntryBrightnessMultiplier = 1.95f;

        [Tooltip("Duration (seconds) for the liquid layer to punch UP into the ignition peak.")]
        [Min(0.01f)]
        [SerializeField] private float _boostGlowRampUpDuration = 0.045f;

        [Tooltip("Duration (seconds) for the liquid layer to settle from ignition peak into the sustained Boost state.")]
        [Min(0.01f)]
        [SerializeField] private float _boostGlowSettleDuration = 0.12f;

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
        [Tooltip("Primary color of engine exhaust particles (top/start color, cyan-blue). GG: TopColor=(0.099,0.846,1.0).")]
        [SerializeField] private Color _engineParticleColorTop = new Color(0.099f, 0.846f, 1.0f, 1f);

        [Tooltip("Secondary color of engine exhaust particles (bottom/end color, magenta). GG: BottomColor=(1.0,0.0,0.915).")]
        [SerializeField] private Color _engineParticleColorBottom = new Color(1.0f, 0.0f, 0.915f, 1f);

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
        public float BoostGlowEntryBrightnessMultiplier => _boostGlowEntryBrightnessMultiplier;
        public float BoostGlowRampUpDuration => _boostGlowRampUpDuration;
        public float BoostGlowSettleDuration => _boostGlowSettleDuration;
        public float BoostGlowRampDownDuration => _boostGlowRampDownDuration;

        // Dash Flash
        public float IFrameFlashAlpha => _iFrameFlashAlpha;
        public float IFrameFlashInterval => _iFrameFlashInterval;

        // Engine — Precise
        public Color EngineParticleColorTop    => _engineParticleColorTop;
        public Color EngineParticleColorBottom => _engineParticleColorBottom;
        public float EngineParticleMinSpeed => _engineParticleMinSpeed;
        public float EngineIdleEmissionRate => _engineIdleEmissionRate;
        public float EngineMaxEmissionRate => _engineMaxEmissionRate;
        public float EngineDashEmissionRate => _engineDashEmissionRate;
        public float EngineStartSizeMin => _engineStartSizeMin;
        public float EngineStartSizeMax => _engineStartSizeMax;

        // Thruster Pulse
        public float BoostBackScalePeak => _boostBackScalePeak;
        public float BoostBackEntryPulseDuration => _boostBackEntryPulseDuration;
        public float BoostBackPulsePeriod => _boostBackPulsePeriod;
        public float BoostBackPulseScale => _boostBackPulseScale;
        public float BoostBackRestoreDuration => _boostBackRestoreDuration;
    }
}
