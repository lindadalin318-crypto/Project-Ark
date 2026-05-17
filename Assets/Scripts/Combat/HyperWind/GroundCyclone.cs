using System;
using System.Collections.Generic;
using ProjectArk.Core;
using UnityEngine;


namespace ProjectArk.Combat.HyperWind
{
    /// <summary>
    /// Gameplay core for HyperWind L8 ground cyclone: spawn warning, projectile capture/orbit, and inherited-direction burst release.
    /// Visuals are intentionally separate and replaceable.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class GroundCyclone : MonoBehaviour
    {


        private static Vector2 _lastPlayerFireDirection = Vector2.right;

        public static event Action<GroundCyclone, ICycloneCaptureTarget> OnAnyProjectileCaptured;
        public static event Action<GroundCyclone, ICycloneCaptureTarget> OnAnyProjectileReleased;

        [Header("Lifecycle")]

        [SerializeField] [Min(0f)] private float _spawnDuration = 0.5f;
        [SerializeField] [Min(0.1f)] private float _drawDuration = 4f;
        [SerializeField] [Min(0f)] private float _burstDuration = 0.3f;

        [Header("Capture")]
        [SerializeField] [Min(0.1f)] private float _influenceRadius = 4f;
        [SerializeField] [Min(0.1f)] private float _orbitRadius = 2f;
        [SerializeField] [Min(1)] private int _capacity = 15;
        [SerializeField] private LayerMask _captureLayers;
        [SerializeField] private bool _discardOverflow = true;

        [Header("Orbit / Amplify")]
        [SerializeField] private float _orbitDegreesPerSecond = 300f;
        [SerializeField] [Min(0f)] private float _speedMultiplierPerTurn = 0.5f;
        [SerializeField] [Min(1f)] private float _maxSpeedMultiplier = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool _destroyWhenFinished = true;
        [SerializeField] private bool _drawGizmos = true;

        private readonly List<CapturedProjectileState> _captured = new List<CapturedProjectileState>(16);

        private readonly Collider2D[] _overlapBuffer = new Collider2D[64];
        private CircleCollider2D _trigger;
        private GroundCyclonePhase _state = GroundCyclonePhase.Spawn;

        private float _stateElapsed;
        private bool _hasWarnedMissingLayers;

        public int CapturedCount => _captured.Count;
        public int TotalCapturedCount { get; private set; }
        public int TotalReleasedCount { get; private set; }
        public bool IsCapturing => _state == GroundCyclonePhase.Draw;
        public GroundCyclonePhase CurrentPhase => _state;

        public float PhaseProgress01 => Mathf.Clamp01(_stateElapsed / Mathf.Max(0.0001f, GetCurrentPhaseDuration()));
        public float InfluenceRadius => _influenceRadius;
        public float OrbitRadius => _orbitRadius;

        private void Awake()
        {
            EnsureCaptureLayersConfigured();
            _trigger = GetComponent<CircleCollider2D>();
            _trigger.isTrigger = true;
            _trigger.radius = _influenceRadius;
        }


        private void OnEnable()
        {
            CombatEvents.OnPlayerProjectileFired += HandlePlayerProjectileFired;
        }

        private void OnDisable()
        {
            CombatEvents.OnPlayerProjectileFired -= HandlePlayerProjectileFired;
            ReleaseAllCaptured(_lastPlayerFireDirection);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _stateElapsed += deltaTime;

            switch (_state)
            {
                case GroundCyclonePhase.Spawn:
                    if (_stateElapsed >= _spawnDuration)
                    {
                        EnterState(GroundCyclonePhase.Draw);
                    }
                    break;

                case GroundCyclonePhase.Draw:
                    CaptureOverlaps();
                    TickCaptured(deltaTime);
                    if (_stateElapsed >= _drawDuration)
                    {
                        ReleaseAllCaptured(_lastPlayerFireDirection);
                        EnterState(GroundCyclonePhase.Burst);
                    }
                    break;

                case GroundCyclonePhase.Burst:
                    if (_stateElapsed >= _burstDuration)
                    {
                        EnterState(GroundCyclonePhase.Finished);
                    }
                    break;

                case GroundCyclonePhase.Finished:

                    if (_destroyWhenFinished)
                    {
                        Destroy(gameObject);
                    }
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != GroundCyclonePhase.Draw)

            {
                return;
            }

            TryCapture(other);
        }

        private static void HandlePlayerProjectileFired(Vector2 position, Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                _lastPlayerFireDirection = direction.normalized;
            }
        }

        private void EnterState(GroundCyclonePhase nextState)
        {
            _state = nextState;
            _stateElapsed = 0f;
        }

        private float GetCurrentPhaseDuration()
        {
            switch (_state)
            {
                case GroundCyclonePhase.Spawn:
                    return _spawnDuration;
                case GroundCyclonePhase.Draw:
                    return _drawDuration;
                case GroundCyclonePhase.Burst:
                    return _burstDuration;
                default:
                    return 1f;
            }
        }

