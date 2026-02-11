using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Enemy projectile entity. Moves in a straight line, hits Player layer,
    /// and recycles via the object pool. Ignores Enemy layer collisions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour, IPoolable
    {
        private Rigidbody2D _rigidbody;
        private PoolReference _poolRef;
        private TrailRenderer _trail;

        private float _damage;
        private float _knockback;
        private float _speed;
        private float _lifetimeTimer;
        private bool _isAlive;

        private Vector3 _originalScale;

        // Cached layer indices
        private static int _enemyLayer = -1;
        private static int EnemyLayer => _enemyLayer >= 0 ? _enemyLayer : (_enemyLayer = LayerMask.NameToLayer("Enemy"));

        // ──────────────────── Public Properties ────────────────────
        public Vector2 Direction { get; set; }
        public float Speed => _speed;
        public float Damage => _damage;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _poolRef = GetComponent<PoolReference>();
            _trail = GetComponent<TrailRenderer>();
            _originalScale = transform.localScale;

            // Fallback sprite if none assigned
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = CreateFallbackSprite(6);

            // Ensure trail renderer
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrail(_trail, sr);
            }
        }

        /// <summary>
        /// Initialize the enemy projectile with direction, speed, damage, etc.
        /// Called by ShootState or any enemy shooting logic.
        /// </summary>
        public void Initialize(Vector2 direction, float speed, float damage,
                               float knockback, float lifetime)
        {
            Direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _knockback = knockback;
            _lifetimeTimer = lifetime;
            _isAlive = true;

            _rigidbody.linearVelocity = Direction * _speed;
        }

        private void Update()
        {
            if (!_isAlive) return;

            _lifetimeTimer -= Time.deltaTime;
            if (_lifetimeTimer <= 0f)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isAlive) return;

            // Ignore collisions with Enemy layer (friendly fire) and same layer
            int otherLayer = other.gameObject.layer;
            if (otherLayer == gameObject.layer || otherLayer == EnemyLayer)
                return;

            // Deal damage via IDamageable interface
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                Vector2 knockbackDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                damageable.TakeDamage(_damage, knockbackDir, _knockback);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!_isAlive) return;
            _isAlive = false;
            _rigidbody.linearVelocity = Vector2.zero;

            if (_poolRef != null)
                _poolRef.ReturnToPool();
        }

        // ──────────────────── IPoolable ────────────────────

        public void OnGetFromPool()
        {
            _isAlive = true;
            transform.localScale = _originalScale;
            if (_trail != null)
                _trail.Clear();
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        // ──────────────────── Trail Configuration ────────────────────

        private static void ConfigureTrail(TrailRenderer trail, SpriteRenderer sr)
        {
            trail.time = 0.12f;
            trail.minVertexDistance = 0.1f;
            trail.autodestruct = false;
            trail.emitting = true;
            trail.alignment = LineAlignment.TransformZ;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.06f),
                new Keyframe(1f, 0f));

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0f), new GradientColorKey(new Color(1f, 0.1f, 0f), 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            if (sr != null && sr.sharedMaterial != null)
                trail.sharedMaterial = sr.sharedMaterial;
        }

        // ──────────────────── Fallback Sprite ────────────────────

        private static Sprite _fallbackSprite;
        private static Sprite CreateFallbackSprite(int resolution)
        {
            if (_fallbackSprite != null) return _fallbackSprite;

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
                    tex.SetPixel(x, y, (dx * dx + dy * dy) <= radiusSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();

            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                                            new Vector2(0.5f, 0.5f), resolution);
            return _fallbackSprite;
        }
    }
}
