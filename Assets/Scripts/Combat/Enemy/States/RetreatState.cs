using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Retreat state (Shooter type): enemy moves away from the player
    /// to re-establish preferred shooting distance.
    /// Transitions:
    ///   - Distance >= PreferredRange → ShootState (via brain)
    ///   - HasTarget=false OR Distance > LeashRange → ReturnState
    /// </summary>
    public class RetreatState : IState
    {
        private readonly EnemyBrain _brain;

        public RetreatState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter() { }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;
            var entity = _brain.Entity;
            var stats = _brain.Stats;

            // If target lost or out of leash → return to spawn
            if (!perception.HasTarget || perception.DistanceToTarget > stats.LeashRange)
            {
                _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // If we've retreated far enough → transition to shoot
            // ShooterBrain will wire _brain.ShootState; we use the generic accessor
            if (perception.DistanceToTarget >= stats.PreferredRange)
            {
                // ShooterBrain exposes ShootState; we access via the brain's custom property
                var shooterBrain = _brain as ShooterBrain;
                if (shooterBrain != null)
                {
                    _brain.StateMachine.TransitionTo(shooterBrain.ShootState);
                }
                else
                {
                    // Fallback: go back to chase (shouldn't happen for shooter)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                }
                return;
            }

            // Move away from the player
            Vector2 myPos = entity.transform.position;
            Vector2 awayDir = (myPos - perception.LastKnownTargetPosition).normalized;
            entity.MoveTo(awayDir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
