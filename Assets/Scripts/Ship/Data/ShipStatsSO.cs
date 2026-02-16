using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Data asset for ship movement, aiming, dash, and hit-feedback configuration.
    /// All tunable values live here — never hardcode in MonoBehaviours.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShipStats", menuName = "ProjectArk/Ship/Ship Stats")]
    public class ShipStatsSO : ScriptableObject
    {
        // ══════════════════════════════════════════════════════════════
        // Movement — Base
        // ══════════════════════════════════════════════════════════════

        [Header("Movement — Base")]
        [Tooltip("Maximum movement speed in units/second.")]
        [SerializeField] private float _moveSpeed = 12f;

        [Tooltip("How quickly the ship reaches max speed (units/sec²). Higher = snappier.")]
        [SerializeField] private float _acceleration = 45f;

        [Tooltip("How quickly the ship slows down when no input (units/sec²). Lower = more slide.")]
        [SerializeField] private float _deceleration = 25f;

        // ══════════════════════════════════════════════════════════════
        // Movement — Curves & Feel
        // ══════════════════════════════════════════════════════════════

        [Header("Movement — Curves & Feel")]
        [Tooltip("Acceleration curve (t: 0→1 = idle→maxSpeed). Y maps to acceleration multiplier.")]
        [SerializeField] private AnimationCurve _accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Deceleration curve (t: 0→1 = maxSpeed→idle). Y maps to deceleration multiplier.")]
        [SerializeField] private AnimationCurve _decelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Angle threshold (degrees) beyond which a sharp-turn penalty is applied.")]
        [Range(30f, 180f)]
        [SerializeField] private float _sharpTurnAngleThreshold = 90f;

        [Tooltip("Speed multiplier applied during sharp turns (0 = full stop, 1 = no penalty).")]
        [Range(0f, 1f)]
        [SerializeField] private float _sharpTurnSpeedPenalty = 0.7f;

        [Tooltip("Acceleration multiplier applied during the first moments of movement.")]
        [Min(1f)]
        [SerializeField] private float _initialBoostMultiplier = 1.5f;

        [Tooltip("Duration (seconds) of the initial boost after starting to move.")]
        [Min(0f)]
        [SerializeField] private float _initialBoostDuration = 0.05f;

        [Tooltip("Normalized speed below which the ship is considered 'stopped'.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _minMoveSpeedThreshold = 0.1f;

        [Header("Movement — Advanced (New!)")]
        [Tooltip("Controls how snappy direction changes feel (0 = slidey, 1 = instant direction change).")]
        [Range(0f, 1f)]
        [SerializeField] private float _directionChangeSnappiness = 0.65f;

        [Tooltip("Minimum speed (units/sec) before input starts affecting direction. Prevents jitter at very low speeds.")]
        [Min(0f)]
        [SerializeField] private float _minSpeedForDirectionChange = 0.5f;

        // ══════════════════════════════════════════════════════════════
        // Aiming
        // ══════════════════════════════════════════════════════════════

        [Header("Aiming")]
        [Tooltip("Rotation speed in degrees/second. 0 = instant snap to target.")]
        [SerializeField] private float _rotationSpeed = 720f;

        // ══════════════════════════════════════════════════════════════
        // Dash
        // ══════════════════════════════════════════════════════════════

        [Header("Dash")]
        [Tooltip("Dash movement speed in units/second.")]
        [Min(0f)]
        [SerializeField] private float _dashSpeed = 30f;

        [Tooltip("Duration of the dash in seconds.")]
        [Min(0.01f)]
        [SerializeField] private float _dashDuration = 0.15f;

        [Tooltip("Cooldown between dashes in seconds.")]
        [Min(0f)]
        [SerializeField] private float _dashCooldown = 0.3f;

        [Tooltip("Input buffer window for dash (seconds). Presses within this window before cooldown ends are queued.")]
        [Min(0f)]
        [SerializeField] private float _dashBufferWindow = 0.15f;

        [Tooltip("Ratio of dash speed preserved as exit momentum (0 = full stop, 1 = full speed).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dashExitSpeedRatio = 0.5f;

        [Tooltip("Whether the ship gains invincibility frames during dash.")]
        [SerializeField] private bool _dashIFrames = true;

        // ══════════════════════════════════════════════════════════════
        // Survival
        // ══════════════════════════════════════════════════════════════

        [Header("Survival")]
        [Tooltip("Maximum hit points for the ship.")]
        [SerializeField] private float _maxHP = 100f;

        [Tooltip("Duration of the white flash when the ship takes damage (seconds).")]
        [SerializeField] private float _hitFlashDuration = 0.1f;

        // ══════════════════════════════════════════════════════════════
        // Hit Feedback
        // ══════════════════════════════════════════════════════════════

        [Header("Hit Feedback")]
        [Tooltip("HitStop freeze duration in seconds (0 = disabled).")]
        [Min(0f)]
        [SerializeField] private float _hitStopDuration = 0.05f;

        [Tooltip("Invincibility frame duration after taking damage (seconds).")]
        [Min(0f)]
        [SerializeField] private float _iFrameDuration = 1.0f;

        [Tooltip("Blink interval during invincibility frames (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float _iFrameBlinkInterval = 0.1f;

        [Tooltip("Base screen-shake intensity on hit.")]
        [Min(0f)]
        [SerializeField] private float _screenShakeBaseIntensity = 0.3f;

        [Tooltip("Additional screen-shake intensity per point of damage.")]
        [Min(0f)]
        [SerializeField] private float _screenShakeDamageScale = 0.01f;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Movement Base
        // ══════════════════════════════════════════════════════════════

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Movement Curves & Feel
        // ══════════════════════════════════════════════════════════════

        public AnimationCurve AccelerationCurve => _accelerationCurve;
        public AnimationCurve DecelerationCurve => _decelerationCurve;
        public float SharpTurnAngleThreshold => _sharpTurnAngleThreshold;
        public float SharpTurnSpeedPenalty => _sharpTurnSpeedPenalty;
        public float InitialBoostMultiplier => _initialBoostMultiplier;
        public float InitialBoostDuration => _initialBoostDuration;
        public float MinMoveSpeedThreshold => _minMoveSpeedThreshold;
        public float DirectionChangeSnappiness => _directionChangeSnappiness;
        public float MinSpeedForDirectionChange => _minSpeedForDirectionChange;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Aiming
        // ══════════════════════════════════════════════════════════════

        public float RotationSpeed => _rotationSpeed;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Dash
        // ══════════════════════════════════════════════════════════════

        public float DashSpeed => _dashSpeed;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;
        public float DashBufferWindow => _dashBufferWindow;
        public float DashExitSpeedRatio => _dashExitSpeedRatio;
        public bool DashIFrames => _dashIFrames;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Survival
        // ══════════════════════════════════════════════════════════════

        public float MaxHP => _maxHP;
        public float HitFlashDuration => _hitFlashDuration;

        // ══════════════════════════════════════════════════════════════
        // Public Getters — Hit Feedback
        // ══════════════════════════════════════════════════════════════

        public float HitStopDuration => _hitStopDuration;
        public float IFrameDuration => _iFrameDuration;
        public float IFrameBlinkInterval => _iFrameBlinkInterval;
        public float ScreenShakeBaseIntensity => _screenShakeBaseIntensity;
        public float ScreenShakeDamageScale => _screenShakeDamageScale;
    }
}
