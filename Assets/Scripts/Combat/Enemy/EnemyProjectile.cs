using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Combat.HyperWind;
using ProjectArk.HyperWind;

namespace ProjectArk.Combat.Enemy


{
    /// <summary>
    /// Enemy projectile entity. Moves in a straight line, hits Player layer,
    /// and recycles via the object pool. Ignores Enemy layer collisions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour, IPoolable, ICycloneCaptureTarget

    {
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private PoolReference _poolRef;
        private TrailRenderer _trail;


        private float _damage;
        private float _knockback;
        private float _speed;
        private float _lifetimeTimer;
        private bool _isAlive;
        private bool _isCycloneCaptured;

        private Vector3 _originalScale;


        [Header("HyperWind")]
        [Tooltip("When enabled, enemy physical projectiles drift under IWindFieldService.")]
        [SerializeField] private bool _enableWindFieldDrift = true;

        [Tooltip("Acceleration applied from sampled wind velocity. Higher values bend projectile trajectories faster.")]
        [SerializeField] [Min(0f)] private float _windDriftAcceleration = 2f;

        [Tooltip("Maximum accumulated wind drift velocity added on top of the projectile's authored velocity.")]
        [SerializeField] [Min(0f)] private float _maxWindDriftSpeed = 8f;

        private IWindFieldService _windFieldService;
        private Vector2 _windDriftVelocity;
        private bool _windDriftApplied;

        // Cached layer indices

        private static int _enemyLayer = -1;
        private static int EnemyLayer => _enemyLayer >= 0 ? _enemyLayer : (_enemyLayer = LayerMask.NameToLayer("Enemy"));

        // ──────────────────── Public Properties ────────────────────
        public Vector2 Direction { get; set; }
        public float Speed => _speed;
        public float Damage => _damage;
        public Transform CapturableTransform => transform;
        public GameObject CapturableGameObject => gameObject;
        public bool CanBeCapturedByCyclone => _isAlive && !_isCycloneCaptured && gameObject.activeInHierarchy;
        public float CycloneBaseSpeed => _speed;

        private void Awake()

        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
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
            _isCycloneCaptured = false;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            _windDriftVelocity = Vector2.zero;
            _windDriftApplied = false;

            _rigidbody.linearVelocity = Direction * _speed;


        }

        private void Update()
        {
            if (!_isAlive) return;

            RemoveAppliedWindDrift();

            _lifetimeTimer -= Time.deltaTime;
            if (_lifetimeTimer <= 0f)
            {
                ReturnToPool();
                return;
            }

            ApplyWindFieldDrift(Time.deltaTime);
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
                var payload = new DamagePayload(_damage, DamageType.Physical, knockbackDir, _knockback, gameObject);
                damageable.TakeDamage(payload);
            }

            ReturnToPool();
        }

        private void ApplyWindFieldDrift(float deltaTime)
        {
            if (!_enableWindFieldDrift || _windDriftAcceleration <= 0f || _maxWindDriftSpeed <= 0f)
            {
                return;
            }

            if (_windFieldService == null && !ServiceLocator.TryGet(out _windFieldService))
            {
                return;
            }

            WindSample sample = _windFieldService.Sample(_rigidbody.position);
            Vector2 windAcceleration = sample.Velocity * _windDriftAcceleration;
            _windDriftVelocity += windAcceleration * deltaTime;
            _windDriftVelocity = Vector2.ClampMagnitude(_windDriftVelocity, _maxWindDriftSpeed);

            if (_windDriftVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _rigidbody.linearVelocity += _windDriftVelocity;
            _windDriftApplied = true;

            if (_rigidbody.linearVelocity.sqrMagnitude > 0.0001f)
            {
                Direction = _rigidbody.linearVelocity.normalized;
            }
        }

        private void RemoveAppliedWindDrift()
        {
            if (!_windDriftApplied || _rigidbody == null)
            {
                return;
            }

            _rigidbody.linearVelocity -= _windDriftVelocity;
            _windDriftApplied = false;
        }

        private void ResetWindDriftState()
        {
            _windDriftVelocity = Vector2.zero;
            _windDriftApplied = false;
        }

        public void CaptureByCyclone()
        {
            if (!CanBeCapturedByCyclone)
            {
                return;
            }

            RemoveAppliedWindDrift();
            _isAlive = false;
            _isCycloneCaptured = true;
            _rigidbody.linearVelocity = Vector2.zero;
            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        public void ReleaseFromCyclone(Vector2 direction, float speedMultiplier, float damageMultiplier)
        {
            if (!_isCycloneCaptured)
            {
                return;
            }

            Vector2 releaseDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            float safeSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            float safeDamageMultiplier = Mathf.Max(0f, damageMultiplier);

            _isCycloneCaptured = false;
            _isAlive = true;
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            ResetWindDriftState();
            Direction = releaseDirection;
            _speed *= safeSpeedMultiplier;
            _damage *= safeDamageMultiplier;
            _rigidbody.linearVelocity = Direction * _speed;
        }

        public void DiscardByCyclone()
        {
            if (_isCycloneCaptured)
            {
                _isCycloneCaptured = false;
                if (_collider != null)
                {
                    _collider.enabled = true;
                }
            }

            _isAlive = true;
            ReturnToPool();
        }

        private void ReturnToPool()


        {
            if (!_isAlive) return;
            _isAlive = false;
            ResetWindDriftState();
            _rigidbody.linearVelocity = Vector2.zero;

            if (_poolRef != null)

                _poolRef.ReturnToPool();
        }

        // ──────────────────── IPoolable ────────────────────

        public void OnGetFromPool()
        {
            _isAlive = true;
            _isCycloneCaptured = false;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            transform.localScale = _originalScale;
            if (_trail != null)

                _trail.Clear();
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            ResetWindDriftState();
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
