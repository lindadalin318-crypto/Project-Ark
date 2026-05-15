using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Minishoot-style charge attack state.
    /// Flow: pause/telegraph shake -> lock direction -> committed dash -> recovery.
    /// </summary>
    public class ChargeState : IState
    {
        private enum Phase
        {
            Telegraph,
            Dashing,
            Recovery
        }

        private readonly ChargeRusherBrain _brain;

        private Phase _phase;
        private AttackDataSO _attack;
        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private Vector3 _baseSpriteLocalPosition;
        private Vector2 _lockedDirection;
        private float _timer;
        private float _hitTimer;
        private bool _hasHit;

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

        private static int _blockLayerMask = -1;
        private static int BlockLayerMask
        {
            get
            {
                if (_blockLayerMask < 0)
                    _blockLayerMask = LayerMask.GetMask("Block", "Wall", "Obstacle");
                return _blockLayerMask;
            }
        }

        private static readonly Collider2D[] _playerBuffer = new Collider2D[8];
        private static readonly Collider2D[] _blockBuffer = new Collider2D[4];
        private static ContactFilter2D _playerFilter = new ContactFilter2D();
        private static ContactFilter2D _blockFilter = new ContactFilter2D();

        public ChargeState(ChargeRusherBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _attack = SelectChargeAttack();
            _spriteRenderer = _brain.GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            _baseSpriteLocalPosition = _spriteRenderer != null ? _spriteRenderer.transform.localPosition : Vector3.zero;
            _lockedDirection = GetAimDirection();
            _timer = GetTelegraphDuration();
            _hitTimer = 0f;
            _hasHit = false;
            _phase = Phase.Telegraph;

            _brain.Entity.StopMovement();
            TintTelegraph();
        }

        public void OnUpdate(float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Telegraph:
                    UpdateTelegraph(deltaTime);
                    break;
                case Phase.Dashing:
                    UpdateDash(deltaTime);
                    break;
                case Phase.Recovery:
                    UpdateRecovery(deltaTime);
                    break;
            }
        }

        public void OnExit()
        {
            _brain.Entity.StopMovement();
            RestoreColor();
            _brain.ReturnDirectorToken();
            _attack = null;
            _spriteRenderer = null;
            _hasHit = false;
            _hitTimer = 0f;
        }

        private AttackDataSO SelectChargeAttack()
        {
            if (_brain.Stats.HasAttackData)
            {
                for (int i = 0; i < _brain.Stats.Attacks.Length; i++)
                {
                    var attack = _brain.Stats.Attacks[i];
                    if (attack != null && attack.Type == AttackType.Charge)
                        return attack;
                }
            }

            return null;
        }

        private void UpdateTelegraph(float deltaTime)
        {
            _lockedDirection = GetAimDirection();
            ApplyTelegraphShake();

            _timer -= deltaTime;
            if (_timer > 0f) return;

            _phase = Phase.Dashing;
            _timer = GetChargeMaxDuration();
            _hitTimer = 0f;
            _hasHit = false;
            RestoreColor();
            _brain.Entity.MoveAtSpeed(_lockedDirection, GetChargeSpeed());
        }

        private void UpdateDash(float deltaTime)
        {
            _brain.Entity.MoveAtSpeed(_lockedDirection, GetChargeSpeed());
            TryHitPlayer(deltaTime);

            _timer -= deltaTime;
            if (_timer <= 0f || HasHitBlocker())
            {
                EnterRecovery();
            }
        }

        private void UpdateRecovery(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;

            if (_brain.Perception.HasTarget && _brain.Perception.DistanceToTarget < _brain.Stats.LeashRange)
                _brain.StateMachine.TransitionTo(_brain.ChaseState);
            else
                _brain.StateMachine.TransitionTo(_brain.ReturnState);
        }

        private void EnterRecovery()
        {
            _phase = Phase.Recovery;
            _timer = GetRecoveryDuration();
            _brain.Entity.StopMovement();
        }

        private Vector2 GetAimDirection()
        {
            Vector2 origin = _brain.transform.position;
            Vector2 target = _brain.Perception.LastKnownTargetPosition;

            if (_brain.Perception.PlayerTransform != null)
            {
                target = _brain.Perception.PlayerTransform.position;
                target += EstimateTargetVelocity(_brain.Perception.PlayerTransform) * GetChargeAnticipation();
            }
            else if (_brain.Perception.CurrentTargetEntity != null)
            {
                target = _brain.Perception.CurrentTargetEntity.transform.position;
                target += EstimateTargetVelocity(_brain.Perception.CurrentTargetEntity.transform) * GetChargeAnticipation();
            }

            Vector2 direction = target - origin;
            if (direction.sqrMagnitude < 0.001f)
                direction = _brain.Entity.FacingDirection;

            return direction.normalized;
        }

        private static Vector2 EstimateTargetVelocity(Transform target)
        {
            if (target == null) return Vector2.zero;

            var body = target.GetComponent<Rigidbody2D>();
            return body != null ? body.linearVelocity : Vector2.zero;
        }

        private void TryHitPlayer(float deltaTime)
        {
            float interval = _attack != null ? _attack.ChargeHitInterval : 0f;
            if (_hasHit && interval <= 0f) return;

            if (interval > 0f)
            {
                _hitTimer -= deltaTime;
                if (_hitTimer > 0f) return;
            }

            int count;
            Collider2D[] hits;

            if (_attack != null)
            {
                count = HitboxResolver.Resolve(_attack, _brain.transform.position, _lockedDirection,
                                               PlayerLayerMask, out hits);
            }
            else
            {
                _playerFilter.SetLayerMask(PlayerLayerMask);
                count = Physics2D.OverlapCircle(_brain.transform.position,
                                                _brain.Stats.AttackRange,
                                                _playerFilter,
                                                _playerBuffer);
                hits = _playerBuffer;
            }

            for (int i = 0; i < count; i++)
            {
                var damageable = hits[i].GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                float damage = _attack != null ? _attack.Damage : _brain.Stats.AttackDamage;
                float knockback = _attack != null ? _attack.Knockback : _brain.Stats.AttackKnockback;
                Vector2 knockbackDir = ((Vector2)hits[i].transform.position - (Vector2)_brain.transform.position).normalized;
                if (knockbackDir.sqrMagnitude < 0.001f)
                    knockbackDir = _lockedDirection;

                var payload = new DamagePayload(damage, DamageType.Physical, knockbackDir, knockback,
                                                _brain.Entity.gameObject);
                damageable.TakeDamage(payload);
                _hasHit = true;

                if (interval > 0f)
                    _hitTimer = interval;

                break;
            }
        }

        private bool HasHitBlocker()
        {
            _blockFilter.SetLayerMask(BlockLayerMask);
            int count = Physics2D.OverlapCircle(_brain.transform.position,
                                                GetBlockProbeRadius(),
                                                _blockFilter,
                                                _blockBuffer);
            return count > 0;
        }

        private void ApplyTelegraphShake()
        {
            if (_spriteRenderer == null) return;

            float shake = Mathf.Sin(Time.time * 80f) * 0.035f;
            Transform spriteTransform = _spriteRenderer.transform;
            spriteTransform.localPosition = _baseSpriteLocalPosition + new Vector3(shake, 0f, 0f);
        }

        private void TintTelegraph()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _attack != null ? _attack.TelegraphColor : Color.red;
        }

        private void RestoreColor()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _baseColor;
            _spriteRenderer.transform.localPosition = _baseSpriteLocalPosition;
        }

        private float GetTelegraphDuration()
        {
            return _attack != null ? _attack.TelegraphDuration : _brain.Stats.TelegraphDuration;
        }

        private float GetChargeMaxDuration()
        {
            return _attack != null ? _attack.ChargeMaxDuration : _brain.Stats.AttackActiveDuration;
        }

        private float GetRecoveryDuration()
        {
            return _attack != null ? _attack.RecoveryDuration : _brain.Stats.RecoveryDuration;
        }

        private float GetChargeSpeed()
        {
            float speed = _attack != null ? _attack.ChargeSpeed : _brain.Stats.MoveSpeed * 3f;
            return speed * _brain.Entity.RuntimeSpeedMultiplier;
        }

        private float GetChargeAnticipation()
        {
            return _attack != null ? _attack.ChargeAnticipation : 0f;
        }

        private float GetBlockProbeRadius()
        {
            return _attack != null ? Mathf.Max(0.1f, _attack.HitboxRadius * 0.5f) : 0.35f;
        }
    }
}
