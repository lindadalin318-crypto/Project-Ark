using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Shoot state (Shooter type): enemy stands still and fires a burst of projectiles
    /// at the player. Uses the Signal-Window model:
    ///   Telegraph (color flash) → Burst Fire → Recovery (punish window)
    /// Transitions:
    ///   - Player too close (< RetreatRange) → RetreatState
    ///   - Target lost or out of leash → ReturnState
    ///   - Burst complete + recovery done → ChaseState (re-evaluate position)
    /// </summary>
    public class ShootState : IState
    {
        private readonly EnemyBrain _brain;

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
            _phase = Phase.Telegraph;
            _phaseTimer = _brain.Stats.TelegraphDuration;
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
                _spriteRenderer.color = new Color(1f, 0.6f, 0f, 1f); // Orange flash for shooter
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
            // Restore sprite color
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
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
            var stats = _brain.Stats;

            _burstTimer -= deltaTime;

            if (_burstTimer <= 0f && _shotsFired < stats.ShotsPerBurst)
            {
                FireProjectile();
                _shotsFired++;
                _burstTimer = stats.BurstInterval;
            }

            // Burst complete → enter recovery
            if (_shotsFired >= stats.ShotsPerBurst)
            {
                _phase = Phase.Recovery;
                _phaseTimer = stats.RecoveryDuration;

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
            var stats = _brain.Stats;
            var perception = _brain.Perception;

            if (_projectilePool == null)
            {
                Debug.LogWarning("[ShootState] No projectile pool available. Skipping shot.");
                return;
            }

            // Calculate direction toward player's current/last known position
            Vector2 myPos = _brain.Entity.transform.position;
            Vector2 targetPos = perception.LastKnownPlayerPosition;
            Vector2 dir = (targetPos - myPos).normalized;

            // Spawn projectile from pool
            Vector3 spawnPos = myPos + dir * 0.6f; // Offset slightly forward
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            var go = _projectilePool.Get(spawnPos, rotation);
            var proj = go.GetComponent<EnemyProjectile>();

            if (proj != null)
            {
                proj.Initialize(dir, stats.ProjectileSpeed, stats.ProjectileDamage,
                                stats.ProjectileKnockback, stats.ProjectileLifetime);
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
            Vector2 dir = (perception.LastKnownPlayerPosition - myPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                // Use MoveTo + StopMovement to set FacingDirection without actual movement
                _brain.Entity.MoveTo(dir);
                _brain.Entity.StopMovement();
            }
        }

        private void EnsureProjectilePool()
        {
            var stats = _brain.Stats;

            if (stats.ProjectilePrefab == null)
            {
                Debug.LogError($"[ShootState] {_brain.Entity.gameObject.name} has no ProjectilePrefab assigned in EnemyStatsSO!");
                return;
            }

            if (PoolManager.Instance != null)
            {
                _projectilePool = PoolManager.Instance.GetPool(stats.ProjectilePrefab, 10, 50);
            }
            else
            {
                Debug.LogWarning("[ShootState] PoolManager.Instance is null. Enemy projectiles cannot be pooled.");
            }
        }
    }
}
