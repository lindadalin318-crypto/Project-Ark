using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Chase state: enemy moves directly toward the last known player position.
    /// Transitions:
    ///   - Distance &lt; AttackRange → EngageState
    ///   - Distance &gt; LeashRange OR HasTarget=false → ReturnState
    /// </summary>
    public class ChaseState : IState
    {
        private readonly EnemyBrain _brain;

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

            // If close enough to attack → engage
            if (perception.DistanceToTarget < stats.AttackRange)
            {
                _brain.StateMachine.TransitionTo(_brain.EngageState);
                return;
            }

            // Move toward last known player position
            Vector2 myPos = entity.transform.position;
            Vector2 dir = (perception.LastKnownPlayerPosition - myPos).normalized;
            entity.MoveTo(dir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
