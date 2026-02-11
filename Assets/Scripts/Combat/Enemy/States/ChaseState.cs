using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Chase state: enemy moves directly toward the last known player position.
    /// When reaching attack range, requests an attack token from <see cref="EnemyDirector"/>.
    /// If the token is denied, transitions to <see cref="OrbitState"/> instead of attacking.
    /// Transitions:
    ///   - Rusher: Distance less than AttackRange AND token granted -> EngageState
    ///   - Rusher: Distance less than AttackRange AND token denied  -> OrbitState
    ///   - Shooter: Distance less than PreferredRange AND token granted -> ShootState
    ///   - Shooter: Distance less than PreferredRange AND token denied  -> OrbitState
    ///   - Distance greater than LeashRange OR HasTarget=false -> ReturnState
    /// </summary>
    public class ChaseState : IState
    {
        private readonly EnemyBrain _brain;

        // Boids separation weight: higher value = stronger spreading during chase
        private const float SEPARATION_WEIGHT = 0.6f;

        public ChaseState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter() { }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;
            var entity = _brain.Entity;
            var stats = _brain.Stats;

            // If target is lost (memory decayed) or out of leash range → return
            if (!perception.HasTarget || perception.DistanceToTarget > stats.LeashRange)
            {
                _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // Shooter-type: transition to ShootState when within PreferredRange
            if (_brain is ShooterBrain shooterBrain)
            {
                if (perception.DistanceToTarget < stats.PreferredRange)
                {
                    if (TryRequestToken())
                    {
                        _brain.StateMachine.TransitionTo(shooterBrain.ShootState);
                    }
                    else
                    {
                        _brain.StateMachine.TransitionTo(_brain.OrbitState);
                    }
                    return;
                }
            }
            else
            {
                // Rusher-type: close enough to melee attack → engage (if token available)
                if (perception.DistanceToTarget < stats.AttackRange)
                {
                    if (TryRequestToken())
                    {
                        _brain.StateMachine.TransitionTo(_brain.EngageState);
                    }
                    else
                    {
                        _brain.StateMachine.TransitionTo(_brain.OrbitState);
                    }
                    return;
                }
            }

            // Move toward last known player position with Boids separation
            Vector2 myPos = entity.transform.position;
            Vector2 chaseDir = (perception.LastKnownTargetPosition - myPos).normalized;

            // Blend chase direction with separation force to avoid stacking
            Vector2 separation = entity.GetSeparationForce();
            Vector2 finalDir = (chaseDir + separation * SEPARATION_WEIGHT).normalized;
            entity.MoveTo(finalDir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }

        // ──────────────────── Director Token ────────────────────

        /// <summary>
        /// Try to request an attack token from the Director.
        /// Returns true if no Director exists (backward compatible) or token is granted.
        /// </summary>
        private bool TryRequestToken()
        {
            if (EnemyDirector.Instance == null) return true;
            return EnemyDirector.Instance.RequestToken(_brain);
        }
    }
}
