using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Satellite behavior: Auto Turret.
    /// Every <see cref="InternalCooldown"/> seconds (managed by SatelliteRunner),
    /// detects the nearest enemy within <see cref="_detectionRange"/> and fires
    /// a low-damage Matter-family projectile toward it.
    ///
    /// Lifecycle managed by <see cref="SatelliteRunner"/>:
    ///   Initialize → EvaluateTrigger (every frame when cooldown ready) → Execute → Cleanup
    /// </summary>
    public class AutoTurretBehavior : SatelliteBehavior
    {
        [Header("Detection")]
        [Tooltip("Radius in world units to scan for enemies")]
        [SerializeField] private float _detectionRange = 15f;

        [Tooltip("Layer mask for enemy detection — must be set explicitly")]
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Projectile")]
        [Tooltip("Matter-family projectile prefab to fire (e.g. Projectile_Matter)")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Speed of the fired projectile")]
        [SerializeField] private float _projectileSpeed = 18f;

        [Tooltip("Damage dealt per shot")]
        [SerializeField] private float _projectileDamage = 5f;

        [Tooltip("Lifetime of the fired projectile in seconds")]
        [SerializeField] private float _projectileLifetime = 2f;

        [Tooltip("Knockback applied to hit target")]
        [SerializeField] private float _projectileKnockback = 0.5f;

        // Cached context reference
        private StarChartContext _context;
        private GameObjectPool _pool;

        // --- SatelliteBehavior ---

        public override void Initialize(StarChartContext context)
        {
            _context = context;

            // Pre-warm the projectile pool
            if (_projectilePrefab != null && PoolManager.Instance != null)
                _pool = PoolManager.Instance.GetPool(_projectilePrefab, 10, 30);
        }

        /// <summary>
        /// Returns true when there is at least one enemy in range.
        /// SatelliteRunner only calls this when the internal cooldown is ready.
        /// </summary>
        public override bool EvaluateTrigger(StarChartContext context)
        {
            return FindNearestEnemy(context.ShipTransform.position) != null;
        }

        /// <summary>
        /// Fires one projectile toward the nearest enemy.
        /// </summary>
        public override void Execute(StarChartContext context)
        {
            Transform target = FindNearestEnemy(context.ShipTransform.position);
            if (target == null) return;

            if (_pool == null)
            {
                if (_projectilePrefab == null || PoolManager.Instance == null) return;
                _pool = PoolManager.Instance.GetPool(_projectilePrefab, 10, 30);
            }

            Vector2 origin = context.ShipTransform.position;
            Vector2 direction = ((Vector2)target.position - origin).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            GameObject bulletObj = _pool.Get(origin, Quaternion.Euler(0f, 0f, angle));
            var projectile = bulletObj.GetComponent<Projectile>();
            if (projectile == null) return;

            var parms = new ProjectileParams(
                damage:         _projectileDamage,
                speed:          _projectileSpeed,
                lifetime:       _projectileLifetime,
                knockback:      _projectileKnockback,
                impactVFXPrefab: null,
                damageType:     DamageType.Physical
            );

            projectile.Initialize(direction, parms, modifiers: null);
        }

        public override void Cleanup()
        {
            _pool = null;
        }

        // --- Private helpers ---

        private Transform FindNearestEnemy(Vector2 origin)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _detectionRange, _enemyLayer);
            if (hits.Length == 0) return null;

            Transform nearest = null;
            float nearestSqDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                float sqDist = ((Vector2)hits[i].transform.position - origin).sqrMagnitude;
                if (sqDist < nearestSqDist)
                {
                    nearestSqDist = sqDist;
                    nearest = hits[i].transform;
                }
            }

            return nearest;
        }
    }
}
