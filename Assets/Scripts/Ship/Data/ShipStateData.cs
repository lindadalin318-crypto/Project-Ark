using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Serializable data package for a single ship state.
    /// Mirrors GG's StateData class: encapsulates all physics parameters for one state,
    /// applied atomically via Apply() when the state machine transitions.
    /// </summary>
    [System.Serializable]
    public class ShipStateData
    {
        // ══════════════════════════════════════════════════════════════
        // Identity
        // ══════════════════════════════════════════════════════════════

        [Tooltip("Which state this data block belongs to.")]
        public ShipShipState state = ShipShipState.Normal;

        [Tooltip("Animator trigger name to fire on state entry. Leave empty to skip.")]
        public string animatorTrigger = "";

        [Tooltip("Minimum time (s) this state must remain active before TryToState() can exit it. " +
                 "ToStateForce() ignores this. GG: minTime=0.2s for Boost, 0.225s for Dodge.")]
        [Min(0f)]
        public float minTime = 0f;

        // ══════════════════════════════════════════════════════════════
        // Physics — Linear
        // ══════════════════════════════════════════════════════════════

        [Header("Linear Physics")]
        [Tooltip("Rigidbody2D.linearDamping for this state. GG Normal=3, Boost=2.5, Dash=1.7.")]
        [Min(0f)]
        public float linearDrag = 3f;

        [Tooltip("Rigidbody2D.angularDamping for this state. Usually 0.")]
        [Min(0f)]
        public float angularDrag = 0f;

        [Tooltip("Forward thrust acceleration (units/s²). Applied each FixedUpdate while input held.")]
        [Min(0f)]
        public float moveAcceleration = 20f;

        [Tooltip("Maximum linear speed (units/s). Velocity is clamped to this each FixedUpdate.")]
        [Min(0f)]
        public float maxMoveSpeed = 8f;

        // ══════════════════════════════════════════════════════════════
        // Physics — Angular
        // ══════════════════════════════════════════════════════════════

        [Header("Angular Physics")]
        [Tooltip("Angular acceleration (deg/s²). GG Normal=800, Boost=400, Dash=200 (mass=1 adapted).")]
        [Min(0f)]
        public float angularAcceleration = 800f;

        [Tooltip("Maximum angular speed (deg/s). GG Normal=360, Boost=360, Dash=180.")]
        [Min(0f)]
        public float maxRotationSpeed = 360f;

        // ══════════════════════════════════════════════════════════════
        // Colliders — i-frame management (Dash invulnerability)
        // ══════════════════════════════════════════════════════════════

        [Header("Colliders (i-frame)")]
        [Tooltip("Colliders to disable while this state is active (e.g. main hull collider during Dash). " +
                 "Mirrors GG StateData.colliders[]. Leave empty for states that don't need i-frames.")]
        public Collider2D[] colliders = System.Array.Empty<Collider2D>();

        // ══════════════════════════════════════════════════════════════
        // Apply / Disable
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Atomically applies all physics parameters to the ship components.
        /// Mirrors GG StateData.Apply(). Called by ShipStateController on state entry.
        /// </summary>
        /// <param name="rb">Ship Rigidbody2D.</param>
        /// <param name="motor">ShipMotor — receives maxMoveSpeed and acceleration.</param>
        /// <param name="aiming">ShipAiming — receives angular parameters.</param>
        /// <param name="animator">Optional Animator — fires animatorTrigger if set.</param>
        public void Apply(Rigidbody2D rb, ShipMotor motor, ShipAiming aiming, Animator animator)
        {
            // ── Rigidbody2D physics
            if (rb != null)
            {
                rb.linearDamping  = linearDrag;
                rb.angularDamping = angularDrag;
            }

            // ── Motor parameters (written to runtime fields, not SO)
            if (motor != null)
            {
                motor.RuntimeMaxSpeed        = maxMoveSpeed;
                motor.RuntimeMoveAcceleration = moveAcceleration;
            }

            // ── Aiming parameters
            if (aiming != null)
            {
                aiming.RuntimeAngularAcceleration = angularAcceleration;
                aiming.RuntimeMaxRotationSpeed    = maxRotationSpeed;
            }

            // ── Animator trigger
            if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
                animator.SetTrigger(animatorTrigger);

            // ── Disable colliders (i-frame)
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        /// <summary>
        /// Re-enables all colliders that were disabled by Apply().
        /// Mirrors GG StateData.Disable(). Called by ShipStateController on state exit.
        /// </summary>
        public void Disable()
        {
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = true;
            }
        }
    }
}
