using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Stalker strike state: rapid reveal + backstab attack.
    /// Phases (all very fast):
    ///   1. Reveal: fade alpha from stealth to 1.0 over a brief duration
    ///   2. Attack: single melee hit using HitboxResolver (or legacy fallback)
    ///   3. Commit: brief freeze, then transition to DisengageState
    /// Uses AttackDataSO if available, falls back to legacy EnemyStatsSO fields.
    /// </summary>
    public class StalkerStrikeState : IState
    {
        private readonly StalkerBrain _brain;

        private enum Phase { Reveal, Attack, Commit }
        private Phase _currentPhase;
        private float _timer;
        private float _currentAlpha;
        private bool _hasHit;

        // Attack data for this strike
        private AttackDataSO _selectedAttack;

        // Timing constants
        private const float REVEAL_DURATION = 0.2f;
        private const float COMMIT_DURATION = 0.15f;

        // Player layer mask (cached)
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

        // Legacy fallback buffer (NonAlloc)
        private static readonly Collider2D[] _legacyBuffer = new Collider2D[4];

        public StalkerStrikeState(StalkerBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _currentPhase = Phase.Reveal;
            _timer = REVEAL_DURATION;
            _currentAlpha = _brain.StealthAlpha;
            _hasHit = false;

            // Select attack pattern
            _selectedAttack = _brain.Stats.SelectRandomAttack();

            // Stop movement during strike
            _brain.Entity.StopMovement();
        }

        public void OnUpdate(float deltaTime)
        {
            switch (_currentPhase)
            {
                case Phase.Reveal:
                    UpdateReveal(deltaTime);
                    break;
                case Phase.Attack:
                    ExecuteAttack();
                    break;
                case Phase.Commit:
                    UpdateCommit(deltaTime);
                    break;
            }
        }

        private void UpdateReveal(float deltaTime)
        {
            // Rapid fade-in
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, _brain.RevealSpeed * deltaTime);
            _brain.SetAlpha(_currentAlpha);

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                _brain.SetAlpha(1f);
                _currentPhase = Phase.Attack;
            }
        }

        private void ExecuteAttack()
        {
            if (!_hasHit)
            {
                _hasHit = true;
                PerformHit();
            }

            // Move to commit phase
            _currentPhase = Phase.Commit;
            _timer = COMMIT_DURATION;
        }

        private void PerformHit()
        {
            Vector2 origin = _brain.Entity.transform.position;
            Vector2 facing = _brain.Entity.FacingDirection;

            if (_selectedAttack != null)
            {
                // Data-driven hitbox via HitboxResolver
                int count = HitboxResolver.Resolve(
                    _selectedAttack, origin, facing, PlayerLayerMask, out Collider2D[] results);

                for (int i = 0; i < count; i++)
                {
                    var damageable = results[i].GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector2 knockDir = ((Vector2)results[i].transform.position - origin).normalized;
                        var payload = new DamagePayload(_selectedAttack.Damage, DamageType.Physical,
                                                        knockDir, _selectedAttack.Knockback,
                                                        _brain.Entity.gameObject);
                        damageable.TakeDamage(payload);
                    }
                }
            }
            else
            {
                // Legacy fallback: circle overlap with NonAlloc
                float damage = _brain.Stats.AttackDamage;
                float knockback = _brain.Stats.AttackKnockback;
                float range = _brain.Stats.AttackRange;

                int count = Physics2D.OverlapCircleNonAlloc(origin, range, _legacyBuffer, PlayerLayerMask);

                for (int i = 0; i < count; i++)
                {
                    var damageable = _legacyBuffer[i].GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector2 knockDir = ((Vector2)_legacyBuffer[i].transform.position - origin).normalized;
                        var payload = new DamagePayload(damage, DamageType.Physical,
                                                        knockDir, knockback,
                                                        _brain.Entity.gameObject);
                        damageable.TakeDamage(payload);
                    }
                }
            }
        }

        private void UpdateCommit(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                // Attack done → Disengage
                _brain.StateMachine.TransitionTo(_brain.DisengageState);
            }
        }

        public void OnExit()
        {
            // Ensure fully visible if interrupted mid-reveal
            _brain.SetAlpha(1f);
        }
    }
}
