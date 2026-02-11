using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Chase state: enemy moves directly toward the last known player position.
    /// Transitions:
    ///   - Rusher: Distance &lt; AttackRange → EngageState
    ///   - Shooter: Distance &lt; PreferredRange → ShootState
    ///   - Distance &gt; LeashRange OR HasTarget=false → ReturnState
    /// </summary>
    public class ChaseState : IState
    {
        private readonly EnemyBrain _brain;

        // Boids separation weight: 0.3 = subtle push, preserves pursuit intent
        private const float SEPARATION_WEIGHT = 0.3f;

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
                    _brain.StateMachine.TransitionTo(shooterBrain.ShootState);
                    return;
                }
            }
            else
            {
                // Rusher-type: close enough to melee attack → engage
                if (perception.DistanceToTarget < stats.AttackRange)
                {
                    _brain.StateMachine.TransitionTo(_brain.EngageState);
                    return;
                }
            }

            // Move toward last known player position with Boids separation
            Vector2 myPos = entity.transform.position;
            Vector2 chaseDir = (perception.LastKnownPlayerPosition - myPos).normalized;

            // Blend chase direction with separation force to avoid stacking
            Vector2 separation = entity.GetSeparationForce();
            Vector2 finalDir = (chaseDir + separation * SEPARATION_WEIGHT).normalized;
            entity.MoveTo(finalDir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
