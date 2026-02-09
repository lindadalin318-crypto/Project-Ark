using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Rheology Prism modifier: bounces the projectile off walls up to <see cref="MaxBounces"/> times.
    /// Attach to a Prefab referenced by <see cref="PrismSO.ProjectileModifierPrefab"/>.
    /// </summary>
    public class BounceModifier : MonoBehaviour, IProjectileModifier
    {
        [Header("Bounce Settings")]
        [SerializeField] private int _maxBounces = 3;
        [SerializeField] private LayerMask _wallLayer;

        /// <summary> Maximum number of wall bounces before the projectile is destroyed normally. </summary>
        public int MaxBounces
        {
            get => _maxBounces;
            set => _maxBounces = value;
        }

        private int _remainingBounces;

        // --- IProjectileModifier ---

        public void OnProjectileSpawned(Projectile projectile)
        {
            _remainingBounces = _maxBounces;
        }

        public void OnProjectileUpdate(Projectile projectile, float deltaTime)
        {
            // No per-frame logic needed for bounce
        }

        public void OnProjectileHit(Projectile projectile, Collider2D other)
        {
            // Only bounce off walls
            if (((1 << other.gameObject.layer) & _wallLayer) == 0)
                return;

            if (_remainingBounces <= 0)
            {
                // Out of bounces — allow normal destroy
                return;
            }

            _remainingBounces--;

            // Calculate reflection direction using the wall's contact normal
            Vector2 incomingDir = projectile.Direction;
            Vector2 contactNormal = GetContactNormal(projectile.transform.position, other);
            Vector2 reflectedDir = Vector2.Reflect(incomingDir, contactNormal).normalized;

            // Update projectile direction and velocity
            projectile.Direction = reflectedDir;
            projectile.Speed = projectile.Speed; // preserve current speed

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = reflectedDir * projectile.Speed;

            // Prevent pool return while bounces remain
            if (_remainingBounces > 0)
                projectile.ShouldDestroyOnHit = false;
            else
                projectile.ShouldDestroyOnHit = true;

            Debug.Log($"[Bounce] Reflected off {other.name}, remaining bounces: {_remainingBounces}");
        }

        /// <summary>
        /// Estimate the contact normal by casting a short ray from the projectile
        /// toward the wall collider. Falls back to inverting the projectile direction.
        /// </summary>
        private Vector2 GetContactNormal(Vector2 origin, Collider2D wallCollider)
        {
            // Cast a short ray in the projectile's travel direction to find the hit normal
            Vector2 closestPoint = wallCollider.ClosestPoint(origin);
            Vector2 toWall = (closestPoint - origin);
            float dist = toWall.magnitude;

            if (dist > 0.01f)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, toWall.normalized, dist + 0.5f, _wallLayer);
                if (hit.collider != null)
                    return hit.normal;
            }

            // Fallback: approximate normal as direction from wall to projectile
            Vector2 fallbackNormal = (origin - closestPoint).normalized;
            return fallbackNormal != Vector2.zero ? fallbackNormal : Vector2.up;
        }
    }
}
