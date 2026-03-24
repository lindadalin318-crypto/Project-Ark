using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Lightweight VFX coordinator for the ship's multi-layer sprite system.
    ///
    /// Responsibilities:
    ///   1. Hold all 5 sprite layer references (Back / Liquid / HL / Solid / Core)
    ///   2. Capture baseline colors at Awake and distribute to workers
    ///   3. Subscribe to ShipStateController.OnStateChanged + ShipHealth.OnDamageTaken + ShipMotor.OnSpeedChanged
    ///   4. Route events to the appropriate worker:
    ///      - ShipBoostVisuals  (boost sprite swap, glow, trail, thruster pulse)
    ///      - ShipHitVisuals    (hit flash, i-frame blink, low-HP pulse)
    ///      - ShipDashVisuals   (i-frame flicker, dodge ghost, after-images via DashAfterImageSpawner)
    ///      - ShipVisualJuice   (movement tilt, squash/stretch)
    ///   5. Provide ResetVFX() for object pool return
    ///
    /// This class no longer contains any VFX implementation — all visual logic lives in Workers.
    /// </summary>
    public class ShipView : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // Serialized References — 5-layer sprite structure
        // ══════════════════════════════════════════════════════════════

        [Header("Sprite Layers")]
        [Tooltip("Rear thruster layer (SortOrder -3).")]
        [SerializeField] private SpriteRenderer _backRenderer;

        [Tooltip("Energy/glow layer (SortOrder -2). Uses Additive material.")]
        [SerializeField] private SpriteRenderer _liquidRenderer;

        [Tooltip("Highlight layer (SortOrder -1). Default alpha 0.5.")]
        [SerializeField] private SpriteRenderer _hlRenderer;

        [Tooltip("Main solid body layer (SortOrder 0).")]
        [SerializeField] private SpriteRenderer _solidRenderer;

        [Tooltip("Core/cockpit layer (SortOrder 1). Placeholder — no sprite required.")]
        [SerializeField] private SpriteRenderer _coreRenderer;

        // ══════════════════════════════════════════════════════════════
        // Serialized References — VFX Workers
        // ══════════════════════════════════════════════════════════════

        [Header("VFX Workers")]
        [SerializeField] private ShipBoostVisuals _boostVisuals;
        [SerializeField] private ShipHitVisuals _hitVisuals;
        [SerializeField] private ShipDashVisuals _dashVisuals;
        [SerializeField] private ShipVisualJuice _juiceVisuals;
        [SerializeField] private DashAfterImageSpawner _afterImageSpawner;

        [Header("VFX Enable Toggles")]
        [Tooltip("Master switch for all Boost visuals (sprite swap, glow, trail, thruster pulse).")]
        [SerializeField] private bool _enableBoostVFX = true;

        [Tooltip("Master switch for all hit visuals (white flash, i-frame blink, low-HP pulse).")]
        [SerializeField] private bool _enableHitVFX = true;

        [Tooltip("Master switch for all dash visuals (i-frame flicker, dodge ghost, after-images).")]
        [SerializeField] private bool _enableDashVFX = true;

        [Tooltip("Master switch for all juice visuals (movement tilt, squash/stretch).")]
        [SerializeField] private bool _enableJuiceVFX = true;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        // ══════════════════════════════════════════════════════════════
        // Cached Components
        // ══════════════════════════════════════════════════════════════

        private ShipMotor _motor;
        private ShipDash _dash;
        private ShipBoost _boost;
        private ShipAiming _aiming;
        private ShipStateController _stateController;
        private ShipHealth _shipHealth;

        // Baseline colors captured at Awake so workers can restore correctly
        private Color _liquidBaseColor;
        private Color _solidBaseColor;
        private Color _hlBaseColor;
        private Color _coreBaseColor;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
            _dash = GetComponent<ShipDash>();
            _boost = GetComponent<ShipBoost>();
            _aiming = GetComponent<ShipAiming>();
            _stateController = GetComponent<ShipStateController>();
            _shipHealth = GetComponent<ShipHealth>();

            if (_stateController == null)
                Debug.LogError("[ShipView] Missing ShipStateController. VFX routing will not work.", this);

            if (_shipHealth == null)
                Debug.LogWarning("[ShipView] Missing ShipHealth. Hit flash and i-frame blink will not work.", this);

            // Capture baseline colors from sprite renderers
            CaptureBaselineColors();

            // Initialize workers with baseline colors and component references
            InitializeWorkers();
        }

        private void OnEnable()
        {
            if (_stateController != null)
                _stateController.OnStateChanged += HandleStateChanged;

            if (_shipHealth != null)
                _shipHealth.OnDamageTaken += HandleDamageTaken;

            // Route speed changes for juice (squash/stretch)
            if (_motor != null)
                _motor.OnSpeedChanged += HandleSpeedChanged;
        }

        private void OnDisable()
        {
            if (_stateController != null)
                _stateController.OnStateChanged -= HandleStateChanged;

            if (_shipHealth != null)
                _shipHealth.OnDamageTaken -= HandleDamageTaken;

            if (_motor != null)
                _motor.OnSpeedChanged -= HandleSpeedChanged;

            ResetVFX();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Resets all VFX to their initial state.
        /// Call this when returning the ship to an object pool.
        /// </summary>
        public void ResetVFX()
        {
            if (_boostVisuals != null) _boostVisuals.ResetState();
            if (_hitVisuals != null) _hitVisuals.ResetState();
            if (_dashVisuals != null) _dashVisuals.ResetState();
            if (_juiceVisuals != null) _juiceVisuals.ResetState();
            // Note: _afterImageSpawner.CancelSpawning() is already called
            // by _dashVisuals.ResetState() via second-level delegation.
        }

        // ══════════════════════════════════════════════════════════════
        // Initialization Helpers
        // ══════════════════════════════════════════════════════════════

        private void CaptureBaselineColors()
        {
            if (_liquidRenderer != null)
                _liquidBaseColor = _liquidRenderer.color;

            if (_solidRenderer != null)
                _solidBaseColor = _solidRenderer.color;

            // HL layer: initialize with configurable base alpha
            if (_hlRenderer != null)
            {
                _hlBaseColor = _hlRenderer.color;
                float hlAlpha = _juiceSettings != null ? _juiceSettings.HLBaseAlpha : 0.5f;
                var c = _hlBaseColor;
                c.a = hlAlpha;
                _hlRenderer.color = c;
                _hlBaseColor = c;
            }

            // Core layer: initialize with configurable base alpha
            if (_coreRenderer != null)
            {
                _coreBaseColor = _coreRenderer.color;
                float coreAlpha = _juiceSettings != null ? _juiceSettings.CoreBaseAlpha : 0.3f;
                var c = _coreBaseColor;
                c.a = coreAlpha;
                _coreRenderer.color = c;
                _coreBaseColor = c;
            }
        }

        private void InitializeWorkers()
        {
            if (_boostVisuals != null)
                _boostVisuals.Initialize(_liquidBaseColor, _hlBaseColor, _coreBaseColor);

            if (_hitVisuals != null)
                _hitVisuals.Initialize(_liquidBaseColor, _solidBaseColor, _hlBaseColor, _coreBaseColor, _shipHealth);

            if (_dashVisuals != null)
                _dashVisuals.Initialize(_solidBaseColor, _hlBaseColor, _coreBaseColor);

            if (_juiceVisuals != null)
                _juiceVisuals.Initialize(_motor, _dash, _boost, _aiming);

            if (_afterImageSpawner != null)
                _afterImageSpawner.Initialize(_dash);
        }

        // ══════════════════════════════════════════════════════════════
        // Event Routing
        // ══════════════════════════════════════════════════════════════

        private void HandleStateChanged(ShipShipState prevState, ShipShipState newState)
        {
            switch (newState)
            {
                case ShipShipState.Boost:
                    if (_enableBoostVFX && _boostVisuals != null) _boostVisuals.OnBoostStarted();
                    break;

                case ShipShipState.Dash:
                    Vector2 dashDir = (_dash != null) ? _dash.DashDirection : Vector2.zero;
                    if (_enableDashVFX && _dashVisuals != null) _dashVisuals.OnDashStarted(dashDir);
                    if (_enableJuiceVFX && _juiceVisuals != null) _juiceVisuals.OnDashStarted();
                    break;

                case ShipShipState.Normal:
                    if (prevState == ShipShipState.Boost && _enableBoostVFX && _boostVisuals != null)
                        _boostVisuals.OnBoostEnded();
                    else if (prevState == ShipShipState.Dash && _enableDashVFX && _dashVisuals != null)
                        _dashVisuals.OnDashEnded();
                    break;
            }
        }

        private void HandleSpeedChanged(float normalizedSpeed)
        {
            if (_enableJuiceVFX && _juiceVisuals != null)
                _juiceVisuals.OnSpeedChanged(normalizedSpeed);
        }

        private void HandleDamageTaken(float damage, float currentHP)
        {
            if (_enableHitVFX && _hitVisuals != null)
                _hitVisuals.OnDamageTaken(damage, currentHP);
        }
    }
}
