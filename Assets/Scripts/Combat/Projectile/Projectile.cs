using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Combat.HyperWind;
using ProjectArk.HyperWind;

namespace ProjectArk.Combat


{
    /// <summary>
    /// Physical projectile entity. Moves in a straight line via Rigidbody2D,
    /// handles collision detection, lifetime expiry, and pool recycling.
    /// Supports <see cref="IProjectileModifier"/> hooks for Star Chart extensibility.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, IPoolable, ICycloneCaptureTarget

    {
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private PoolReference _poolRef;
        private TrailRenderer _trail;


        private float _damage;
        private float _knockback;
        private float _lifetimeTimer;
        private bool _isAlive;
        private bool _isCycloneCaptured;
        private DamageType _damageType;


        // Impact VFX prefab (from ProjectileParams)
        private GameObject _impactVFXPrefab;

        // 星图扩展钩子
        private readonly List<IProjectileModifier> _modifiers = new();

        [Header("HyperWind")]
        [Tooltip("When enabled, this physical projectile drifts under IWindFieldService. Laser/Echo families do not use this component.")]
        [SerializeField] private bool _enableWindFieldDrift = true;

        [Tooltip("Acceleration applied from sampled wind velocity. Higher values bend projectile trajectories faster.")]
        [SerializeField] [Min(0f)] private float _windDriftAcceleration = 2f;

        [Tooltip("Maximum accumulated wind drift velocity added on top of the projectile's authored velocity.")]
        [SerializeField] [Min(0f)] private float _maxWindDriftSpeed = 8f;

        private IWindFieldService _windFieldService;
        private Vector2 _windDriftVelocity;
        private bool _windDriftApplied;

        // Cached layer indices for collision filtering

        private static int _playerLayer = -1;
        private static int PlayerLayer => _playerLayer >= 0 ? _playerLayer : (_playerLayer = LayerMask.NameToLayer("Player"));

        // --- 供 IProjectileModifier 读写的公共属性 ---

        /// <summary> Current movement direction (normalized). </summary>
        public Vector2 Direction { get; set; }

        /// <summary> Current speed (units/second). </summary>
        public float Speed { get; set; }

        /// <summary> Damage this projectile will deal on hit. </summary>
        public float Damage => _damage;

        /// <summary> Knockback force applied to hit target. </summary>
        public float Knockback => _knockback;

        /// <summary>
        /// When false, the projectile will not be recycled on hit (pierce / boomerang behavior).
        /// Reset to true on pool return.
        /// </summary>
        public bool ShouldDestroyOnHit { get; set; } = true;

        public Transform CapturableTransform => transform;
        public GameObject CapturableGameObject => gameObject;
        public bool CanBeCapturedByCyclone => _isAlive && !_isCycloneCaptured && gameObject.activeInHierarchy;
        public float CycloneBaseSpeed => Speed;

        /// <summary>

        /// Remaining lifetime in seconds. Readable and writable by IProjectileModifier
        /// implementations (e.g. MinePlacerModifier extends it at spawn time).
        /// </summary>
        public float LifetimeRemaining
        {
            get => _lifetimeTimer;
            set => _lifetimeTimer = value;
        }

        // --- Procedural fallback defaults ---
        // Used only when the prefab has no sprite / no TrailRenderer AND the Core SO
        // does not supply its own trail parameters. Not intended as the long-term owner.
        // Per ProceduralPresentation_WorkflowSpec §3.5 / §7-3, every usage of these
        // defaults must be surfaced via Debug.LogWarning so missing assets don't
        // silently reach shipping.
        private const float DEFAULT_TRAIL_TIME = 0.15f;
        private const float DEFAULT_TRAIL_WIDTH = 0.085f;
        private static readonly Color DEFAULT_TRAIL_COLOR = Color.white;
        private static readonly HashSet<string> MissingTrailWarningKeys = new();

        private void Awake()

        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _poolRef = GetComponent<PoolReference>();
            _trail = GetComponent<TrailRenderer>();


            // Fallback: generate a procedural sprite if SpriteRenderer has none assigned.
            // Prevents invisible projectiles when the prefab was created without a sprite.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CreateFallbackSprite(8);
                Debug.LogWarning(
                    $"[Projectile] Missing sprite on '{name}' — generated 8px procedural fallback. " +
                    $"Assign a real sprite to the prefab's SpriteRenderer, or treat this bullet via a Core that supplies its own visual.",
                    this);
            }

            // Ensure TrailRenderer exists and is properly configured
            // (replicates the classic BasicBullet trail effect).
            // NOTE: parameters here are default fallback values; the Core SO's
            // TrailTime / TrailWidth / TrailColor (if set) will override these
            // at Initialize() time.
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrail(_trail, sr, DEFAULT_TRAIL_TIME, DEFAULT_TRAIL_WIDTH, DEFAULT_TRAIL_COLOR);

