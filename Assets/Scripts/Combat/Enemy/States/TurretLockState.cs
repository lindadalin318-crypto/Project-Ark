using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Turret lock-on state: tracks the player and acquires a firing lock.
    /// Shows a thin aim-line indicator (via EnemyLaserBeam or LineRenderer).
    /// After LockOnDuration, transitions to TurretAttackState.
    /// Transitions:
    ///   - Lock complete -> TurretAttackState
    ///   - Target lost -> TurretScanState
    /// </summary>
    public class TurretLockState : IState
    {
        private readonly TurretBrain _brain;
        private float _lockTimer;

        // Aim line visual
        private EnemyLaserBeam _aimLineBeam;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;

        public TurretLockState(TurretBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _lockTimer = _brain.LockOnDuration;

            // Select which attack to use for this cycle
            _brain.SelectAttackForCycle();

            // Visual feedback: slight tint to indicate locking
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _spriteRenderer.color = new Color(1f, 0.8f, 0.2f, 1f); // Yellow-orange = locking
            }

            // Try to get an aim-line component for visual telegraph
            _aimLineBeam = _brain.GetComponentInChildren<EnemyLaserBeam>();
        }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;

            // Target lost → back to scan
            if (!perception.HasTarget)
            {
                _brain.StateMachine.TransitionTo(_brain.ScanState);
                return;
            }

            // Track the player: rotate toward them
            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 targetPos = perception.LastKnownTargetPosition;
            Vector2 toPlayer = (targetPos - myPos).normalized;

            // Smoothly rotate facing direction at configured speed
            _brain.Entity.MoveTo(toPlayer);
            _brain.Entity.StopMovement();

            // Update aim line visual
            float range = _brain.SelectedAttack != null
                ? _brain.SelectedAttack.LaserRange
                : _brain.Stats.SightRange;

            if (_aimLineBeam != null)
            {
                _aimLineBeam.ShowAimLine(myPos, _brain.Entity.FacingDirection, range);
            }

            // Count down lock-on timer
            _lockTimer -= deltaTime;
            if (_lockTimer <= 0f)
            {
                _brain.StateMachine.TransitionTo(_brain.TurretAttackState);
            }
        }

        public void OnExit()
        {
            // Restore sprite color
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            // Hide aim line
            if (_aimLineBeam != null)
                _aimLineBeam.HideAimLine();
        }
    }
}
