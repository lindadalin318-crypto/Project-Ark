using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Flee state: enemy runs directly away from the last known player position
    /// at maximum speed. Triggered by the Fear system when fear threshold is crossed.
    /// Exits when:
    ///   - FleeDuration expires (from EnemyStatsSO)
    ///   - Enemy reaches LeashRange from its spawn point
    /// After fleeing, transitions to ReturnState (goes back to spawn).
    /// </summary>
    public class FleeState : IState
    {
        private readonly EnemyBrain _brain;
        private float _timer;

        // Speed multiplier during flee (relative to MoveSpeed)
        private const float FLEE_SPEED_MULTIPLIER = 1.5f;

        public FleeState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.FleeDuration;

            // Return any attack token — fleeing enemies don't attack
            _brain.ReturnDirectorToken();
        }

        public void OnUpdate(float deltaTime)
        {
            var entity = _brain.Entity;
            var perception = _brain.Perception;
            var stats = _brain.Stats;

            _timer -= deltaTime;

            // Exit conditions
            bool timeUp = _timer <= 0f;
            float distToSpawn = Vector2.Distance(
                (Vector2)entity.transform.position, _brain.SpawnPosition);
            bool beyondLeash = distToSpawn > stats.LeashRange;

            if (timeUp || beyondLeash)
            {
                // Fear subsided or ran far enough — return to spawn
                // Reset the fear component
                var fear = _brain.GetComponent<EnemyFear>();
                if (fear != null)
                    fear.IsFleeing = false;

                _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // Run away from the last known player position
            Vector2 myPos = entity.transform.position;
            Vector2 threatPos = perception.HasTarget
                ? perception.LastKnownTargetPosition
                : _brain.SpawnPosition;

            Vector2 fleeDir = (myPos - threatPos).normalized;

            // If flee direction is zero (exactly on top of threat), pick random
            if (fleeDir.sqrMagnitude < 0.01f)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                fleeDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            float fleeSpeed = stats.MoveSpeed * FLEE_SPEED_MULTIPLIER;
            entity.MoveAtSpeed(fleeDir, fleeSpeed);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
