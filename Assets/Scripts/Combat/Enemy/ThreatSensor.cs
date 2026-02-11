using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Threat sensor component for detecting incoming player projectiles.
    /// Scans at 5 Hz (same as vision) for projectiles heading toward this enemy.
    /// Used by DodgeState and BlockState — enemies with "CanDodge" or "CanBlock"
    /// behavior tags check this sensor to trigger evasive actions.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    public class ThreatSensor : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────
        [Header("Threat Detection")]
        [Tooltip("Layer mask for player projectiles to detect as threats.")]
        [SerializeField] private LayerMask _projectileMask;

        // ──────────────────── Runtime State ────────────────────
        private EnemyEntity _entity;
        private EnemyStatsSO _stats;
        private float _scanTimer;

        // Scan frequency (matches vision check)
        private const float SCAN_INTERVAL = 0.2f;

        // Dot product threshold: projectile must be heading roughly toward us
        // (dot > 0.3 means within ~72° cone heading our way)
        private const float HEADING_THRESHOLD = 0.3f;

        // NonAlloc buffer for projectile scan
        private static readonly Collider2D[] _scanBuffer = new Collider2D[8];

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Whether an incoming threat is currently detected. </summary>
        public bool IsThreatDetected { get; private set; }

        /// <summary> Direction the threat is coming FROM (normalized). </summary>
        public Vector2 ThreatDirection { get; private set; }

        /// <summary> World position of the closest detected threat. </summary>
        public Vector2 ThreatPosition { get; private set; }

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _stats = _entity.Stats;
        }

        private void Update()
        {
            if (!_entity.IsAlive || _stats == null) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = SCAN_INTERVAL;
                PerformThreatScan();
            }
        }

        // ──────────────────── Scan Logic ────────────────────

        private void PerformThreatScan()
        {
            IsThreatDetected = false;
            ThreatDirection = Vector2.zero;

            float radius = _stats.ThreatDetectionRadius;
            Vector2 myPos = transform.position;

            int count = Physics2D.OverlapCircleNonAlloc(
                myPos, radius, _scanBuffer, _projectileMask);

            if (count == 0) return;

            float closestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var rb = _scanBuffer[i].attachedRigidbody;
                if (rb == null) continue;

                Vector2 projPos = _scanBuffer[i].transform.position;
                Vector2 projVel = rb.linearVelocity;

                if (projVel.sqrMagnitude < 0.1f) continue; // Stationary, not a threat

                // Check if projectile is heading toward us
                Vector2 toMe = (myPos - projPos).normalized;
                float dot = Vector2.Dot(projVel.normalized, toMe);

                if (dot > HEADING_THRESHOLD)
                {
                    float dist = Vector2.Distance(myPos, projPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        IsThreatDetected = true;
                        ThreatDirection = -toMe; // Direction threat is coming FROM
                        ThreatPosition = projPos;
                    }
                }
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Reset sensor state. Called on pool return.
        /// </summary>
        public void ResetSensor()
        {
            IsThreatDetected = false;
            ThreatDirection = Vector2.zero;
            ThreatPosition = Vector2.zero;
            _scanTimer = 0f;
        }
    }
}
