using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Data asset for ship visual juice / feedback parameters.
    /// Controls tilt, squash-stretch, dash after-images,
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
        // Boost Liquid — Sprite Swap + Color Tween + SortOrder
        // ══════════════════════════════════════════════════════════════

        [Header("Boost Liquid — Sprite Swap")]
        [Tooltip("Sprite to swap onto Liquid layer when entering Boost. Null = no swap.")]
        [SerializeField] private Sprite _boostLiquidSprite;

        [Header("Boost Liquid — SortOrder Override")]
        [Tooltip("When true, Liquid sortingOrder is raised above Solid during Boost.")]
        [SerializeField] private bool _boostLiquidSortOverride = true;

        [Tooltip("SortingOrder for Liquid layer during Boost (Solid is 0, so 1 = above Solid).")]
        [SerializeField] private int _boostLiquidSortOrder = 1;

        [Header("Boost Liquid — Color")]
        [Tooltip("Target HDR color for the Liquid layer during Boost. Additive material makes bright colors glow. Use values > 1 for HDR bloom.")]
        [SerializeField] private Color _boostLiquidColor = new Color(3f, 4f, 5f, 1f);

        [Tooltip("Duration (seconds) for the Liquid layer to tween from baseline to Boost color.")]
        [Min(0.01f)]
        [SerializeField] private float _boostLiquidRampUpDuration = 0.12f;

        [Tooltip("Duration (seconds) for the Liquid layer to tween from Boost color back to baseline.")]
        [Min(0.01f)]
        [SerializeField] private float _boostLiquidRampDownDuration = 0.3f;

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

        // Boost Liquid
        public Sprite BoostLiquidSprite => _boostLiquidSprite;
        public bool BoostLiquidSortOverride => _boostLiquidSortOverride;
        public int BoostLiquidSortOrder => _boostLiquidSortOrder;
        public Color BoostLiquidColor => _boostLiquidColor;
        public float BoostLiquidRampUpDuration => _boostLiquidRampUpDuration;
        public float BoostLiquidRampDownDuration => _boostLiquidRampDownDuration;

        // Dash Flash
        public float IFrameFlashAlpha => _iFrameFlashAlpha;
        public float IFrameFlashInterval => _iFrameFlashInterval;

        // Thruster Pulse
        public float BoostBackScalePeak => _boostBackScalePeak;
        public float BoostBackEntryPulseDuration => _boostBackEntryPulseDuration;
        public float BoostBackPulsePeriod => _boostBackPulsePeriod;
        public float BoostBackPulseScale => _boostBackPulseScale;
        public float BoostBackRestoreDuration => _boostBackRestoreDuration;

        // ══════════════════════════════════════════════════════════════
        // Highlight Layer (Ship_Sprite_HL) — dynamic state response
        // ══════════════════════════════════════════════════════════════

        [Header("Highlight Layer (HL)")]
        [Tooltip("HL layer base alpha in Normal state.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hlBaseAlpha = 0.5f;

        [Tooltip("HL layer alpha during Boost (energy glow intensifies).")]
        [Range(0f, 1f)]
        [SerializeField] private float _hlBoostAlpha = 0.85f;

        [Tooltip("Duration for HL alpha to ramp up into Boost state.")]
        [Min(0.01f)]
        [SerializeField] private float _hlBoostRampUpDuration = 0.12f;

        [Tooltip("Duration for HL alpha to ramp down from Boost to Normal.")]
        [Min(0.01f)]
        [SerializeField] private float _hlBoostRampDownDuration = 0.25f;

        // ══════════════════════════════════════════════════════════════
        // Core Layer (Ship_Sprite_Core) — energy state + warning
        // ══════════════════════════════════════════════════════════════

        [Header("Core Layer")]
        [Tooltip("Core layer base alpha in Normal state (subtle ambient glow).")]
        [Range(0f, 1f)]
        [SerializeField] private float _coreBaseAlpha = 0.3f;

        [Tooltip("Core layer alpha during Boost (cockpit energy surge).")]
        [Range(0f, 1f)]
        [SerializeField] private float _coreBoostAlpha = 0.9f;

        [Tooltip("Duration for Core alpha to ramp up into Boost state.")]
        [Min(0.01f)]
        [SerializeField] private float _coreBoostRampUpDuration = 0.15f;

        [Tooltip("Duration for Core alpha to ramp down from Boost to Normal.")]
        [Min(0.01f)]
        [SerializeField] private float _coreBoostRampDownDuration = 0.3f;

        [Tooltip("HP ratio threshold below which Core enters low-HP warning pulse.")]
        [Range(0f, 1f)]
        [SerializeField] private float _coreLowHPThreshold = 0.3f;

        [Tooltip("Tint color applied to Core layer during low-HP warning.")]
        [SerializeField] private Color _coreLowHPColor = new Color(1f, 0.2f, 0.1f, 0.9f);

        [Tooltip("Period of the low-HP Core pulse cycle (seconds).")]
        [Min(0.1f)]
        [SerializeField] private float _coreLowHPPulsePeriod = 0.6f;

        [Tooltip("Alpha range of the low-HP Core pulse (min alpha).")]
        [Range(0f, 1f)]
        [SerializeField] private float _coreLowHPPulseMinAlpha = 0.3f;

        // ══════════════════════════════════════════════════════════════
        // Hit Flash (multi-layer) — replaces ShipHealth single-layer
        // ══════════════════════════════════════════════════════════════

        [Header("Hit Flash (Multi-Layer)")]
        [Tooltip("Duration of the hit white flash across all sprite layers.")]
        [Min(0.01f)]
        [SerializeField] private float _hitFlashDuration = 0.1f;

        [Tooltip("Color used for hit flash. White = classic hit flash.")]
        [SerializeField] private Color _hitFlashColor = Color.white;

        [Tooltip("Duration of post-hit i-frame blink. 0 = use ShipStatsSO.IFrameDuration.")]
        [Min(0f)]
        [SerializeField] private float _hitIFrameDuration = 0f;

        [Tooltip("Blink interval for post-hit i-frames across all layers.")]
        [Min(0.01f)]
        [SerializeField] private float _hitIFrameBlinkInterval = 0.08f;

        [Tooltip("Alpha during the dim phase of hit i-frame blink.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hitIFrameDimAlpha = 0.25f;

        // HL
        public float HLBaseAlpha => _hlBaseAlpha;
        public float HLBoostAlpha => _hlBoostAlpha;
        public float HLBoostRampUpDuration => _hlBoostRampUpDuration;
        public float HLBoostRampDownDuration => _hlBoostRampDownDuration;

        // Core
        public float CoreBaseAlpha => _coreBaseAlpha;
        public float CoreBoostAlpha => _coreBoostAlpha;
        public float CoreBoostRampUpDuration => _coreBoostRampUpDuration;
        public float CoreBoostRampDownDuration => _coreBoostRampDownDuration;
        public float CoreLowHPThreshold => _coreLowHPThreshold;
        public Color CoreLowHPColor => _coreLowHPColor;
        public float CoreLowHPPulsePeriod => _coreLowHPPulsePeriod;
        public float CoreLowHPPulseMinAlpha => _coreLowHPPulseMinAlpha;

        // Hit Flash
        public float HitFlashDuration => _hitFlashDuration;
        public Color HitFlashColor => _hitFlashColor;
        public float HitIFrameDuration => _hitIFrameDuration;
        public float HitIFrameBlinkInterval => _hitIFrameBlinkInterval;
        public float HitIFrameDimAlpha => _hitIFrameDimAlpha;

        // ══════════════════════════════════════════════════════════════
        // Boost Trail — Sustain Layer Blending
        // ══════════════════════════════════════════════════════════════

        [Header("Boost Trail — Startup Sequencing")]
        [Tooltip("Delay before sustained flame trails start, giving the startup burst a short head start.")]
        [Min(0f)]
        [SerializeField] private float _boostSustainFlameStartDelay = 0.045f;

        [Tooltip("Delay before EmberTrail joins, so startup layers read before sustained embers take over.")]
        [Min(0f)]
        [SerializeField] private float _boostEmberTrailStartDelay = 0.07f;

        [Tooltip("Delay before EmberSparks fires, so FlameCore remains the first readable ignition cue.")]
        [Min(0f)]
        [SerializeField] private float _boostEmberSparksBurstDelay = 0.018f;

        [Header("Boost Trail — FlameTrail Sustain")]
        [Tooltip("Master BoostIntensity threshold after which FlameTrail begins blending in.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flameTrailBlendInThreshold = 0.18f;

        [Tooltip("Maximum share of the master BoostIntensity granted to FlameTrail once sustain is fully established.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flameTrailMaxIntensity = 0.78f;

        [Header("Boost Trail — EmberTrail Sustain")]
        [Tooltip("Master BoostIntensity threshold after which EmberTrail is allowed to join.")]
        [Range(0f, 1f)]
        [SerializeField] private float _emberTrailBlendInThreshold = 0.42f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EmberTrail once sustain is fully established.")]
        [Range(0f, 1f)]
        [SerializeField] private float _emberTrailMaxIntensity = 0.32f;

        [Header("Boost Trail — EnergyLayer2 Sustain")]
        [Tooltip("Master BoostIntensity threshold after which EnergyLayer2 begins blending in.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer2BlendInThreshold = 0.16f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EnergyLayer2.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer2MaxIntensity = 0.62f;

        [Header("Boost Trail — EnergyLayer3 Sustain")]
        [Tooltip("Master BoostIntensity threshold after which EnergyLayer3 is allowed to appear.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer3BlendInThreshold = 0.38f;

        [Tooltip("Maximum share of the master BoostIntensity granted to EnergyLayer3.")]
        [Range(0f, 1f)]
        [SerializeField] private float _energyLayer3MaxIntensity = 0.34f;

        [Header("Boost Trail — Particle Sustain Curve")]
        [Tooltip("Minimum startSizeMultiplier at zero sustain intensity.")]
        [Min(0f)]
        [SerializeField] private float _sustainParticleMinSize = 0.25f;

        [Tooltip("Minimum startSpeedMultiplier at zero sustain intensity.")]
        [Min(0f)]
        [SerializeField] private float _sustainParticleMinSpeed = 0.45f;

        [Tooltip("Master intensity below this threshold causes sustained particles to auto-stop (eliminates idle 'playing but empty' overhead).")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _sustainParticleStopThreshold = 0.005f;

        [Header("Boost Trail — Intensity Animation")]
        [Tooltip("Duration for _BoostIntensity 0→1 on Boost start.")]
        [Min(0.01f)]
        [SerializeField] private float _boostIntensityRampUpDuration = 0.22f;

        [Tooltip("Duration for _BoostIntensity 1→0 on Boost end.")]
        [Min(0.01f)]
        [SerializeField] private float _boostIntensityRampDownDuration = 0.42f;

        [Header("Boost Trail — TrailRenderer")]
        [Tooltip("Trail fade time in seconds (how long trail persists after stop).")]
        [Min(0f)]
        [SerializeField] private float _boostTrailTime = 2.2f;

        [Tooltip("Trail width multiplier.")]
        [Min(0f)]
        [SerializeField] private float _boostTrailWidthMultiplier = 2.75f;

        [Header("Boost Trail — Bloom Burst")]
        [Tooltip("Peak Bloom Intensity during Boost activation.")]
        [Min(0f)]
        [SerializeField] private float _bloomBurstIntensity = 3.2f;

        [Tooltip("Peak local volume weight during the Boost activation burst.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bloomPeakWeight = 0.88f;

        [Tooltip("Fast attack duration of the Bloom burst.")]
        [Min(0f)]
        [SerializeField] private float _bloomAttackDuration = 0.05f;

        [Tooltip("How long the Bloom stays at peak before fading out.")]
        [Min(0f)]
        [SerializeField] private float _bloomSustainDuration = 0f;

        [Tooltip("Softer release duration of the Bloom burst.")]
        [Min(0f)]
        [SerializeField] private float _bloomReleaseDuration = 0.22f;

        // ── Boost Trail Getters ─────────────────────────────────────
        // Startup Sequencing
        public float BoostSustainFlameStartDelay => _boostSustainFlameStartDelay;
        public float BoostEmberTrailStartDelay => _boostEmberTrailStartDelay;
        public float BoostEmberSparksBurstDelay => _boostEmberSparksBurstDelay;

        // FlameTrail Sustain
        public float FlameTrailBlendInThreshold => _flameTrailBlendInThreshold;
        public float FlameTrailMaxIntensity => _flameTrailMaxIntensity;

        // EmberTrail Sustain
        public float EmberTrailBlendInThreshold => _emberTrailBlendInThreshold;
        public float EmberTrailMaxIntensity => _emberTrailMaxIntensity;

        // EnergyLayer2 Sustain
        public float EnergyLayer2BlendInThreshold => _energyLayer2BlendInThreshold;
        public float EnergyLayer2MaxIntensity => _energyLayer2MaxIntensity;

        // EnergyLayer3 Sustain
        public float EnergyLayer3BlendInThreshold => _energyLayer3BlendInThreshold;
        public float EnergyLayer3MaxIntensity => _energyLayer3MaxIntensity;

        // Particle Sustain Curve
        public float SustainParticleMinSize => _sustainParticleMinSize;
        public float SustainParticleMinSpeed => _sustainParticleMinSpeed;
        public float SustainParticleStopThreshold => _sustainParticleStopThreshold;

        // Intensity Animation
        public float BoostIntensityRampUpDuration => _boostIntensityRampUpDuration;
        public float BoostIntensityRampDownDuration => _boostIntensityRampDownDuration;

        // TrailRenderer
        public float BoostTrailTime => _boostTrailTime;
        public float BoostTrailWidthMultiplier => _boostTrailWidthMultiplier;

        // Bloom Burst
        public float BloomBurstIntensity => _bloomBurstIntensity;
        public float BloomPeakWeight => _bloomPeakWeight;
        public float BloomAttackDuration => _bloomAttackDuration;
        public float BloomSustainDuration => _bloomSustainDuration;
        public float BloomReleaseDuration => _bloomReleaseDuration;

    }
}
