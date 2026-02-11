using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Block state: enemy faces the incoming threat and raises a block,
    /// reducing damage by BlockDamageReduction while the threat persists.
    /// Triggered when ThreatSensor detects incoming projectile on an enemy with "CanBlock" tag.
    /// Holds block for BlockDuration or until the threat passes, then returns to combat.
    /// </summary>
    public class BlockState : IState
    {
        private readonly EnemyBrain _brain;
        private float _timer;
        private ThreatSensor _sensor;

        public BlockState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.BlockDuration;
            _sensor = _brain.GetComponent<ThreatSensor>();

            // Activate blocking on entity
            _brain.Entity.IsBlocking = true;
            _brain.Entity.BlockDamageReduction = _brain.Stats.BlockDamageReduction;

            // Stop movement — brace for impact
            _brain.Entity.StopMovement();

            // Face the threat direction
            if (_sensor != null && _sensor.IsThreatDetected)
            {
                _brain.Entity.MoveAtSpeed(_sensor.ThreatDirection, 0f);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;

            // Continue facing the threat
            if (_sensor != null && _sensor.IsThreatDetected)
            {
                // Update facing toward threat source
                Vector2 toThreat = (_sensor.ThreatPosition - (Vector2)_brain.Entity.transform.position).normalized;
                if (toThreat.sqrMagnitude > 0.01f)
                {
                    // Just update facing, don't move
                    _brain.Entity.MoveAtSpeed(toThreat, 0f);
                }
            }

            // Exit conditions: timer up or threat passed
            bool timeUp = _timer <= 0f;
            bool threatPassed = _sensor == null || !_sensor.IsThreatDetected;

            if (timeUp || threatPassed)
            {
                // Block complete — return to appropriate state
                if (_brain.Perception.HasTarget)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                else
                    _brain.StateMachine.TransitionTo(_brain.IdleState);
            }
        }

        public void OnExit()
        {
            // Deactivate blocking
            _brain.Entity.IsBlocking = false;
            _brain.Entity.BlockDamageReduction = 0f;
        }
    }
}
