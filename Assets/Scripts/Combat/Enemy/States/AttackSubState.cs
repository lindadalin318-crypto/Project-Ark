using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Attack sub-state: active hitbox phase.
    /// Enemy generates a damage area (OverlapCircle), cannot turn (commitment),
    /// and transitions to RecoverySubState after AttackActiveDuration expires.
    /// Uses Physics2D.OverlapCircle + LayerMask to detect the player.
    /// </summary>
    public class AttackSubState : IState
    {
        private readonly EnemyBrain _brain;
        private readonly EngageState _engage;
        private float _timer;
        private bool _hasDealtDamage;

        // Cached player layer mask for OverlapCircle detection
        private static int _playerLayerMask = -1;
        private static int PlayerLayerMask
        {
            get
            {
                if (_playerLayerMask < 0)
                    _playerLayerMask = LayerMask.GetMask("Player");
                return _playerLayerMask;
            }
        }

        public AttackSubState(EnemyBrain brain, EngageState engage)
        {
            _brain = brain;
            _engage = engage;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.AttackActiveDuration;
            _hasDealtDamage = false;

            // Stop movement — full commitment, no turning
            _brain.Entity.StopMovement();
        }

        public void OnUpdate(float deltaTime)
        {
            // Try to hit the player once during the active window
            if (!_hasDealtDamage)
            {
                TryHitPlayer();
            }

            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                _engage.SubStateMachine.TransitionTo(_engage.RecoveryState);
            }
        }

        public void OnExit() { }

        /// <summary>
        /// Perform an OverlapCircle at the enemy's position to detect
        /// player targets within AttackRange, and deal damage via IDamageable.
        /// </summary>
        private void TryHitPlayer()
        {
            var stats = _brain.Stats;
            Vector2 attackOrigin = (Vector2)_brain.Entity.transform.position +
                                   _brain.Entity.FacingDirection * (stats.AttackRange * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackOrigin, stats.AttackRange, PlayerLayerMask);

            for (int i = 0; i < hits.Length; i++)
            {
                var damageable = hits[i].GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector2 knockbackDir = ((Vector2)hits[i].transform.position - (Vector2)_brain.Entity.transform.position).normalized;
                    damageable.TakeDamage(stats.AttackDamage, knockbackDir, stats.AttackKnockback);
                    _hasDealtDamage = true;
                }
            }

            // Even if no IDamageable found, log for debugging during early development
            if (!_hasDealtDamage && hits.Length > 0)
            {
                Debug.Log($"[AttackSubState] Hit {hits.Length} collider(s) on Player layer but no IDamageable found.");
                _hasDealtDamage = true; // Prevent repeated logs
            }
        }
    }
}