        private void CaptureOverlaps()

        {
            if (_captureLayers.value == 0)
            {
                if (!_hasWarnedMissingLayers)
                {
                    Debug.LogWarning("[GroundCyclone] Capture layer mask is empty. No projectiles can be captured.", this);
                    _hasWarnedMissingLayers = true;
                }

                return;
            }

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _influenceRadius, _overlapBuffer, _captureLayers);
            for (int i = 0; i < count; i++)
            {
                TryCapture(_overlapBuffer[i]);
            }
        }

        private bool TryCapture(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            var target = other.GetComponent<ICycloneCaptureTarget>();
            if (target == null || !target.CanBeCapturedByCyclone)
            {
                return false;
            }

            if (_captured.Count >= _capacity)
            {
                if (_discardOverflow)
                {
                    target.DiscardByCyclone();
                }

                return false;
            }

            Vector2 offset = (Vector2)target.CapturableTransform.position - (Vector2)transform.position;
            float angle = offset.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg
                : 360f * _captured.Count / Mathf.Max(1, _capacity);

            target.CaptureByCyclone();
            var state = new CapturedProjectileState(target, angle);
            _captured.Add(state);
            TotalCapturedCount++;
            OnAnyProjectileCaptured?.Invoke(this, target);
            PlaceCaptured(state);
            return true;

        }

        private void TickCaptured(float deltaTime)
        {
            float deltaAngle = _orbitDegreesPerSecond * deltaTime;
            float completedTurnDelta = Mathf.Abs(deltaAngle) / 360f;

            for (int i = _captured.Count - 1; i >= 0; i--)
            {
                CapturedProjectileState state = _captured[i];
                if (state.Target == null || state.Target.CapturableGameObject == null)
                {
                    _captured.RemoveAt(i);
                    continue;
                }

                state.AngleDegrees += deltaAngle;
                state.CompletedTurns += completedTurnDelta;
                PlaceCaptured(state);
            }
        }

        private void PlaceCaptured(CapturedProjectileState state)
        {
            float radians = state.AngleDegrees * Mathf.Deg2Rad;
            Vector2 orbitOffset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _orbitRadius;
            state.Target.CapturableTransform.position = (Vector2)transform.position + orbitOffset;
        }

        private void ReleaseAllCaptured(Vector2 direction)
        {
            Vector2 releaseDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            for (int i = 0; i < _captured.Count; i++)
            {
                CapturedProjectileState state = _captured[i];
                if (state.Target == null || state.Target.CapturableGameObject == null)
                {
                    continue;
                }

                float multiplier = Mathf.Clamp(
                    1f + state.CompletedTurns * _speedMultiplierPerTurn,
                    1f,
                    _maxSpeedMultiplier);

                state.Target.ReleaseFromCyclone(releaseDirection, multiplier, multiplier);
                TotalReleasedCount++;
                OnAnyProjectileReleased?.Invoke(this, state.Target);
            }

            _captured.Clear();

        }

        private void Reset()
        {
            ConfigureDefaultCaptureLayers();
        }

        private void EnsureCaptureLayersConfigured()
        {
            if (_captureLayers.value != 0)
            {
                return;
            }

            ConfigureDefaultCaptureLayers();
            if (_captureLayers.value == 0)
            {
                Debug.LogError("[GroundCyclone] Capture layer mask could not be configured. Ensure PlayerProjectile and Default layers exist.", this);
            }
        }

        private void ConfigureDefaultCaptureLayers()
        {
            // EnemyProjectile layer does not exist in the current project yet; EnemyProjectile.prefab is on Default.
            // Keep the mask explicit and rely on ICycloneCaptureTarget filtering until the layer matrix is formalized.
            _captureLayers = LayerMask.GetMask("PlayerProjectile", "Default");
        }

        private void OnValidate()

        {
            _spawnDuration = Mathf.Max(0f, _spawnDuration);
            _drawDuration = Mathf.Max(0.1f, _drawDuration);
            _burstDuration = Mathf.Max(0f, _burstDuration);
            _influenceRadius = Mathf.Max(0.1f, _influenceRadius);
            _orbitRadius = Mathf.Clamp(_orbitRadius, 0.1f, _influenceRadius);
            _capacity = Mathf.Max(1, _capacity);
            _maxSpeedMultiplier = Mathf.Max(1f, _maxSpeedMultiplier);
            _speedMultiplierPerTurn = Mathf.Max(0f, _speedMultiplierPerTurn);

            if (_trigger != null)
            {
                _trigger.radius = _influenceRadius;
                _trigger.isTrigger = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(0.45f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _influenceRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.25f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, _orbitRadius);
        }
    }
}
