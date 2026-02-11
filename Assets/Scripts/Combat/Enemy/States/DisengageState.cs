using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Disengage state: Stalker dashes away from the player at boosted speed
    /// and fades back to stealth alpha. Transitions to StealthState when
    /// beyond DisengageDistance or when maximum disengage time expires.
    /// </summary>
    public class DisengageState : IState
    {
        private readonly StalkerBrain _brain;
        private float _currentAlpha;

        // Maximum disengage time to prevent infinite fleeing
        private const float MAX_DISENGAGE_TIME = 3f;
        private float _timer;

        public DisengageState(StalkerBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _currentAlpha = 1f;
            _timer = MAX_DISENGAGE_TIME;
        }

        public void OnUpdate(float deltaTime)
        {
            var entity = _brain.Entity;
            var perception = _brain.Perception;

            // Gradually fade back to stealth alpha
            _currentAlpha = Mathf.MoveTowards(
                _currentAlpha, _brain.StealthAlpha, _brain.RevealSpeed * 0.5f * deltaTime);
            _brain.SetAlpha(_currentAlpha);

            // Move away from player at boosted speed
            Vector2 myPos = entity.transform.position;
            Vector2 playerPos = perception.HasTarget
                ? perception.LastKnownTargetPosition
                : _brain.SpawnPosition;
            Vector2 awayDir = (myPos - playerPos).normalized;

            float boostedSpeed = _brain.Stats.MoveSpeed * _brain.DisengageSpeedMultiplier;
            entity.MoveAtSpeed(awayDir, boostedSpeed);

            // Check if we've disengaged far enough
            float dist = Vector2.Distance(myPos, playerPos);
            _timer -= deltaTime;

            if (dist >= _brain.DisengageDistance || _timer <= 0f)
            {
                _brain.StateMachine.TransitionTo(_brain.StealthState);
            }
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