                string warningKey = name.Replace("(Clone)", string.Empty).Trim();
                if (MissingTrailWarningKeys.Add(warningKey))
                {
                    Debug.LogWarning(
                        $"[Projectile] '{name}' has no TrailRenderer — added one at runtime with default parameters " +
                        $"(time={DEFAULT_TRAIL_TIME:F2}, width={DEFAULT_TRAIL_WIDTH:F3}). " +
                        $"For shipping, add a pre-configured TrailRenderer to the prefab or drive the parameters from StarCoreSO. " +
                        $"This warning is reported once per projectile prefab key to avoid pool prewarm spam.",
                        this);
                }
            }

        }

        /// <summary>
        /// Configures a TrailRenderer with the classic BasicBullet trail style:
        /// short color-to-transparent fade that tapers from a given head width to zero.
        /// Parameters come from <see cref="StarCoreSO"/> when available, otherwise from
        /// <see cref="DEFAULT_TRAIL_TIME"/> / <see cref="DEFAULT_TRAIL_WIDTH"/> / <see cref="DEFAULT_TRAIL_COLOR"/>.
        /// </summary>
        private static void ConfigureTrail(TrailRenderer trail, SpriteRenderer sr,
                                           float trailTime, float trailWidth, Color trailColor)
        {
            trail.time = trailTime;
            trail.minVertexDistance = 0.1f;
            trail.autodestruct = false;
            trail.emitting = true;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.alignment = LineAlignment.TransformZ;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.generateLightingData = false;

            // Width curve: taper from head width to 0 at tail
            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, trailWidth),
                new Keyframe(1f, 0f));

            // Color gradient: solid color → transparent color (inherits sprite tint on top)
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            // Use the same material as the SpriteRenderer so the trail
            // renders correctly under URP 2D.
            if (sr != null && sr.sharedMaterial != null)
                trail.sharedMaterial = sr.sharedMaterial;
        }

        /// <summary>
        /// Creates a small filled-circle sprite as a runtime fallback.
        /// </summary>
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
                    float distSq = dx * dx + dy * dy;
                    tex.SetPixel(x, y, distSq <= radiusSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();

            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                                            new Vector2(0.5f, 0.5f), resolution);
            return _fallbackSprite;
        }

        /// <summary>
        /// Initialize from the Star Chart pipeline with computed params and optional modifiers.
        /// </summary>
        public void Initialize(Vector2 direction, ProjectileParams parms,
                               List<IProjectileModifier> modifiers = null)
        {
            Direction = direction.normalized;
            Speed = parms.Speed;
            _damage = parms.Damage;
            _knockback = parms.Knockback;
            _damageType = parms.DamageType;
            _lifetimeTimer = parms.Lifetime;
            _impactVFXPrefab = parms.ImpactVFXPrefab;
            _isAlive = true;
            _isCycloneCaptured = false;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            _windDriftVelocity = Vector2.zero;
            _windDriftApplied = false;

            CombatEvents.RaisePlayerProjectileFired(transform.position, Direction);


            // Apply Core-driven trail overrides if supplied (negative / transparent => keep fallback).

            ApplyTrailOverrides(parms);

            _rigidbody.linearVelocity = Direction * Speed;

            _modifiers.Clear();
            if (modifiers != null)
                _modifiers.AddRange(modifiers);

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileSpawned(this);
        }

        /// <summary>
        /// Overrides the TrailRenderer configuration with per-Core parameters
        /// when the caller provided them. Negative dimensions / alpha==0 are
        /// treated as "not specified" and the existing trail configuration is kept.
        /// </summary>
        private void ApplyTrailOverrides(ProjectileParams parms)
        {
            if (_trail == null) return;

            bool hasTime = parms.TrailTime > 0f;
            bool hasWidth = parms.TrailWidth > 0f;
            bool hasColor = parms.TrailColor.a > 0f;

            if (!hasTime && !hasWidth && !hasColor) return;

            var sr = GetComponent<SpriteRenderer>();
            float time = hasTime ? parms.TrailTime : DEFAULT_TRAIL_TIME;
            float width = hasWidth ? parms.TrailWidth : DEFAULT_TRAIL_WIDTH;
            Color color = hasColor ? parms.TrailColor : DEFAULT_TRAIL_COLOR;
            ConfigureTrail(_trail, sr, time, width, color);
            _trail.Clear();
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

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileUpdate(this, Time.deltaTime);

            ApplyWindFieldDrift(Time.deltaTime);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isAlive) return;

            // Ignore collisions with same layer (other projectiles) and Player layer
            int otherLayer = other.gameObject.layer;
            if (otherLayer == gameObject.layer || otherLayer == PlayerLayer)
                return;

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileHit(this, other);

            // Deal damage via IDamageable interface
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                Vector2 knockbackDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                var payload = new DamagePayload(_damage, _damageType, knockbackDir, _knockback, gameObject);
                damageable.TakeDamage(payload);
            }

            SpawnImpactVFX();

            // If a modifier set ShouldDestroyOnHit to false, skip pool return (pierce / boomerang)
            if (ShouldDestroyOnHit)
                ReturnToPool();
        }

        private void SpawnImpactVFX()
        {
            if (_impactVFXPrefab == null) return;
            if (PoolManager.Instance == null) return;

            var pool = PoolManager.Instance.GetPool(_impactVFXPrefab, 5, 20);
            pool.Get(transform.position, transform.rotation);
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
            Speed *= safeSpeedMultiplier;
            _damage *= safeDamageMultiplier;
            _rigidbody.linearVelocity = Direction * Speed;
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

        /// <summary>
        /// Public entry point for modifiers (e.g. BoomerangModifier) to force-recycle this projectile.
        /// </summary>
        public void ForceReturnToPool()
        {
            ReturnToPool();
        }

        // --- IPoolable ---

        public void OnGetFromPool()
        {
            _isAlive = true;
            if (_trail != null)
                _trail.Clear();
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            _isCycloneCaptured = false;
            ResetWindDriftState();
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            _rigidbody.linearVelocity = Vector2.zero;

            // Destroy dynamically-added modifier components (e.g. BoomerangModifier)


            // to prevent component accumulation across pool reuses.
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i] is MonoBehaviour mb && mb != null && mb.gameObject == gameObject)
                    Destroy(mb);
            }

            _modifiers.Clear();
            ShouldDestroyOnHit = true;
            transform.localScale = Vector3.one;
        }
    }
}
