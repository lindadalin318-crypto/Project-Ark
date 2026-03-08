using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// State machine controller for the ship — mirrors GG's Player state machine.
    ///
    /// Architecture:
    ///   - Holds a reference to ShipStatsSO which contains ShipStateData[] array
    ///   - TryToState(): respects minTime lock (used for soft transitions)
    ///   - ToStateForce(): ignores all locks (used by Boost/Dash for hard transitions)
    ///   - On state entry: calls ShipStateData.Apply() to atomically write all physics params
    ///   - On state exit: calls ShipStateData.Disable() to restore colliders
    ///   - Fires OnStateChanged event for ShipView / VFX subscribers
    ///
    /// Usage:
    ///   ShipBoost → ToStateForce(Boost) on start, ToStateForce(Normal) on end
    ///   ShipDash  → ToStateForce(Dash)  on start, ToStateForce(Normal) on end
    ///   ShipView  → subscribe OnStateChanged to drive VFX
    /// </summary>
    [RequireComponent(typeof(ShipMotor))]
    [RequireComponent(typeof(ShipAiming))]
    public class ShipStateController : MonoBehaviour
    {
        [SerializeField] private ShipStatsSO _stats;

        // ══════════════════════════════════════════════════════════════
        // Events
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired whenever the state changes.
        /// Parameters: (previousState, newState).
        /// </summary>
        public event Action<ShipShipState, ShipShipState> OnStateChanged;

        // ══════════════════════════════════════════════════════════════
        // Public Properties
        // ══════════════════════════════════════════════════════════════

        /// <summary>Current active state.</summary>
        public ShipShipState CurrentState { get; private set; } = ShipShipState.Normal;

        /// <summary>
        /// Data block for the current state. May be null if stats are not configured.
        /// </summary>
        public ShipStateData CurrentStateData { get; private set; }

        /// <summary>
        /// When false, TryToState() will be blocked (minTime not yet elapsed).
        /// ToStateForce() always bypasses this.
        /// </summary>
        public bool IsCanChangeState { get; private set; } = true;

        // ══════════════════════════════════════════════════════════════
        // Private State
        // ══════════════════════════════════════════════════════════════

        private ShipMotor    _motor;
        private ShipAiming   _aiming;
        private Animator     _animator;
        private Rigidbody2D  _rb;

        private float _stateEntryTime;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            _motor    = GetComponent<ShipMotor>();
            _aiming   = GetComponent<ShipAiming>();
            _animator = GetComponent<Animator>(); // optional
            _rb       = GetComponent<Rigidbody2D>();

            if (_stats == null)
            {
                Debug.LogError($"[ShipStateController] _stats (ShipStatsSO) is not assigned on " +
                               $"{gameObject.name}! Assign it in the Inspector.", this);
                return;
            }

            // Apply Normal state immediately on Awake
            ApplyState(ShipShipState.Normal, force: true);
        }

        private void Update()
        {
            // Poll minTime: unlock state change once minTime has elapsed
            if (!IsCanChangeState && CurrentStateData != null)
            {
                if (Time.time >= _stateEntryTime + CurrentStateData.minTime)
                    IsCanChangeState = true;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Public API — State Transitions
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Attempts to transition to the requested state.
        /// Blocked if IsCanChangeState is false (minTime not elapsed).
        /// Idempotent: requesting the current state is a no-op.
        /// </summary>
        /// <returns>True if the transition was performed.</returns>
        public bool TryToState(ShipShipState newState)
        {
            if (newState == CurrentState) return false;
            if (!IsCanChangeState) return false;

            ApplyState(newState, force: false);
            return true;
        }

        /// <summary>
        /// Forces an immediate transition to the requested state, ignoring all locks.
        /// Mirrors GG's Player.ToStateForce(). Used by Boost and Dash.
        /// Idempotent: requesting the current state is a no-op.
        /// </summary>
        public void ToStateForce(ShipShipState newState)
        {
            if (newState == CurrentState) return;
            ApplyState(newState, force: true);
        }

        /// <summary>
        /// Returns true if the ship is currently in the specified state.
        /// Replaces the scattered IsBoosting / IsDashing properties.
        /// </summary>
        public bool IsInState(ShipShipState state) => CurrentState == state;

        // ══════════════════════════════════════════════════════════════
        // Internal — State Application
        // ══════════════════════════════════════════════════════════════

        private void ApplyState(ShipShipState newState, bool force)
        {
            ShipShipState prevState = CurrentState;

            // Exit current state: restore colliders
            CurrentStateData?.Disable();

            // Fetch new state data
            ShipStateData newData = _stats != null ? _stats.GetStateData(newState) : null;

            // Update state
            CurrentState     = newState;
            CurrentStateData = newData;
            _stateEntryTime  = Time.time;

            // Lock state changes until minTime elapses (only if minTime > 0)
            IsCanChangeState = (newData == null || newData.minTime <= 0f);

            // Apply new state parameters
            newData?.Apply(_rb, _motor, _aiming, _animator);

            // Notify subscribers
            OnStateChanged?.Invoke(prevState, newState);
        }
    }
}
