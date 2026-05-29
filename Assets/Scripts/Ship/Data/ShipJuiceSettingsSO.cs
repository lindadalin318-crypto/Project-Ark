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

        [Tooltip("Peak local scale applied to readable ship layers on actual damage hit.")]
        [Min(1f)]
        [SerializeField] private float _hitImpactScalePeak = 1.08f;

        [Tooltip("How long the hit impact scale stays at peak before releasing.")]
        [Min(0.005f)]
        [SerializeField] private float _hitImpactAttackDuration = 0.025f;

        [Tooltip("How long readable ship layers take to return to normal scale after hit impact.")]
        [Min(0.005f)]
        [SerializeField] private float _hitImpactReleaseDuration = 0.08f;

        // ══════════════════════════════════════════════════════════════
        // Fire Feedback — short muzzle/core flash
        // ══════════════════════════════════════════════════════════════

        [Header("Fire Feedback")]
        [Tooltip("Color applied to the weapon mount layer on weapon fire.")]
        [SerializeField] private Color _fireWeaponMountFlashColor = new Color(0.4f, 1.4f, 1.8f, 1f);

        [Tooltip("Color applied to the core layer on weapon fire.")]
        [SerializeField] private Color _fireCorePulseColor = new Color(0.5f, 1.6f, 2.2f, 1f);

        [Tooltip("Peak local scale for the weapon mount fire flash.")]
        [Min(1f)]
        [SerializeField] private float _fireWeaponMountScalePeak = 1.14f;

        [Tooltip("Peak local scale for the core fire pulse.")]
        [Min(1f)]
        [SerializeField] private float _fireCoreScalePeak = 1.08f;

        [Tooltip("How long the fire flash stays at peak before restoring color.")]
        [Min(0.005f)]
        [SerializeField] private float _fireFlashAttackDuration = 0.025f;

        [Tooltip("Delay after color restore before scale returns to normal.")]
        [Min(0.005f)]
        [SerializeField] private float _fireFlashReleaseDuration = 0.08f;

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
        public float HitImpactScalePeak => _hitImpactScalePeak;
        public float HitImpactAttackDuration => _hitImpactAttackDuration;
        public float HitImpactReleaseDuration => _hitImpactReleaseDuration;

        // Fire Feedback
        public Color FireWeaponMountFlashColor => _fireWeaponMountFlashColor;
        public Color FireCorePulseColor => _fireCorePulseColor;
        public float FireWeaponMountScalePeak => _fireWeaponMountScalePeak;
        public float FireCoreScalePeak => _fireCoreScalePeak;
        public float FireFlashAttackDuration => _fireFlashAttackDuration;
        public float FireFlashReleaseDuration => _fireFlashReleaseDuration;

        // ══════════════════════════════════════════════════════════════
        // Boost Trail — Ares Chain Bloom
        // ══════════════════════════════════════════════════════════════

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
        // Bloom Burst
        public float BloomBurstIntensity => _bloomBurstIntensity;
        public float BloomPeakWeight => _bloomPeakWeight;
        public float BloomAttackDuration => _bloomAttackDuration;
        public float BloomSustainDuration => _bloomSustainDuration;
        public float BloomReleaseDuration => _bloomReleaseDuration;

    }
}
