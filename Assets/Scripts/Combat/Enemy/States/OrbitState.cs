using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Orbit state: enemy circles around the player at a safe distance,
    /// waiting for an attack token from <see cref="EnemyDirector"/>.
    /// Creates "movie-feel" combat — non-attacking enemies appear menacing
    /// but give the player breathing room.
    /// Transitions:
    ///   - Token acquired -> EngageState (Rusher) / ShootState (Shooter)
    ///   - Target lost or out of leash -> ReturnState
    /// </summary>
    public class OrbitState : IState
    {
        private readonly EnemyBrain _brain;

        // Orbit parameters
        private float _orbitAngle;       // Current angle on the orbit circle (radians)
        private int _orbitDirection;     // +1 = counter-clockwise, -1 = clockwise
        private float _tokenCheckTimer;  // Throttled token re-request

        // Token re-request interval (avoid per-frame overhead)
        private const float TOKEN_CHECK_INTERVAL = 0.4f;

        // Boids separation weight during orbit (higher than chase for tighter spacing)
        private const float SEPARATION_WEIGHT = 0.5f;

        public OrbitState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            // Pick random orbit direction
            _orbitDirection = Random.value > 0.5f ? 1 : -1;

            // Calculate initial angle from player
            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 playerPos = _brain.Perception.LastKnownTargetPosition;
            Vector2 toEnemy = myPos - playerPos;
            _orbitAngle = Mathf.Atan2(toEnemy.y, toEnemy.x);

            _tokenCheckTimer = TOKEN_CHECK_INTERVAL;
        }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;
            var stats = _brain.Stats;
            var entity = _brain.Entity;

            // ── Check target validity ──
            if (!perception.HasTarget || perception.DistanceToTarget > stats.LeashRange)
            {
                ReturnTokenIfHeld();
                _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // ── Periodically try to acquire attack token ──
            _tokenCheckTimer -= deltaTime;
            if (_tokenCheckTimer <= 0f)
            {
                _tokenCheckTimer = TOKEN_CHECK_INTERVAL;

                if (TryAcquireToken())
                {
                    // Token granted — transition to attack
                    TransitionToAttack();
                    return;
                }
            }

            // ── Orbit movement ──
            Vector2 playerPos = perception.LastKnownTargetPosition;
            float orbitRadius = GetOrbitRadius();

            // Advance angle
            float angularSpeed = GetOrbitAngularSpeed();
            _orbitAngle += _orbitDirection * angularSpeed * Mathf.Deg2Rad * deltaTime;

            // Calculate target position on orbit circle
            Vector2 targetPos = playerPos + new Vector2(
                Mathf.Cos(_orbitAngle) * orbitRadius,
                Mathf.Sin(_orbitAngle) * orbitRadius);

            // Move toward the orbit target point
            Vector2 myPos = entity.transform.position;
            Vector2 moveDir = (targetPos - myPos).normalized;

            // Blend with Boids separation
            Vector2 separation = entity.GetSeparationForce();
            Vector2 finalDir = (moveDir + separation * SEPARATION_WEIGHT).normalized;

            entity.MoveTo(finalDir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }

        // ──────────────────── Helpers ────────────────────

        private float GetOrbitRadius()
        {
            float baseRange = _brain.Stats.AttackRange;
            float multiplier = EnemyDirector.Instance != null
                ? EnemyDirector.Instance.OrbitRadiusMultiplier
                : 1.5f;
            return baseRange * multiplier;
        }

        private float GetOrbitAngularSpeed()
        {
            return EnemyDirector.Instance != null
                ? EnemyDirector.Instance.OrbitSpeed
                : 90f;
        }

        private bool TryAcquireToken()
        {
            if (EnemyDirector.Instance == null) return true; // No director = free attack
            return EnemyDirector.Instance.RequestToken(_brain);
        }

        private void ReturnTokenIfHeld()
        {
            if (EnemyDirector.Instance != null)
                EnemyDirector.Instance.ReturnToken(_brain);
        }

        private void TransitionToAttack()
        {
            // Shooter-type uses ShootState, others use EngageState
            if (_brain is ShooterBrain shooterBrain)
            {
                _brain.StateMachine.TransitionTo(shooterBrain.ShootState);
            }
            else
            {
                _brain.StateMachine.TransitionTo(_brain.EngageState);
            }
        }
    }
}
