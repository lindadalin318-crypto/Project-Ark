using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Echo family projectile: expanding shockwave AOE.
    /// Spawns at the fire point with a small initial radius, then linearly expands
    /// its CircleCollider2D over time. Each enemy is hit at most once per wave.
    /// Passes through walls. Supports sector-limited spread via angle checks.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class EchoWave : MonoBehaviour, IPoolable
    {
        [Header("Wave Settings")]
        [SerializeField] private float _initialRadius = 0.2f;
        [SerializeField] private float _visualScaleMultiplier = 2f;

        [Header("Collision")]
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _wallMask;

        private CircleCollider2D _circleCollider;
        private PoolReference _poolRef;

        // Runtime state
        private bool _isAlive;
        private float _timer;
        private float _lifetime;
        private float _expandSpeed;
        private float _damage;
        private float _knockback;
        private GameObject _impactVFXPrefab;

        // Direction and sector support
        private Vector2 _direction;
        private float _spreadAngle; // Half-angle in degrees; 0 = full circle
        private bool _useSector;

        // Deduplication: each enemy hit at most once per wave
        private readonly HashSet<Collider2D> _hitEnemies = new();

        // Modifier support
        private readonly List<IProjectileModifier> _modifiers = new();

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            _circleCollider.isTrigger = true;
            _poolRef = GetComponent<PoolReference>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // If masks were not configured in the Inspector, set sensible defaults
            if (_enemyMask == 0)
                _enemyMask = LayerMask.GetMask("Enemy");
            if (_wallMask == 0)
                _wallMask = LayerMask.GetMask("Wall");

            // Generate a procedural circle sprite if none is assigned
            if (_spriteRenderer != null && _spriteRenderer.sprite == null)
                _spriteRenderer.sprite = CreateCircleSprite(64);
        }

        /// <summary>
        /// Creates a procedural filled-circle sprite for visual fallback.
        /// </summary>
        private static Sprite CreateCircleSprite(int resolution)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = resolution * 0.5f;
            float radiusSq = center * center;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSq = dx * dx + dy * dy;
                    tex.SetPixel(x, y, distSq <= radiusSq ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                                 new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// Initialize and activate the echo wave.
        /// Called by StarChartController.SpawnEchoWave().
        /// </summary>
        /// <param name="origin">Spawn position (world space).</param>
        /// <param name="direction">Fire direction (for sector waves).</param>
        /// <param name="parms">Projectile parameters (speed = expand rate, lifetime, damage, etc.).</param>
        /// <param name="modifiers">Injected projectile modifiers (Tint effects, etc.).</param>
        /// <param name="spreadAngle">Half-angle spread in degrees. 0 = full 360° ring.</param>
        public void Fire(Vector2 origin, Vector2 direction, ProjectileParams parms,
                         List<IProjectileModifier> modifiers, float spreadAngle)
        {
            _isAlive = true;
            _timer = 0f;
            _lifetime = parms.Lifetime;
            _expandSpeed = parms.Speed;
            _damage = parms.Damage;
            _knockback = parms.Knockback;
            _impactVFXPrefab = parms.ImpactVFXPrefab;

            _direction = direction.normalized;
            _spreadAngle = spreadAngle;
            _useSector = spreadAngle > 0f && spreadAngle < 180f;

            // Reset collider
            _circleCollider.radius = _initialRadius;
            _circleCollider.enabled = true;

            // Clear dedup set
            _hitEnemies.Clear();

            // Collect modifiers
            _modifiers.Clear();
            if (modifiers != null)
                _modifiers.AddRange(modifiers);

            // Update visual scale to match initial radius
            UpdateVisualScale(_initialRadius);
        }

        private void Update()
        {
            if (!_isAlive) return;

            _timer += Time.deltaTime;

            if (_timer >= _lifetime)
            {
                ReturnToPool();
                return;
            }

            // Expand collider radius linearly based on speed
            float currentRadius = _initialRadius + _expandSpeed * _timer;
            _circleCollider.radius = currentRadius;

            // Update visual scale to match
            UpdateVisualScale(currentRadius);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isAlive) return;

            // Ignore walls (pass-through characteristic)
            if (IsInLayerMask(other.gameObject.layer, _wallMask))
                return;

            // Only process enemies
            if (!IsInLayerMask(other.gameObject.layer, _enemyMask))
                return;

            // Deduplication: skip if already hit this enemy in this wave
            if (_hitEnemies.Contains(other))
                return;

            // Sector check: if using sector mode, verify the enemy is within the arc
            if (_useSector)
            {
                Vector2 toEnemy = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                float angleBetween = Vector2.Angle(_direction, toEnemy);
                if (angleBetween > _spreadAngle)
                    return;
            }

            // Mark as hit
            _hitEnemies.Add(other);

            // Notify modifiers about the hit
            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileHit(null, other);

            // Placeholder damage log
            Debug.Log($"[EchoWave] Hit {other.name}, damage={_damage:F1}, knockback={_knockback:F1}");

            // Spawn impact VFX at enemy position
            SpawnImpactVFX(other.transform.position);
        }

        private void UpdateVisualScale(float radius)
        {
            // Scale the transform so sprite/particle visuals match collider size
            float diameter = radius * _visualScaleMultiplier;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private void SpawnImpactVFX(Vector2 hitPoint)
        {
            if (_impactVFXPrefab == null) return;
            if (PoolManager.Instance == null) return;

            var pool = PoolManager.Instance.GetPool(_impactVFXPrefab, 5, 20);
            pool.Get(hitPoint, Quaternion.identity);
        }

        private void ReturnToPool()
        {
            if (!_isAlive) return;
            _isAlive = false;

            if (_poolRef != null)
                _poolRef.ReturnToPool();
        }

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        // --- IPoolable ---

        public void OnGetFromPool()
        {
            _isAlive = true;
            _timer = 0f;
            _hitEnemies.Clear();
            _circleCollider = _circleCollider != null ? _circleCollider : GetComponent<CircleCollider2D>();
            _circleCollider.radius = _initialRadius;
            _circleCollider.enabled = false;
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            _modifiers.Clear();
            _hitEnemies.Clear();

            if (_circleCollider != null)
            {
                _circleCollider.radius = _initialRadius;
                _circleCollider.enabled = false;
            }

            // Reset scale
            transform.localScale = Vector3.one;
        }
    }
}
