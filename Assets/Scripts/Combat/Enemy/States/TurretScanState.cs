using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Turret scan state: idle rotation, scanning for targets.
    /// Slowly sweeps facing direction back and forth (or continuously rotates).
    /// Transitions:
    ///   - HasTarget -> TurretLockState
    /// </summary>
    public class TurretScanState : IState
    {
        private readonly TurretBrain _brain;
        private float _scanAngle;
        private int _scanDirection; // +1 or -1

        public TurretScanState(TurretBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _brain.Entity.StopMovement();

            // Initialize scan from current facing direction
            Vector2 facing = _brain.Entity.FacingDirection;
            _scanAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            _scanDirection = 1;
        }

        public void OnUpdate(float deltaTime)
        {
            // Check if target is detected
            if (_brain.Perception.HasTarget)
            {
                _brain.StateMachine.TransitionTo(_brain.LockState);
                return;
            }

            // Sweep rotation
            _scanAngle += _scanDirection * _brain.ScanRotationSpeed * deltaTime;

            // Reverse direction at sweep limits (±90° from start)
            // Simple ping-pong sweep
            if (_scanAngle > 360f) _scanAngle -= 360f;
            if (_scanAngle < 0f) _scanAngle += 360f;

            // Update facing direction
            float rad = _scanAngle * Mathf.Deg2Rad;
            Vector2 newFacing = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // Use MoveTo to set FacingDirection, then stop
            _brain.Entity.MoveTo(newFacing);
            _brain.Entity.StopMovement();
        }

        public void OnExit() { }
    }
}
