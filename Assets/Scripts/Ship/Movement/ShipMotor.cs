using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles ship physics-based movement using Rigidbody2D.
    /// Reads input from InputHandler. Does NOT handle rotation (that's ShipAiming).
    ///
    /// Movement model: curve-driven acceleration/deceleration with sharp-turn penalty
    /// and initial-boost burst. Provides velocity override API for Dash system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipMotor : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired each physics frame with normalized speed (0..1).
        /// Subscribe for VFX intensity, audio pitch, etc.
        /// </summary>
        public event Action<float> OnSpeedChanged;

        // ══════════════════════════════════════════════════════════════
        // Public Properties
        // ══════════════════════════════════════════════════════════════

        public Vector2 CurrentVelocity => _rigidbody.linearVelocity;
        public float CurrentSpeed => _rigidbody.linearVelocity.magnitude;
        public float NormalizedSpeed => _stats.MoveSpeed > 0f
            ? Mathf.Clamp01(_rigidbody.linearVelocity.magnitude / _stats.MoveSpeed)
            : 0f;

        /// <summary>
        /// True when the dash system has taken control of velocity.
        /// Normal movement logic is skipped while dashing.
        /// </summary>
        public bool IsDashing { get; set; }

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private Rigidbody2D _rigidbody;
        private InputHandler _inputHandler;
        private float _previousNormalizedSpeed;

        // Curve-driven acceleration tracking
        private float _accelerationProgress; // 0→1, how far along the accel curve
        private float _decelerationProgress; // 0→1, how far along the decel curve
        private float _timeSinceStartedMoving;
        private bool _wasMoving;

        // Velocity override (used by ShipDash)
        private bool _hasVelocityOverride;
        private Vector2 _velocityOverride;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _inputHandler = GetComponent<InputHandler>();
        }

        private void FixedUpdate()
        {
            if (IsDashing)
            {
                // During dash, apply velocity override if set
                if (_hasVelocityOverride)
                    _rigidbody.linearVelocity = _velocityOverride;

                EmitSpeedEvent();
                return;
            }

            HandleMovement();
            EmitSpeedEvent();
        }

        // ══════════════════════════════════════════════════════════════
        // Curve-Driven Movement
        // ══════════════════════════════════════════════════════════════

        private void HandleMovement()
        {
            Vector2 input = _inputHandler.MoveInput;
            Vector2 currentVel = _rigidbody.linearVelocity;
            float dt = Time.fixedDeltaTime;
            bool isMoving = input.sqrMagnitude > 0.001f;

            if (isMoving)
            {
                // Track time since movement started (for initial boost)
                if (!_wasMoving)
                {
                    _timeSinceStartedMoving = 0f;
                    _accelerationProgress = 0f;
                }
                _timeSinceStartedMoving += dt;

                // Reset deceleration tracking
                _decelerationProgress = 0f;

                // ── Sharp Turn Detection ──
                float sharpTurnMultiplier = 1f;
                if (currentVel.sqrMagnitude > 0.01f)
                {
                    float angleBetween = Vector2.Angle(currentVel.normalized, input.normalized);
                    if (angleBetween > _stats.SharpTurnAngleThreshold)
                    {
                        // Penalize speed during sharp turns
                        sharpTurnMultiplier = _stats.SharpTurnSpeedPenalty;
                        float penalizedSpeed = currentVel.magnitude * sharpTurnMultiplier;
                        currentVel = currentVel.normalized * penalizedSpeed;
                        _rigidbody.linearVelocity = currentVel;
                    }
                }

                // ── Curve-Driven Acceleration ──
                float targetSpeed = _stats.MoveSpeed;
                float currentSpeed = currentVel.magnitude;

                // Advance acceleration progress based on current speed ratio
                _accelerationProgress = Mathf.Clamp01(currentSpeed / targetSpeed);
                float curveMultiplier = _stats.AccelerationCurve.Evaluate(_accelerationProgress);
                // Ensure minimum curve value so we can always start moving
                curveMultiplier = Mathf.Max(curveMultiplier, 0.1f);

                float baseAccel = _stats.Acceleration * curveMultiplier;

                // ── Initial Boost ──
                if (_timeSinceStartedMoving <= _stats.InitialBoostDuration)
                {
                    baseAccel *= _stats.InitialBoostMultiplier;
                }

                // ── Apply acceleration toward desired velocity ──
                Vector2 desiredVelocity = input * targetSpeed;
                Vector2 velocityDiff = desiredVelocity - currentVel;
                float maxStep = baseAccel * dt;

                if (velocityDiff.sqrMagnitude <= maxStep * maxStep)
                {
                    _rigidbody.linearVelocity = desiredVelocity;
                }
                else
                {
                    _rigidbody.linearVelocity = currentVel + velocityDiff.normalized * maxStep;
                }
            }
            else
            {
                // ── Curve-Driven Deceleration ──
                _wasMoving = false;
                _accelerationProgress = 0f;

                float speed = currentVel.magnitude;
                if (speed < _stats.MinMoveSpeedThreshold)
                {
                    _rigidbody.linearVelocity = Vector2.zero;
                    _decelerationProgress = 0f;
                }
                else
                {
                    // Advance deceleration progress (1 = was at max speed, 0 = stopped)
                    _decelerationProgress = Mathf.Clamp01(1f - (speed / _stats.MoveSpeed));
                    float curveMultiplier = _stats.DecelerationCurve.Evaluate(_decelerationProgress);
                    curveMultiplier = Mathf.Max(curveMultiplier, 0.1f);

                    float decelStep = _stats.Deceleration * curveMultiplier * dt;

                    if (speed <= decelStep)
                    {
                        _rigidbody.linearVelocity = Vector2.zero;
                    }
                    else
                    {
                        _rigidbody.linearVelocity = currentVel.normalized * (speed - decelStep);
                    }
                }
            }

            _wasMoving = isMoving;

            // ── Final safety speed clamp ──
            if (_rigidbody.linearVelocity.sqrMagnitude > _stats.MoveSpeed * _stats.MoveSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _stats.MoveSpeed;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — Impulse (backward compatible)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Applies an instantaneous velocity impulse (e.g., weapon recoil).
        /// The existing deceleration logic will naturally dampen it.
        /// </summary>
        public void ApplyImpulse(Vector2 impulse)
        {
            _rigidbody.linearVelocity += impulse;
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — Velocity Override (used by ShipDash)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets a velocity override. While active, FixedUpdate applies this velocity each frame.
        /// Used by ShipDash to maintain consistent dash speed.
        /// </summary>
        public void SetVelocityOverride(Vector2 velocity)
        {
            _hasVelocityOverride = true;
            _velocityOverride = velocity;
            _rigidbody.linearVelocity = velocity;
        }

        /// <summary>
        /// Clears the velocity override, returning control to normal movement.
        /// </summary>
        public void ClearVelocityOverride()
        {
            _hasVelocityOverride = false;
            _velocityOverride = Vector2.zero;
        }

        // ══════════════════════════════════════════════════════════════
        // Speed Event
        // ══════════════════════════════════════════════════════════════

        private void EmitSpeedEvent()
        {
            float normalized = NormalizedSpeed;
            if (!Mathf.Approximately(normalized, _previousNormalizedSpeed))
            {
                OnSpeedChanged?.Invoke(normalized);
                _previousNormalizedSpeed = normalized;
            }
        }
    }
}
