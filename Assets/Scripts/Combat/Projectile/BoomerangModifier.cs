using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Anomaly family boomerang behavior modifier.
    /// Outbound phase: decelerates to zero.
    /// Return phase: reverses direction and accelerates back toward the owner.
    /// Allows one hit per enemy per leg (outbound + return).
    /// </summary>
    public class BoomerangModifier : MonoBehaviour, IProjectileModifier
    {
        [Header("Boomerang Settings")]
        [SerializeField] private float _turnAroundTime = 0.4f;
        [SerializeField] private float _returnDistanceThreshold = 1.0f;

        // Runtime state
        private Vector2 _ownerPosition;
        private Vector2 _initialDirection;
        private float _baseSpeed;
        private float _elapsed;
        private float _totalFlightTime; // half = outbound, half = return
        private bool _isReturning;
        private Projectile _projectile;

        // Hit dedup: separate sets for outbound and return legs
        private readonly HashSet<Collider2D> _outboundHits = new();
        private readonly HashSet<Collider2D> _returnHits = new();

        public void OnProjectileSpawned(Projectile projectile)
        {
            _projectile = projectile;
            _ownerPosition = projectile.transform.position;
            _initialDirection = projectile.Direction;
            _baseSpeed = projectile.Speed;
            _elapsed = 0f;
            _isReturning = false;

            // Total flight time = half outbound + turnAround pause + half return
            // We use Speed * Lifetime as max range; outbound takes half the lifetime
            _totalFlightTime = _turnAroundTime * 2f + 0.5f; // tuned for feel

            // Prevent the projectile from being destroyed on hit
            projectile.ShouldDestroyOnHit = false;

            _outboundHits.Clear();
            _returnHits.Clear();
        }

        public void OnProjectileUpdate(Projectile projectile, float deltaTime)
        {
            _elapsed += deltaTime;
            var rb = projectile.GetComponent<Rigidbody2D>();

            if (!_isReturning)
            {
                // --- Outbound phase: decelerate ---
                float outboundProgress = Mathf.Clamp01(_elapsed / _turnAroundTime);
                float currentSpeed = Mathf.Lerp(_baseSpeed, 0f, outboundProgress);

                if (outboundProgress >= 1f)
                {
                    // Switch to return phase
                    _isReturning = true;
                    _elapsed = 0f;

                    // Update owner position to latest (in case ship moved)
                    // _ownerPosition stays as launch position for predictable arc
                }

                projectile.Direction = _initialDirection;
                rb.linearVelocity = projectile.Direction * currentSpeed;
            }
            else
            {
                // --- Return phase: accelerate back toward owner ---
                float returnProgress = Mathf.Clamp01(_elapsed / _turnAroundTime);
                float currentSpeed = Mathf.Lerp(0f, _baseSpeed * 1.3f, returnProgress);

                Vector2 toOwner = (_ownerPosition - (Vector2)projectile.transform.position).normalized;
                projectile.Direction = toOwner;
                rb.linearVelocity = toOwner * currentSpeed;

                // Check if returned close enough to owner
                float distToOwner = Vector2.Distance(projectile.transform.position, _ownerPosition);
                if (distToOwner < _returnDistanceThreshold && returnProgress > 0.3f)
                {
                    // Force return to pool
                    projectile.ForceReturnToPool();
                }
            }
        }

        public void OnProjectileHit(Projectile projectile, Collider2D other)
        {
            if (!_isReturning)
            {
                // Outbound leg: allow one hit per enemy
                if (_outboundHits.Contains(other)) return;
                _outboundHits.Add(other);
            }
            else
            {
                // Return leg: allow one hit per enemy (even if hit during outbound)
                if (_returnHits.Contains(other)) return;
                _returnHits.Add(other);
            }

            // TODO: Apply damage via IDamageable when enemy system is ready
            Debug.Log($"[Boomerang] Hit {other.name} during {(_isReturning ? "return" : "outbound")} phase. Damage={projectile.Damage:F1}");
        }

        /// <summary>
        /// Reset state when the modifier is returned to pool.
        /// Called by the pool system or manually.
        /// </summary>
        public void ResetState()
        {
            _elapsed = 0f;
            _isReturning = false;
            _projectile = null;
            _outboundHits.Clear();
            _returnHits.Clear();
        }
    }
}
