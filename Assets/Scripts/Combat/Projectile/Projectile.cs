using System;
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

        // 命中特效预制体（从 ProjectileParams 或 WeaponStatsSO 获取）
        private GameObject _impactVFXPrefab;

        // 星图扩展钩子
        private readonly List<IProjectileModifier> _modifiers = new();

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

            Debug.Log($"[Projectile] damage={_damage:F1} speed={Speed:F1}");

            _modifiers.Clear();
            if (modifiers != null)
                _modifiers.AddRange(modifiers);

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileSpawned(this);
        }

        /// <summary>
        /// Legacy initializer. Prefer the ProjectileParams overload.
        /// </summary>
        [Obsolete("Use Initialize(Vector2, ProjectileParams, List<IProjectileModifier>) instead.")]
        public void Initialize(Vector2 direction, WeaponStatsSO stats,
                               List<IProjectileModifier> modifiers = null)
        {
            Initialize(direction, ProjectileParams.FromWeaponStats(stats), modifiers);
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

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].OnProjectileHit(this, other);

            // TODO: 检查 IDamageable 接口对敌人造成伤害 (待敌人系统实现)

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
            _modifiers.Clear();
            ShouldDestroyOnHit = true;
            transform.localScale = Vector3.one;
        }
    }
}
