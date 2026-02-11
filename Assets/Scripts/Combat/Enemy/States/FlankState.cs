using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Flank state: Stalker approaches the player's rear arc while maintaining stealth.
    /// Calculates a target position behind the player using the player's facing direction.
    /// Transitions to StalkerStrikeState when behind player AND within attack range.
    /// Falls back to StealthState if target is lost or leash exceeded.
    /// "Behind player" = Dot(playerFacing, dirFromPlayerToEnemy) less than BEHIND_THRESHOLD.
    /// </summary>
    public class FlankState : IState
    {
        private readonly StalkerBrain _brain;

        // Dot product threshold for "behind" check:
        // playerFacing · dirToEnemy < -0.3 means enemy is in rear arc
        private const float BEHIND_THRESHOLD = -0.3f;

        // How far behind the player to target (world units offset)
        private const float FLANK_OFFSET = 2.5f;

        // Boids separation weight during flanking
        private const float SEPARATION_WEIGHT = 0.4f;

        public FlankState(StalkerBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _brain.SetAlpha(_brain.StealthAlpha);
        }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;
            var entity = _brain.Entity;
            var stats = _brain.Stats;

            // Target lost or out of leash → back to stealth
            if (!perception.HasTarget || perception.DistanceToTarget > stats.LeashRange)
            {
                _brain.StateMachine.TransitionTo(_brain.StealthState);
                return;
            }

            // Maintain stealth alpha
            _brain.SetAlpha(_brain.StealthAlpha);

            Vector2 myPos = entity.transform.position;
            Vector2 playerPos = perception.LastKnownTargetPosition;

            // Get player facing direction (transform.up is the forward for 2D top-down)
            Vector2 playerFacing = Vector2.up;
            if (perception.PlayerTransform != null)
                playerFacing = perception.PlayerTransform.up;

            // Check if we're behind the player
            Vector2 dirToEnemy = (myPos - playerPos).normalized;
            float dot = Vector2.Dot(playerFacing, dirToEnemy);
            bool isBehindPlayer = dot < BEHIND_THRESHOLD;

            // Check if we're within attack range
            float distToPlayer = Vector2.Distance(myPos, playerPos);
            bool inRange = distToPlayer < stats.AttackRange;

            if (isBehindPlayer && inRange)
            {
                // Behind and in range — STRIKE!
                _brain.StateMachine.TransitionTo(_brain.StrikeState);
                return;
            }

            // Calculate target position: behind the player
            Vector2 behindPos = playerPos - playerFacing.normalized * FLANK_OFFSET;

            // Move toward the flanking position
            Vector2 moveDir = (behindPos - myPos).normalized;

            // Blend with separation force
            Vector2 separation = entity.GetSeparationForce();
            Vector2 finalDir = (moveDir + separation * SEPARATION_WEIGHT).normalized;

            entity.MoveTo(finalDir);
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
        }
    }
}
