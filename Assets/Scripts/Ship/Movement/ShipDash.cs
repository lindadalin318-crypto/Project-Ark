using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Ship dash / dodge component. Listens to InputHandler.OnDashPressed,
    /// executes a fixed-duration dash, manages cooldown, input buffering,
    /// and optional invincibility frames.
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    [RequireComponent(typeof(InputHandler))]
    public class ShipDash : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary> Fired when a dash begins. Provides dash direction. </summary>
        public event Action<Vector2> OnDashStarted;

        /// <summary> Fired when a dash ends. </summary>
        public event Action OnDashEnded;

        // ══════════════════════════════════════════════════════════════
        // Public State
        // ══════════════════════════════════════════════════════════════

        /// <summary> True while a dash is executing. </summary>
        public bool IsDashing { get; private set; }

        /// <summary> Current dash direction (valid during dash). </summary>
        public Vector2 DashDirection { get; private set; }

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private ShipMotor _motor;
        private InputHandler _inputHandler;
        private ShipAiming _aiming;
        private ShipHealth _health;

        private readonly InputBuffer _inputBuffer = new();
        private const string DASH_ACTION = "Dash";

        private bool _isCoolingDown;
        private float _cooldownEndTime;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
            _inputHandler = GetComponent<InputHandler>();
            _aiming = GetComponent<ShipAiming>();
            _health = GetComponent<ShipHealth>();
        }

        private void OnEnable()
        {
            _inputHandler.OnDashPressed += HandleDashInput;
        }

        private void OnDisable()
        {
            _inputHandler.OnDashPressed -= HandleDashInput;
        }

        private void Update()
        {
            // Check if cooldown expired and there's a buffered dash input
            if (_isCoolingDown && Time.time >= _cooldownEndTime)
            {
                _isCoolingDown = false;

                // Try to consume buffered dash
                if (_inputBuffer.Consume(DASH_ACTION, _stats.DashBufferWindow))
                {
                    ExecuteDashAsync().Forget();
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Input Handling
        // ══════════════════════════════════════════════════════════════

        private void HandleDashInput()
        {
            if (IsDashing)
            {
                // Already dashing — buffer the input for later
                _inputBuffer.Record(DASH_ACTION);
                return;
            }

            if (_isCoolingDown)
            {
                // On cooldown — buffer the input
                _inputBuffer.Record(DASH_ACTION);
                return;
            }

            // Ready to dash
            ExecuteDashAsync().Forget();
        }

        // ══════════════════════════════════════════════════════════════
        // Dash Execution
        // ══════════════════════════════════════════════════════════════

        private async UniTaskVoid ExecuteDashAsync()
        {
            if (IsDashing) return;

            // ── 1. Determine dash direction ──
            Vector2 moveInput = _inputHandler.MoveInput;
            Vector2 dashDir;

            if (moveInput.sqrMagnitude > 0.1f)
            {
                dashDir = moveInput.normalized;
            }
            else if (_aiming != null)
            {
                dashDir = _aiming.FacingDirection;
            }
            else
            {
                dashDir = transform.up; // Fallback
            }

            DashDirection = dashDir;

            // ── 2. Start dash ──
            IsDashing = true;
            _motor.IsDashing = true;

            Vector2 dashVelocity = dashDir * _stats.DashSpeed;
            _motor.SetVelocityOverride(dashVelocity);

            // ── 3. I-Frames ──
            bool hadIFrames = false;
            if (_stats.DashIFrames && _health != null)
            {
                _health.SetInvulnerable(true);
                hadIFrames = true;
            }

            // ── 4. Broadcast start event ──
            OnDashStarted?.Invoke(dashDir);

            // ── 5. Wait for dash duration ──
            int durationMs = Mathf.RoundToInt(_stats.DashDuration * 1000f);
            await UniTask.Delay(durationMs, cancellationToken: destroyCancellationToken);

            // ── 6. End dash — preserve exit momentum ──
            _motor.ClearVelocityOverride();
            _motor.IsDashing = false;
            IsDashing = false;

            // Apply exit momentum as impulse in dash direction
            float exitSpeed = _stats.DashSpeed * _stats.DashExitSpeedRatio;
            _motor.ApplyImpulse(dashDir * exitSpeed);

            // ── 7. Disable dash i-frames (post-damage i-frames handled by ShipHealth) ──
            if (hadIFrames && _health != null)
            {
                _health.SetInvulnerable(false);
            }

            // ── 8. Broadcast end event ──
            OnDashEnded?.Invoke();

            // ── 9. Start cooldown ──
            _isCoolingDown = true;
            _cooldownEndTime = Time.time + _stats.DashCooldown;
        }
    }
}
