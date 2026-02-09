using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Light family projectile: instant-hit laser beam using Physics2D.Raycast.
    /// Renders a LineRenderer from origin to hit point (or max range),
    /// fades out over a short duration, then returns to pool.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeam : MonoBehaviour, IPoolable
    {
        [Header("Beam Settings")]
        [SerializeField] private float _beamDuration = 0.1f;
        [SerializeField] private float _fadeDuration = 0.05f;
        [SerializeField] private float _beamStartWidth = 0.15f;
        [SerializeField] private float _beamEndWidth = 0.05f;

        [Header("Collision")]
        [SerializeField] private LayerMask _hitMask = ~0;

        private LineRenderer _lineRenderer;
        private PoolReference _poolRef;

        private float _damage;
        private float _knockback;
        private float _timer;
        private float _totalDuration;
        private bool _isAlive;

        private float _initialStartWidth;
        private float _initialEndWidth;
        private Color _initialStartColor;
        private Color _initialEndColor;

        // Modifier support
        private readonly List<IProjectileModifier> _modifiers = new();

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _poolRef = GetComponent<PoolReference>();
        }

        /// <summary>
        /// Fire the laser beam: perform raycast, render line, schedule fade-out.
        /// Called by StarChartController.SpawnLightBeam().
        /// </summary>
        public void Fire(Vector2 origin, Vector2 direction, ProjectileParams parms,
                         List<IProjectileModifier> modifiers)
        {
            _damage = parms.Damage;
            _knockback = parms.Knockback;
            _isAlive = true;
            _totalDuration = _beamDuration + _fadeDuration;
            _timer = _totalDuration;

            // Collect modifiers
            _modifiers.Clear();
            if (modifiers != null)
                _modifiers.AddRange(modifiers);

            // Calculate max range from speed * lifetime
            float maxRange = parms.Speed * parms.Lifetime;
            if (maxRange <= 0f) maxRange = 20f; // Fallback

            // Perform raycast
            Vector2 dir = direction.normalized;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxRange, _hitMask);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;

                // Notify modifiers about the hit
                for (int i = 0; i < _modifiers.Count; i++)
                    _modifiers[i].OnProjectileHit(null, hit.collider);

                // Placeholder damage log
                Debug.Log($"[LaserBeam] Hit {hit.collider.name} at {hit.point}, " +
                          $"damage={_damage:F1}, knockback={_knockback:F1}");

                // Spawn impact VFX
                SpawnImpactVFX(hit.point, parms.ImpactVFXPrefab);
            }
            else
            {
                endPoint = origin + dir * maxRange;
            }

            // Configure LineRenderer
            SetupLineRenderer(origin, endPoint);

            // Store initial visual state for fading
            _initialStartWidth = _lineRenderer.startWidth;
            _initialEndWidth = _lineRenderer.endWidth;
            _initialStartColor = _lineRenderer.startColor;
            _initialEndColor = _lineRenderer.endColor;
        }

        private void SetupLineRenderer(Vector2 start, Vector2 end)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, (Vector3)start);
            _lineRenderer.SetPosition(1, (Vector3)end);
            _lineRenderer.startWidth = _beamStartWidth;
            _lineRenderer.endWidth = _beamEndWidth;
        }

        private void Update()
        {
            if (!_isAlive) return;

            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                ReturnToPool();
                return;
            }

            // Fade out during the fade phase
            if (_timer < _fadeDuration)
            {
                float fadeT = _timer / _fadeDuration; // 1 → 0

                // Fade width
                _lineRenderer.startWidth = _initialStartWidth * fadeT;
                _lineRenderer.endWidth = _initialEndWidth * fadeT;

                // Fade alpha
                Color startColor = _initialStartColor;
                startColor.a = _initialStartColor.a * fadeT;
                _lineRenderer.startColor = startColor;

                Color endColor = _initialEndColor;
                endColor.a = _initialEndColor.a * fadeT;
                _lineRenderer.endColor = endColor;
            }
        }

        private void SpawnImpactVFX(Vector2 hitPoint, GameObject impactVFXPrefab)
        {
            if (impactVFXPrefab == null) return;
            if (PoolManager.Instance == null) return;

            var pool = PoolManager.Instance.GetPool(impactVFXPrefab, 5, 20);
            pool.Get(hitPoint, Quaternion.identity);
        }

        private void ReturnToPool()
        {
            if (!_isAlive) return;
            _isAlive = false;

            if (_poolRef != null)
                _poolRef.ReturnToPool();
        }

        // --- IPoolable ---

        public void OnGetFromPool()
        {
            _isAlive = true;
            _lineRenderer = _lineRenderer != null ? _lineRenderer : GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            _modifiers.Clear();

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
                _lineRenderer.positionCount = 0;

                // Reset width and color
                _lineRenderer.startWidth = _beamStartWidth;
                _lineRenderer.endWidth = _beamEndWidth;
            }
        }
    }
}
