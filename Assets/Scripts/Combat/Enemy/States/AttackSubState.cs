using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Attack sub-state: active hitbox phase.
    /// Enemy generates a damage area using <see cref="HitboxResolver"/>, cannot turn (commitment),
    /// and transitions to RecoverySubState after ActiveDuration expires.
    /// Reads shape, damage, duration from AttackDataSO if available, otherwise legacy EnemyStatsSO.
    /// Uses NonAlloc physics queries — zero GC allocation.
    /// </summary>
    public class AttackSubState : IState
    {
        private readonly EnemyBrain _brain;
        private readonly EngageState _engage;
        private float _timer;
        private bool _hasDealtDamage;

        // Cached player layer mask
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

        // Fallback buffer for legacy path (NonAlloc)
        private static readonly Collider2D[] _legacyBuffer = new Collider2D[8];

        public AttackSubState(EnemyBrain brain, EngageState engage)
        {
            _brain = brain;
            _engage = engage;
        }

        public void OnEnter()
        {
            var attack = _engage.SelectedAttack;

            // Duration: AttackDataSO > legacy EnemyStatsSO
            _timer = attack != null ? attack.ActiveDuration : _brain.Stats.AttackActiveDuration;
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
        /// Detect players within the hitbox and deal damage via IDamageable.
        /// Uses HitboxResolver when AttackDataSO is available, otherwise legacy OverlapCircle.
        /// </summary>
        private void TryHitPlayer()
        {
            var attack = _engage.SelectedAttack;
            Vector2 origin = _brain.Entity.transform.position;
            Vector2 facing = _brain.Entity.FacingDirection;

            int hitCount;
            Collider2D[] hits;

            if (attack != null)
            {
                // Data-driven path: use HitboxResolver with configured shape
                hitCount = HitboxResolver.Resolve(attack, origin, facing, PlayerLayerMask, out hits);
            }
            else
            {
                // Legacy path: circle overlap using EnemyStatsSO flat fields
                var stats = _brain.Stats;
                Vector2 attackOrigin = origin + facing * (stats.AttackRange * 0.5f);
                hitCount = Physics2D.OverlapCircleNonAlloc(
                    attackOrigin, stats.AttackRange, _legacyBuffer, PlayerLayerMask);
                hits = _legacyBuffer;
            }

            // Read damage/knockback from AttackDataSO or legacy stats
            float damage = attack != null ? attack.Damage : _brain.Stats.AttackDamage;
            float knockback = attack != null ? attack.Knockback : _brain.Stats.AttackKnockback;

            for (int i = 0; i < hitCount; i++)
            {
                var damageable = hits[i].GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector2 knockbackDir = ((Vector2)hits[i].transform.position - origin).normalized;
                    var payload = new DamagePayload(damage, DamageType.Physical, knockbackDir, knockback,
                                                    _brain.Entity.gameObject);
                    damageable.TakeDamage(payload);
                    _hasDealtDamage = true;
                }
            }

            // Debug log for early development
            if (!_hasDealtDamage && hitCount > 0)
            {
                Debug.Log($"[AttackSubState] Hit {hitCount} collider(s) on Player layer but no IDamageable found.");
                _hasDealtDamage = true; // Prevent repeated logs
            }
        }
    }
}
