using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Shooter-type enemy brain. Inherits from EnemyBrain and overrides the HFSM
    /// to wire a ranged combat state graph:
    ///   Idle → Chase → Shoot (Telegraph→Burst→Recovery) → Retreat → Return
    ///
    /// Key behavioral differences from Rusher (EnemyBrain):
    ///   - Uses ShootState instead of EngageState (ranged burst fire vs melee)
    ///   - Has a RetreatState to maintain preferred engagement distance
    ///   - Chase transitions to Shoot when within PreferredRange (not AttackRange)
    /// </summary>
    public class ShooterBrain : EnemyBrain
    {
        // ──────────────────── Shooter-Specific States ────────────────────
        private ShootState _shootState;
        private RetreatState _retreatState;

        // ──────────────────── Public Accessors (read by states) ────────────────────

        /// <summary> The ranged attack state. Used by RetreatState to transition back. </summary>
        public ShootState ShootState => _shootState;

        /// <summary> The retreat state. Used by ShootState when player gets too close. </summary>
        public RetreatState RetreatState => _retreatState;

        // ──────────────────── HFSM Construction ────────────────────

        /// <summary>
        /// Build the shooter's HFSM with five tactical states.
        /// Calls base.BuildStateMachine() to populate shared states (Idle, Chase, Return),
        /// then creates shooter-specific states and re-initializes the FSM.
        /// Note: base also creates EngageState (unused by shooter, but harmless — zero overhead).
        /// </summary>
        protected override void BuildStateMachine()
        {
            // Populate base states: IdleState, ChaseState, EngageState, ReturnState
            base.BuildStateMachine();

            // Create shooter-specific states
            _shootState = new ShootState(this);
            _retreatState = new RetreatState(this);

            // Re-initialize the state machine from Idle (overwrite base's FSM)
            // ChaseState already handles ShooterBrain via polymorphic check
            _stateMachine = new StateMachine { DebugName = "ShooterOuter" };
            _stateMachine.Initialize(IdleState);
        }

#if UNITY_EDITOR
        protected override void OnGUI()
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                string stateName = _stateMachine.CurrentState.GetType().Name;

                GUI.color = Color.cyan;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 200, 25),
                          $"[{stateName}]");
            }
        }
#endif
    }
}
