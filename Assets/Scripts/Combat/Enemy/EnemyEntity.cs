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
        // ──────────────────── Global Events ────────────────────

        /// <summary>
        /// Broadcast when ANY enemy dies. Used by EnemyFear system for fear propagation.
        /// Parameters: death position (world), the dying enemy's EnemyStatsSO.
        /// </summary>
        public static event Action<Vector2, EnemyStatsSO> OnAnyEnemyDeath;
        // ──────────────────── Inspector ────────────────────
        [Header("Data")]
        [SerializeField] private EnemyStatsSO _stats;

        // ──────────────────── Runtime State ────────────────────
        private float _currentHP;
        private float _currentPoise;
        private bool _isDead;

        // ──────────────────── Runtime Stat Overrides (set by affixes) ────────────────────
        // These start at base SO values and get multiplied by affixes at spawn time.
        private float _runtimeMaxHP;
        private float _runtimeDamageMultiplier = 1f;
        private float _runtimeSpeedMultiplier = 1f;

        // ──────────────────── Cached Components ────────────────────
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private PoolReference _poolRef;

        // ──────────────────── Boids Separation ────────────────────
        private static int _enemyLayerMask = -1;
        private static int EnemyLayerMask
        {
            get
            {
                if (_enemyLayerMask < 0)
                    _enemyLayerMask = LayerMask.GetMask("Enemy");
                return _enemyLayerMask;
            }
        }

        // ──────────────────── Events ────────────────────
        /// <summary> Fired when this enemy takes damage. (damage, currentHP) </summary>
        public event Action<float, float> OnDamageTaken;

        /// <summary> Fired when this enemy dies. </summary>
        public event Action OnDeath;

        /// <summary> Fired when poise is broken (reaches 0). Brain subscribes to force StaggerState. </summary>
        public event Action OnPoiseBroken;

        // ──────────────────── Public Properties ────────────────────
        /// <summary> The SO driving this enemy's stats. </summary>
        public EnemyStatsSO Stats => _stats;

        /// <summary> Current hit points. </summary>
        public float CurrentHP => _currentHP;

        /// <summary> Current poise value. When ≤ 0, enemy is staggered. </summary>
        public float CurrentPoise => _currentPoise;

        /// <summary> Whether poise is currently broken (stagger active). </summary>
        public bool IsStaggered { get; private set; }

        /// <inheritdoc/>
        public bool IsAlive => !_isDead;

        /// <summary> Current facing direction (normalized velocity or last move dir). </summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        /// <summary> Whether this enemy is invulnerable (e.g., during boss phase transition). </summary>
        public bool IsInvulnerable { get; set; }

        /// <summary> Whether this enemy is currently blocking incoming attacks. </summary>
        public bool IsBlocking { get; set; }

        /// <summary> Damage reduction multiplier while blocking (set by BlockState). </summary>
        public float BlockDamageReduction { get; set; }

        /// <summary> Runtime damage multiplier (base * affix multipliers). </summary>
        public float RuntimeDamageMultiplier => _runtimeDamageMultiplier;

        /// <summary> Runtime speed multiplier (base * affix multipliers). </summary>
        public float RuntimeSpeedMultiplier => _runtimeSpeedMultiplier;

        /// <summary> Runtime max HP (base * affix multipliers). </summary>
        public float RuntimeMaxHP => _runtimeMaxHP;

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

            _runtimeMaxHP = _stats.MaxHP;
            _runtimeDamageMultiplier = 1f;
            _runtimeSpeedMultiplier = 1f;
            _currentHP = _runtimeMaxHP;
            _currentPoise = _stats.MaxPoise;
            _isDead = false;
            IsStaggered = false;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _stats.BaseColor;
        }

        /// <summary>
        /// Apply an affix's stat multipliers to the runtime stats.
        /// Called by EnemyAffixController during spawn.
        /// </summary>
        public void ApplyAffixMultipliers(float hpMult, float damageMult, float speedMult)
        {
            _runtimeMaxHP *= hpMult;
            _runtimeDamageMultiplier *= damageMult;
            _runtimeSpeedMultiplier *= speedMult;
            _currentHP = _runtimeMaxHP;
        }

        // ──────────────────── IDamageable ────────────────────

        /// <inheritdoc/>
        public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (_isDead) return;

            // Invulnerability check (boss phase transition)
            if (IsInvulnerable) return;

            // Apply block damage reduction
            float finalDamage = damage;
            if (IsBlocking && BlockDamageReduction > 0f)
            {
                finalDamage *= (1f - BlockDamageReduction);
            }

            // Apply damage
            _currentHP -= finalDamage;

            // Apply knockback impulse
            if (knockbackForce > 0f && _rigidbody != null)
                _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);

            // Hit flash feedback (skip during stagger — stagger has its own color)
            if (!IsStaggered && _spriteRenderer != null && _stats != null)
                StartCoroutine(HitFlashCoroutine());

            // Poise reduction: damage also reduces poise (uses reduced damage if blocking)
            if (!IsStaggered && _stats != null && _stats.MaxPoise > 0f)
            {
                _currentPoise -= finalDamage;
                if (_currentPoise <= 0f)
                {
                    IsStaggered = true;
                    _currentPoise = 0f;
                    // 顿帧：韧性击破时触发较强的 HitStop
                    Core.HitStopEffect.Trigger(0.08f);
                    OnPoiseBroken?.Invoke();
                }
            }

            // Notify listeners (pass actual damage dealt, accounting for block)
            OnDamageTaken?.Invoke(finalDamage, _currentHP);

            // Death check
            if (_currentHP <= 0f)
                Die();
        }

        // ──────────────────── Death ────────────────────

        /// <summary>
        /// Reset poise to maximum. Called by StaggerState when stagger ends.
        /// </summary>
        public void ResetPoise()
        {
            if (_stats != null)
                _currentPoise = _stats.MaxPoise;
            IsStaggered = false;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // 顿帧：击杀时触发中等 HitStop
            Core.HitStopEffect.Trigger(0.06f);

            // Disable collision so no further interactions occur
            if (_collider != null)
                _collider.enabled = false;

            // Stop all movement
            StopMovement();

            // Notify listeners
            OnDeath?.Invoke();

            // Global broadcast for fear system and other cross-enemy reactions
            OnAnyEnemyDeath?.Invoke(transform.position, _stats);

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
            _rigidbody.linearVelocity = dir * _stats.MoveSpeed * _runtimeSpeedMultiplier;

            if (dir.sqrMagnitude > 0.001f)
                FacingDirection = dir;
        }

        /// <summary>
        /// Move in the given direction at a custom speed (overrides MoveSpeed).
        /// Used by Stalker disengage, Fear flee, and other variable-speed behaviors.
        /// </summary>
        public void MoveAtSpeed(Vector2 direction, float speed)
        {
            if (_isDead) return;

            Vector2 dir = direction.normalized;
            _rigidbody.linearVelocity = dir * speed;

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

        // ──────────────────── Overlap Resolution (FixedUpdate) ────────────────────

        // 最小允许距离（略大于两个 CircleCollider2D 半径之和 ≈ 0.8）
        private const float MIN_ENEMY_DISTANCE = 0.9f;

        // 共享静态缓冲区：NonAlloc 查询复用，零 GC 分配
        // 16 = 同屏同一小范围内的邻居上限，远超实际需求
        private static readonly Collider2D[] _neighborBuffer = new Collider2D[16];

        /// <summary>
        /// Continuous overlap resolution in FixedUpdate.
        /// Directly adjusts position when enemies are too close, regardless of AI state.
        /// This ensures enemies never stack even during stopped states (Engage/Stagger/etc.).
        /// </summary>
        private void FixedUpdate()
        {
            if (_isDead) return;
            ResolveOverlap();
        }

        private void ResolveOverlap()
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, MIN_ENEMY_DISTANCE, _neighborBuffer, EnemyLayerMask);

            for (int i = 0; i < count; i++)
            {
                if (_neighborBuffer[i].gameObject == gameObject) continue;

                Vector2 away = (Vector2)transform.position - (Vector2)_neighborBuffer[i].transform.position;
                float dist = away.magnitude;

                if (dist >= MIN_ENEMY_DISTANCE) continue;

                // 完全重叠 → 随机方向推开（避免死锁在同一点）
                if (dist < 0.01f)
                {
                    float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    dist = 0.01f;
                }

                // 按重叠量推开：越近推得越多，每人各承担一半
                float overlap = MIN_ENEMY_DISTANCE - dist;
                Vector2 push = away.normalized * (overlap * 0.5f);
                _rigidbody.position += push;
            }
        }

        // ──────────────────── Boids Separation (for ChaseState blending) ────────────────────

        /// <summary>
        /// Calculate a separation force vector pushing this enemy away from nearby enemies.
        /// Uses inverse-distance weighting so closer neighbors exert stronger push.
        /// Call from ChaseState to blend with pursuit direction.
        /// </summary>
        /// <param name="radius">Detection radius for neighbors.</param>
        /// <param name="strength">Maximum magnitude of the separation force.</param>
        public Vector2 GetSeparationForce(float radius = 1.5f, float strength = 2f)
        {
            Vector2 separation = Vector2.zero;
            int neighborCount = 0;

            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, radius, _neighborBuffer, EnemyLayerMask);

            for (int i = 0; i < count; i++)
            {
                if (_neighborBuffer[i].gameObject == gameObject) continue;

                Vector2 away = (Vector2)transform.position - (Vector2)_neighborBuffer[i].transform.position;
                float dist = away.magnitude;

                // 完全重叠时使用随机方向，避免死锁
                if (dist < 0.01f)
                {
                    float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    dist = 0.01f;
                }

                // Inverse distance: closer neighbors push harder
                separation += away.normalized / dist;
                neighborCount++;
            }

            if (neighborCount > 0)
                separation = separation.normalized * strength;

            return separation;
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
            OnPoiseBroken = null;
            IsStaggered = false;
            IsBlocking = false;
            BlockDamageReduction = 0f;
            IsInvulnerable = false;

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
