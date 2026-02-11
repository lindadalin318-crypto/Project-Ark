using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Shoot state (Shooter type): enemy stands still and fires a burst of projectiles
    /// at the player. Uses the Signal-Window model:
    ///   Telegraph (color flash) -> Burst Fire -> Recovery (punish window)
    /// Supports data-driven attacks via AttackDataSO (falls back to legacy EnemyStatsSO fields).
    /// Transitions:
    ///   - Player too close (less than RetreatRange) -> RetreatState
    ///   - Target lost or out of leash -> ReturnState
    ///   - Burst complete + recovery done -> ChaseState (re-evaluate position)
    /// </summary>
    public class ShootState : IState
    {
        private readonly EnemyBrain _brain;

        // Selected attack for this cycle (null = legacy path)
        private AttackDataSO _selectedAttack;

        // Phase management
        private enum Phase { Telegraph, Firing, Recovery }
        private Phase _phase;
        private float _phaseTimer;

        // Burst tracking
        private int _shotsFired;
        private float _burstTimer;

        // Visual feedback
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;

        // Cached pool for enemy projectiles
        private GameObjectPool _projectilePool;

        public ShootState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            // Select a Projectile-type attack if available
            _selectedAttack = SelectProjectileAttack();

            _phase = Phase.Telegraph;
            _phaseTimer = _selectedAttack != null
                ? _selectedAttack.TelegraphDuration
                : _brain.Stats.TelegraphDuration;
            _shotsFired = 0;
            _burstTimer = 0f;

            // Stop movement — committing to shoot
            _brain.Entity.StopMovement();

            // Face the player
            FacePlayer();

            // Telegraph visual: tint sprite to warn the player
            _spriteRenderer = _brain.Entity.GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _spriteRenderer.color = _selectedAttack != null
                    ? _selectedAttack.TelegraphColor
                    : new Color(1f, 0.6f, 0f, 1f); // Orange flash for shooter
            }

            // Ensure projectile pool is ready
            EnsureProjectilePool();
        }

        public void OnUpdate(float deltaTime)
        {
            var perception = _brain.Perception;
            var stats = _brain.Stats;

            // Global check: target lost or out of leash → return
            if (!perception.HasTarget || perception.DistanceToTarget > stats.LeashRange)
            {
                _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // Global check: player too close → retreat
            if (perception.DistanceToTarget < stats.RetreatRange)
            {
                var shooterBrain = _brain as ShooterBrain;
                if (shooterBrain != null)
                {
                    _brain.StateMachine.TransitionTo(shooterBrain.RetreatState);
                    return;
                }
            }

            switch (_phase)
            {
                case Phase.Telegraph:
                    UpdateTelegraph(deltaTime);
                    break;
                case Phase.Firing:
                    UpdateFiring(deltaTime);
                    break;
                case Phase.Recovery:
                    UpdateRecovery(deltaTime);
                    break;
            }
        }

        public void OnExit()
        {
            // Return attack token to the Director
            _brain.ReturnDirectorToken();

            // Restore sprite color
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            _selectedAttack = null;
        }

        // ──────────────────── Attack Selection ────────────────────

        /// <summary>
        /// Find a Projectile-type AttackDataSO from the enemy's Attacks array.
        /// Returns null if none found (legacy path).
        /// </summary>
        private AttackDataSO SelectProjectileAttack()
        {
            var stats = _brain.Stats;
            if (!stats.HasAttackData) return null;

            // Weighted random among Projectile-type attacks
            float totalWeight = 0f;
            for (int i = 0; i < stats.Attacks.Length; i++)
            {
                if (stats.Attacks[i] != null && stats.Attacks[i].Type == AttackType.Projectile)
                    totalWeight += stats.Attacks[i].SelectionWeight;
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < stats.Attacks.Length; i++)
            {
                if (stats.Attacks[i] == null || stats.Attacks[i].Type != AttackType.Projectile)
                    continue;
                cumulative += stats.Attacks[i].SelectionWeight;
                if (roll <= cumulative) return stats.Attacks[i];
            }

            return null;
        }

        // ──────────────────── Phase Updates ────────────────────

        private void UpdateTelegraph(float deltaTime)
        {
            _phaseTimer -= deltaTime;

            if (_phaseTimer <= 0f)
            {
                // Restore color before firing
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _originalColor;

                _phase = Phase.Firing;
                _shotsFired = 0;
                _burstTimer = 0f; // Fire first shot immediately
            }
        }

        private void UpdateFiring(float deltaTime)
        {
            int shotsPerBurst = _selectedAttack != null
                ? _selectedAttack.ShotsPerBurst
                : _brain.Stats.ShotsPerBurst;
            float burstInterval = _selectedAttack != null
                ? _selectedAttack.BurstInterval
                : _brain.Stats.BurstInterval;

            _burstTimer -= deltaTime;

            if (_burstTimer <= 0f && _shotsFired < shotsPerBurst)
            {
                FireProjectile();
                _shotsFired++;
                _burstTimer = burstInterval;
            }

            // Burst complete → enter recovery
            if (_shotsFired >= shotsPerBurst)
            {
                _phase = Phase.Recovery;
                _phaseTimer = _selectedAttack != null
                    ? _selectedAttack.RecoveryDuration
                    : _brain.Stats.RecoveryDuration;

                // Recovery visual: dim the sprite slightly
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _originalColor * 0.6f;
            }
        }

        private void UpdateRecovery(float deltaTime)
        {
            _phaseTimer -= deltaTime;

            if (_phaseTimer <= 0f)
            {
                // Attack cycle done — re-evaluate position via Chase
                _brain.StateMachine.TransitionTo(_brain.ChaseState);
            }
        }

        // ──────────────────── Shooting ────────────────────

        private void FireProjectile()
        {
            var perception = _brain.Perception;

            if (_projectilePool == null)
            {
                Debug.LogWarning("[ShootState] No projectile pool available. Skipping shot.");
                return;
            }

            // Read values from AttackDataSO or legacy stats
            float projSpeed = _selectedAttack != null
                ? _selectedAttack.ProjectileSpeed
                : _brain.Stats.ProjectileSpeed;
            float projDamage = _selectedAttack != null
                ? _selectedAttack.Damage
                : _brain.Stats.ProjectileDamage;
            float projKnockback = _selectedAttack != null
                ? _selectedAttack.ProjectileKnockback
                : _brain.Stats.ProjectileKnockback;
            float projLifetime = _selectedAttack != null
                ? _selectedAttack.ProjectileLifetime
                : _brain.Stats.ProjectileLifetime;

            // Calculate direction toward player's current/last known position
            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 targetPos = perception.LastKnownTargetPosition;
            Vector2 dir = (targetPos - myPos).normalized;

            // Spawn projectile from pool
            Vector3 spawnPos = myPos + dir * 0.6f; // Offset slightly forward
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            var go = _projectilePool.Get(spawnPos, rotation);
            var proj = go.GetComponent<EnemyProjectile>();

            if (proj != null)
            {
                proj.Initialize(dir, projSpeed, projDamage, projKnockback, projLifetime);
            }

            // Update facing direction
            _brain.Entity.MoveTo(dir); // Just to set FacingDirection
            _brain.Entity.StopMovement(); // But don't actually move
        }

        private void FacePlayer()
        {
            var perception = _brain.Perception;
            if (!perception.HasTarget) return;

            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 dir = (perception.LastKnownTargetPosition - myPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                _brain.Entity.MoveTo(dir);
                _brain.Entity.StopMovement();
            }
        }

        private void EnsureProjectilePool()
        {
            // Prefer AttackDataSO prefab, fall back to legacy stats
            GameObject prefab = _selectedAttack != null
                ? _selectedAttack.ProjectilePrefab
                : _brain.Stats.ProjectilePrefab;

            // If AttackDataSO has no prefab, try legacy as well
            if (prefab == null)
                prefab = _brain.Stats.ProjectilePrefab;

            if (prefab == null)
            {
                Debug.LogError($"[ShootState] {_brain.Entity.gameObject.name} has no ProjectilePrefab assigned!");
                return;
            }

            if (PoolManager.Instance != null)
            {
                _projectilePool = PoolManager.Instance.GetPool(prefab, 10, 50);
            }
            else
            {
                Debug.LogWarning("[ShootState] PoolManager.Instance is null. Enemy projectiles cannot be pooled.");
            }
        }
    }
}
