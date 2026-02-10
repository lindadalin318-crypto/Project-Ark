using System;
using System.Collections;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Enemy body layer (躯壳层). Handles HP, movement execution, damage reception,
    /// hit feedback, death flow, and object pool lifecycle.
    /// The Brain layer issues high-level commands; this layer executes the physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyEntity : MonoBehaviour, IDamageable, IPoolable
    {
        // ──────────────────── Inspector ────────────────────
        [Header("Data")]
        [SerializeField] private EnemyStatsSO _stats;

        // ──────────────────── Runtime State ────────────────────
        private float _currentHP;
        private float _currentPoise;
        private bool _isDead;

        // ──────────────────── Cached Components ────────────────────
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private PoolReference _poolRef;

        // ──────────────────── Events ────────────────────
        /// <summary> Fired when this enemy takes damage. (damage, currentHP) </summary>
        public event Action<float, float> OnDamageTaken;

        /// <summary> Fired when this enemy dies. </summary>
        public event Action OnDeath;

        // ──────────────────── Public Properties ────────────────────
        /// <summary> The SO driving this enemy's stats. </summary>
        public EnemyStatsSO Stats => _stats;

        /// <summary> Current hit points. </summary>
        public float CurrentHP => _currentHP;

        /// <inheritdoc/>
        public bool IsAlive => !_isDead;

        /// <summary> Current facing direction (normalized velocity or last move dir). </summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _poolRef = GetComponent<PoolReference>();

            InitializeFromStats();
        }

        /// <summary>
        /// Read values from the referenced EnemyStatsSO and set runtime state.
        /// </summary>
        private void InitializeFromStats()
        {
            if (_stats == null)
            {
                Debug.LogError($"[EnemyEntity] {gameObject.name} has no EnemyStatsSO assigned!");
                return;
            }

            _currentHP = _stats.MaxHP;
            _currentPoise = _stats.MaxPoise;
            _isDead = false;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _stats.BaseColor;
        }

        // ──────────────────── IDamageable ────────────────────

        /// <inheritdoc/>
        public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (_isDead) return;

            // Apply damage
            _currentHP -= damage;

            // Apply knockback impulse
            if (knockbackForce > 0f && _rigidbody != null)
                _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);

            // Hit flash feedback
            if (_spriteRenderer != null && _stats != null)
                StartCoroutine(HitFlashCoroutine());

            // Notify listeners
            OnDamageTaken?.Invoke(damage, _currentHP);

            // Death check
            if (_currentHP <= 0f)
                Die();
        }

        // ──────────────────── Death ────────────────────

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // Disable collision so no further interactions occur
            if (_collider != null)
                _collider.enabled = false;

            // Stop all movement
            StopMovement();

            // Notify listeners
            OnDeath?.Invoke();

            // Return to pool (or destroy if no pool)
            if (_poolRef != null && _poolRef.OwnerPool != null)
            {
                _poolRef.ReturnToPool();
            }
            else
            {
                // Fallback: deactivate instead of Destroy to stay pool-friendly
                gameObject.SetActive(false);
            }
        }

        // ──────────────────── Movement (called by Brain) ────────────────────

        /// <summary>
        /// Move in the given direction at the stats-configured MoveSpeed.
        /// Direction should be normalized by the caller.
        /// </summary>
        public void MoveTo(Vector2 direction)
        {
            if (_isDead || _stats == null) return;

            Vector2 dir = direction.normalized;
            _rigidbody.linearVelocity = dir * _stats.MoveSpeed;

            if (dir.sqrMagnitude > 0.001f)
                FacingDirection = dir;
        }

        /// <summary>
        /// Immediately stop all movement.
        /// </summary>
        public void StopMovement()
        {
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
        }

        // ──────────────────── Visual Feedback ────────────────────

        private Coroutine _flashCoroutine;

        private IEnumerator HitFlashCoroutine()
        {
            // Flash to white
            _spriteRenderer.color = Color.white;

            float duration = _stats != null ? _stats.HitFlashDuration : 0.1f;
            yield return new WaitForSeconds(duration);

            // Restore base color (if still alive)
            if (!_isDead && _spriteRenderer != null && _stats != null)
                _spriteRenderer.color = _stats.BaseColor;
        }

        // ──────────────────── IPoolable ────────────────────

        /// <summary>
        /// Called when retrieved from the object pool. Reset all runtime state.
        /// </summary>
        public void OnGetFromPool()
        {
            InitializeFromStats();

            // Re-enable collision
            if (_collider != null)
                _collider.enabled = true;

            // Stop any residual movement
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when returned to the object pool. Cleanup references.
        /// </summary>
        public void OnReturnToPool()
        {
            _isDead = true;
            StopAllCoroutines();

            // Clear event subscribers to prevent leaks across pool reuses
            OnDamageTaken = null;
            OnDeath = null;

            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_stats == null) return;

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _stats.AttackRange);

            // Draw sight range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stats.SightRange);

            // Draw leash range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _stats.LeashRange);
        }
#endif
    }
}
