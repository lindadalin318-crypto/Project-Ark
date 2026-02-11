using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Turret attack state: executes the selected attack from AttackDataSO.
    /// Supports two modes:
    ///   - Laser: fires an EnemyLaserBeam for LaserDuration
    ///   - Projectile: fires a single high-damage charged shot
    /// After the attack completes, transitions to TurretCooldownState.
    /// </summary>
    public class TurretAttackState : IState
    {
        private readonly TurretBrain _brain;
        private float _attackTimer;
        private bool _hasFired;

        // Visual
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;

        // Laser pool
        private GameObjectPool _laserPool;

        // Projectile pool
        private GameObjectPool _projectilePool;

        public TurretAttackState(TurretBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _hasFired = false;

            var attack = _brain.SelectedAttack;
            if (attack == null)
            {
                // No attack configured — skip to cooldown
                Debug.LogWarning("[TurretAttackState] No AttackDataSO configured. Skipping attack.");
                _attackTimer = 0f;
                return;
            }

            // Attack phase timing:
            // Telegraph is handled by LockState, so we go straight to active
            _attackTimer = attack.ActiveDuration;

            // Visual: flash to telegraph color
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _spriteRenderer.color = attack.TelegraphColor;
            }

            // Execute attack immediately on enter
            ExecuteAttack(attack);
        }

        public void OnUpdate(float deltaTime)
        {
            _attackTimer -= deltaTime;

            if (_attackTimer <= 0f)
            {
                _brain.StateMachine.TransitionTo(_brain.CooldownState);
            }
        }

        public void OnExit()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
        }

        // ──────────────────── Attack Execution ────────────────────

        private void ExecuteAttack(AttackDataSO attack)
        {
            switch (attack.Type)
            {
                case AttackType.Laser:
                    FireLaser(attack);
                    break;

                case AttackType.Projectile:
                    FireProjectile(attack);
                    break;

                case AttackType.Melee:
                    // Turrets shouldn't have melee attacks, but handle gracefully
                    Debug.LogWarning("[TurretAttackState] Melee attack assigned to turret. Ignoring.");
                    break;
            }

            _hasFired = true;
        }

        private void FireLaser(AttackDataSO attack)
        {
            // Try to use the built-in EnemyLaserBeam on the turret
            var aimBeam = _brain.GetComponentInChildren<EnemyLaserBeam>();

            if (aimBeam != null)
            {
                // Use the existing beam component (already on the prefab)
                Vector2 origin = _brain.Entity.transform.position;
                Vector2 direction = _brain.Entity.FacingDirection;

                aimBeam.Fire(origin, direction, attack.Damage, attack.Knockback,
                             attack.LaserRange, attack.LaserDuration, attack.LaserWidth);

                // Set attack timer to match laser duration
                _attackTimer = attack.LaserDuration + 0.15f; // +fade time
            }
            else if (attack.LaserPrefab != null && PoolManager.Instance != null)
            {
                // Spawn from pool
                _laserPool = PoolManager.Instance.GetPool(attack.LaserPrefab, 3, 10);

                Vector2 origin = _brain.Entity.transform.position;
                Vector2 direction = _brain.Entity.FacingDirection;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                var go = _laserPool.Get(origin, Quaternion.Euler(0, 0, angle));
                var beam = go.GetComponent<EnemyLaserBeam>();

                if (beam != null)
                {
                    beam.Fire(origin, direction, attack.Damage, attack.Knockback,
                              attack.LaserRange, attack.LaserDuration, attack.LaserWidth);
                }

                _attackTimer = attack.LaserDuration + 0.15f;
            }
            else
            {
                Debug.LogWarning("[TurretAttackState] Laser attack configured but no beam available.");
            }
        }

        private void FireProjectile(AttackDataSO attack)
        {
            if (attack.ProjectilePrefab == null)
            {
                Debug.LogWarning("[TurretAttackState] Projectile attack configured but no prefab assigned.");
                return;
            }

            if (PoolManager.Instance == null)
            {
                Debug.LogWarning("[TurretAttackState] PoolManager.Instance is null.");
                return;
            }

            _projectilePool = PoolManager.Instance.GetPool(attack.ProjectilePrefab, 5, 20);

            Vector2 origin = _brain.Entity.transform.position;
            Vector2 direction = _brain.Entity.FacingDirection;

            Vector3 spawnPos = origin + direction * 0.6f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            var go = _projectilePool.Get(spawnPos, rotation);
            var proj = go.GetComponent<EnemyProjectile>();

            if (proj != null)
            {
                proj.Initialize(direction, attack.ProjectileSpeed, attack.Damage,
                                attack.ProjectileKnockback, attack.ProjectileLifetime);
            }
        }
    }
}
