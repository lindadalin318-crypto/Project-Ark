using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Dodge state: quick lateral dash perpendicular to the incoming threat direction.
    /// Triggered when ThreatSensor detects incoming projectile on an enemy with "CanDodge" tag.
    /// Short duration (~0.3s), then returns to the previous state via Chase or Idle.
    /// </summary>
    public class DodgeState : IState
    {
        private readonly EnemyBrain _brain;
        private float _timer;
        private Vector2 _dodgeDirection;

        public DodgeState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.DodgeDuration;

            // Calculate dodge direction: perpendicular to threat direction
            var sensor = _brain.GetComponent<ThreatSensor>();
            Vector2 threatDir = sensor != null ? sensor.ThreatDirection : Vector2.right;

            // Choose left or right perpendicular randomly
            float sign = Random.value > 0.5f ? 1f : -1f;
            _dodgeDirection = new Vector2(-threatDir.y * sign, threatDir.x * sign).normalized;

            // If dodge direction is zero, pick a random direction
            if (_dodgeDirection.sqrMagnitude < 0.01f)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                _dodgeDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        public void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;

            // Dash at dodge speed
            float speed = _brain.Stats.DodgeSpeed;
            _brain.Entity.MoveAtSpeed(_dodgeDirection, speed);

            if (_timer <= 0f)
            {
                // Dodge complete — return to appropriate state
                if (_brain.Perception.HasTarget)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                else
                    _brain.StateMachine.TransitionTo(_brain.IdleState);
            }
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
