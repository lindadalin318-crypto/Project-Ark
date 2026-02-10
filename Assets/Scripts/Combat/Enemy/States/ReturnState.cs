using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Return state: enemy moves back to its spawn position.
    /// Upon arrival, transitions to IdleState and restores full HP.
    /// </summary>
    public class ReturnState : IState
    {
        private readonly EnemyBrain _brain;
        private const float ARRIVAL_THRESHOLD = 0.5f;

        public ReturnState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter() { }

        public void OnUpdate(float deltaTime)
        {
            var entity = _brain.Entity;
            Vector2 myPos = entity.transform.position;
            Vector2 spawnPos = _brain.SpawnPosition;

            float dist = Vector2.Distance(myPos, spawnPos);

            if (dist < ARRIVAL_THRESHOLD)
            {
                // Arrived at spawn — restore HP and go idle
                _brain.StateMachine.TransitionTo(_brain.IdleState);
                return;
            }

            // Move toward spawn point
            Vector2 dir = (spawnPos - myPos).normalized;
            entity.MoveTo(dir);

            // If player re-appears during return, switch to chase
            if (_brain.Perception.HasTarget)
            {
                _brain.StateMachine.TransitionTo(_brain.ChaseState);
            }
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();

            // Restore full HP on return to spawn (leash reset)
            // Access via reflection-free approach: entity re-init
            _brain.Entity.OnGetFromPool();
        }
    }
}
