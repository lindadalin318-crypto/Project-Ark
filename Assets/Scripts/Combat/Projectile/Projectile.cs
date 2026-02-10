using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Physical projectile entity. Moves in a straight line via Rigidbody2D,
    /// handles collision detection, lifetime expiry, and pool recycling.
    /// Supports <see cref="IProjectileModifier"/> hooks for Star Chart extensibility.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        private Rigidbody2D _rigidbody;
        private PoolReference _poolRef;
        private TrailRenderer _trail;

        private float _damage;
        private float _knockback;
        private float _lifetimeTimer;
        private bool _isAlive;

        // Impact VFX prefab (from ProjectileParams)
        private GameObject _impactVFXPrefab;

        // 星图扩展钩子
        private readonly List<IProjectileModifier> _modifiers = new();

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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _poolRef = GetComponent<PoolReference>();
            _trail = GetComponent<TrailRenderer>();

            // Fallback: generate a procedural sprite if SpriteRenderer has none assigned.
            // Prevents invisible projectiles when the prefab was created without a sprite.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = CreateFallbackSprite(8);

            // Ensure TrailRenderer exists and is properly configured
            // (replicates the classic BasicBullet trail effect).
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrail(_trail, sr);
            }
        }

        /// <summary>
        /// Configures a TrailRenderer to match the classic BasicBullet trail:
        /// short white-to-transparent fade that tapers from bullet width to zero.
        /// </summary>
        private static void ConfigureTrail(TrailRenderer trail, SpriteRenderer sr)
        {
            trail.time = 0.15f;
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

            // Width curve: taper from 0.085 at head to 0 at tail
            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.085f),
                new Keyframe(1f, 0f));

            // Color gradient: solid white → transparent white (inherits sprite tint)
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
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
            _lifetimeTimer = parms.Lifetime;
            _impactVFXPrefab = parms.ImpactVFXPrefab;
            _isAlive = true;

            _rigidbody.linearVelocity = Direction * Speed;

            _modifiers.Clear();
            if (modifiers != null)
                _modifiers.AddRange(modifiers);

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileSpawned(this);
        }

        private void Update()
        {
            if (!_isAlive) return;

            _lifetimeTimer -= Time.deltaTime;

            if (_lifetimeTimer <= 0f)
            {
                ReturnToPool();
                return;
            }

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileUpdate(this, Time.deltaTime);
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
                damageable.TakeDamage(_damage, knockbackDir, _knockback);
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

        private void ReturnToPool()
        {
            if (!_isAlive) return;
            _isAlive = false;
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
