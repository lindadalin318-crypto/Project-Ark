using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Rheology Prism modifier: steers the projectile toward the nearest enemy
    /// within a 45° forward cone at a configurable turn speed.
    /// Attach to a Prefab referenced by <see cref="PrismSO.ProjectileModifierPrefab"/>.
    /// </summary>
    public class HomingModifier : MonoBehaviour, IProjectileModifier
    {
        [Header("Homing Settings")]
        [Tooltip("Maximum turn rate in degrees per second")]
        [SerializeField] private float _turnSpeed = 180f;

        [Tooltip("Half-angle of the detection cone in degrees (45 = 90° total cone)")]
        [SerializeField] private float _coneHalfAngle = 45f;

        [Tooltip("Maximum detection range in world units")]
        [SerializeField] private float _detectionRange = 20f;

        // Layer mask for enemy detection — must be set explicitly, never ~0
        [SerializeField] private LayerMask _enemyLayer;

        // Runtime state
        private Projectile _projectile;

        // --- IProjectileModifier ---

        public void OnProjectileSpawned(Projectile projectile)
        {
            _projectile = projectile;
        }

        public void OnProjectileUpdate(Projectile projectile, float deltaTime)
        {
            if (projectile == null) return;

            Transform nearest = FindNearestEnemyInCone(projectile.transform.position, projectile.Direction);
            if (nearest == null) return;

            Vector2 toTarget = ((Vector2)nearest.position - (Vector2)projectile.transform.position).normalized;
            Vector2 newDir = Vector2.MoveTowards(
                projectile.Direction,
                toTarget,
                _turnSpeed * Mathf.Deg2Rad * deltaTime
            ).normalized;

            projectile.Direction = newDir;
            projectile.GetComponent<Rigidbody2D>().linearVelocity = newDir * projectile.Speed;

            // Rotate sprite to match direction
            float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg - 90f;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void OnProjectileHit(Projectile projectile, Collider2D other)
        {
            // No special hit logic needed; pool return handled by Projectile
        }

        // --- Private helpers ---

        private Transform FindNearestEnemyInCone(Vector2 origin, Vector2 forward)
        {
            // Use OverlapCircle to get candidates, then filter by cone angle
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _detectionRange, _enemyLayer);
            if (hits.Length == 0) return null;

            Transform nearest = null;
            float nearestSqDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Vector2 toEnemy = ((Vector2)hits[i].transform.position - origin);
                float angle = Vector2.Angle(forward, toEnemy);
                if (angle > _coneHalfAngle) continue;

                float sqDist = toEnemy.sqrMagnitude;
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
