using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Enemy laser beam: instant-hit raycast beam that targets the Player layer.
    /// Similar to the player's <see cref="ProjectArk.Combat.LaserBeam"/> but simplified:
    ///   - No IProjectileModifier support
    ///   - Hits Player layer instead of Enemy layer
    ///   - Supports charge-up visual (width ramps up during telegraph)
    ///   - Supports sustained beam mode (stays active for a configurable duration)
    ///   - Poolable (IPoolable) — zero Instantiate/Destroy in combat
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class EnemyLaserBeam : MonoBehaviour, IPoolable
    {
        [Header("Beam Settings")]
        [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private float _beamStartWidth = 0.3f;
        [SerializeField] private float _beamEndWidth = 0.1f;

        [Header("Collision")]
        [SerializeField] private LayerMask _hitMask;

        // ──────────────────── Runtime State ────────────────────
        private LineRenderer _lineRenderer;
        private PoolReference _poolRef;

        private float _damage;
        private float _knockback;
        private float _remainingDuration;
        private float _totalDuration;
        private bool _isAlive;
        private bool _hasDamaged;

        // For sustained beam: re-raycast each frame
        private Vector2 _origin;
        private Vector2 _direction;
        private float _maxRange;

        // Visual state for fade-out
        private float _initialStartWidth;
        private float _initialEndWidth;
        private Color _initialStartColor;
        private Color _initialEndColor;

        // ──────────────────── Cached Layer ────────────────────
        private static int _playerLayerMask = -1;
        private static int PlayerLayerMask
        {
            get
            {
                if (_playerLayerMask < 0)
                    _playerLayerMask = LayerMask.GetMask("Player");
                return _playerLayerMask;
            }
        }

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _poolRef = GetComponent<PoolReference>();

            // Default hit mask: Player + Wall
            if (_hitMask == 0)
                _hitMask = LayerMask.GetMask("Player", "Wall");
        }

        // ──────────────────── API ────────────────────

        /// <summary>
        /// Fire the laser beam from a position in a direction.
        /// Performs raycast, renders the beam, deals damage.
        /// For sustained beams, call with duration > 0 — the beam persists and re-raycasts.
        /// </summary>
        /// <param name="origin">World position to fire from.</param>
        /// <param name="direction">Beam direction (will be normalized).</param>
        /// <param name="damage">Damage dealt on hit.</param>
        /// <param name="knockback">Knockback force applied to target.</param>
        /// <param name="range">Maximum beam length.</param>
        /// <param name="duration">How long the beam stays active (seconds). 0 = instant flash.</param>
        /// <param name="width">Visual width of the beam.</param>
        public void Fire(Vector2 origin, Vector2 direction, float damage, float knockback,
                         float range, float duration, float width = 0f)
        {
            _origin = origin;
            _direction = direction.normalized;
            _damage = damage;
            _knockback = knockback;
            _maxRange = range;
            _isAlive = true;
            _hasDamaged = false;

            _totalDuration = duration + _fadeDuration;
            _remainingDuration = _totalDuration;

            // Apply width override if provided
            if (width > 0f)
            {
                _beamStartWidth = width;
                _beamEndWidth = width * 0.4f;
            }

            // Initial raycast and render
            PerformRaycastAndRender();

            // Store visual state for fade-out
            _initialStartWidth = _lineRenderer.startWidth;
            _initialEndWidth = _lineRenderer.endWidth;
            _initialStartColor = _lineRenderer.startColor;
            _initialEndColor = _lineRenderer.endColor;
        }

        /// <summary>
        /// Show a thin aim-line from origin to direction (no damage).
        /// Used during Turret's lock-on phase as a visual telegraph.
        /// </summary>
        /// <param name="origin">World position of the turret.</param>
        /// <param name="direction">Aim direction.</param>
        /// <param name="range">Line length.</param>
        /// <param name="width">Line width (thin for aiming).</param>
        /// <param name="color">Line color.</param>
        public void ShowAimLine(Vector2 origin, Vector2 direction, float range,
                                float width = 0.03f, Color? color = null)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;

            Vector2 endPoint = origin + direction.normalized * range;

            // Check for wall obstruction
            RaycastHit2D wallHit = Physics2D.Raycast(origin, direction.normalized, range,
                LayerMask.GetMask("Wall"));
            if (wallHit.collider != null)
                endPoint = wallHit.point;

            _lineRenderer.SetPosition(0, (Vector3)origin);
            _lineRenderer.SetPosition(1, (Vector3)endPoint);
            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width;

            Color lineColor = color ?? new Color(1f, 0.2f, 0.2f, 0.5f);
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
        }

        /// <summary>
        /// Hide the aim line (turn off LineRenderer).
        /// </summary>
        public void HideAimLine()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
                _lineRenderer.positionCount = 0;
            }
        }

        // ──────────────────── Update ────────────────────

        private void Update()
        {
            if (!_isAlive) return;

            _remainingDuration -= Time.deltaTime;

            if (_remainingDuration <= 0f)
            {
                ReturnToPool();
                return;
            }

            // During active phase (not fading), re-raycast for sustained beams
            if (_remainingDuration > _fadeDuration)
            {
                PerformRaycastAndRender();
            }
            // Fade phase
            else
            {
                float fadeT = _remainingDuration / _fadeDuration; // 1 → 0

                _lineRenderer.startWidth = _initialStartWidth * fadeT;
                _lineRenderer.endWidth = _initialEndWidth * fadeT;

                Color startColor = _initialStartColor;
                startColor.a = _initialStartColor.a * fadeT;
                _lineRenderer.startColor = startColor;

                Color endColor = _initialEndColor;
                endColor.a = _initialEndColor.a * fadeT;
                _lineRenderer.endColor = endColor;
            }
        }

        // ──────────────────── Raycast & Damage ────────────────────

        private void PerformRaycastAndRender()
        {
            RaycastHit2D hit = Physics2D.Raycast(_origin, _direction, _maxRange, _hitMask);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;

                // Deal damage once per fire (not per frame for sustained beams)
                if (!_hasDamaged)
                {
                    var damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null && damageable.IsAlive)
                    {
                        damageable.TakeDamage(_damage, _direction, _knockback);
                        _hasDamaged = true;
                    }
                }
            }
            else
            {
                endPoint = _origin + _direction * _maxRange;
            }

            // Configure LineRenderer
            _lineRenderer.enabled = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, (Vector3)_origin);
            _lineRenderer.SetPosition(1, (Vector3)endPoint);
            _lineRenderer.startWidth = _beamStartWidth;
            _lineRenderer.endWidth = _beamEndWidth;
        }

        // ──────────────────── Pool ────────────────────

        private void ReturnToPool()
        {
            if (!_isAlive) return;
            _isAlive = false;

            if (_poolRef != null)
                _poolRef.ReturnToPool();
        }

        public void OnGetFromPool()
        {
            _isAlive = true;
            _hasDamaged = false;
            _lineRenderer = _lineRenderer != null ? _lineRenderer : GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }

        public void OnReturnToPool()
        {
            _isAlive = false;
            _hasDamaged = false;

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
                _lineRenderer.positionCount = 0;
                _lineRenderer.startWidth = _beamStartWidth;
                _lineRenderer.endWidth = _beamEndWidth;
                _lineRenderer.startColor = Color.white;
                _lineRenderer.endColor = Color.white;
            }
        }
    }
}
