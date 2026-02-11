using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Stealth state: Stalker is semi-transparent, slowly drifting near its spawn.
    /// Waits for perception to detect a target, then transitions to FlankState.
    /// When no target is present, drifts back toward spawn point.
    /// </summary>
    public class StealthState : IState
    {
        private readonly StalkerBrain _brain;

        public StealthState(StalkerBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            // Apply stealth alpha
            _brain.SetAlpha(_brain.StealthAlpha);
        }

        public void OnUpdate(float deltaTime)
        {
            // Maintain stealth alpha
            _brain.SetAlpha(_brain.StealthAlpha);

            if (_brain.Perception.HasTarget)
            {
                // Transition to Flank — we have a target to stalk
                _brain.StateMachine.TransitionTo(_brain.FlankState);
                return;
            }

            // No target: drift toward spawn position (idle behavior)
            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 spawnPos = _brain.SpawnPosition;
            float distToSpawn = Vector2.Distance(myPos, spawnPos);

            if (distToSpawn > 1f)
            {
                Vector2 dir = (spawnPos - myPos).normalized;
                _brain.Entity.MoveTo(dir);
            }
            else
            {
                _brain.Entity.StopMovement();
            }
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
