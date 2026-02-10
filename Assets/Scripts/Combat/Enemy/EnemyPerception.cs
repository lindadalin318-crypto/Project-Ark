using System;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Perception system for enemy AI. Provides vision (cone + LoS raycast)
    /// and hearing (subscribes to weapon fire events) with memory decay.
    /// Results are exposed as public properties for HFSM state transition queries.
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────
        [Header("References")]
        [SerializeField] private EnemyStatsSO _stats;

        [Header("Detection")]
        [Tooltip("Layer mask for the player target.")]
        [SerializeField] private LayerMask _playerMask;

        [Tooltip("Layer mask for walls / obstacles that block line of sight.")]
        [SerializeField] private LayerMask _obstacleMask;

        // ──────────────────── Public Properties (read by HFSM states) ────────────────────

        /// <summary> Whether there is a valid target (seen or remembered). </summary>
        public bool HasTarget { get; private set; }

        /// <summary> Last known player position (world space). </summary>
        public Vector2 LastKnownPlayerPosition { get; private set; }

        /// <summary> Whether the player is currently visible this detection tick. </summary>
        public bool CanSeePlayer { get; private set; }

        /// <summary> Whether the enemy recently heard the player fire. </summary>
        public bool HasHeardPlayer { get; private set; }

        /// <summary> Distance from this enemy to the last known player position. </summary>
        public float DistanceToTarget { get; private set; }

        /// <summary> Direct reference to the detected player transform (null if none). </summary>
        public Transform PlayerTransform { get; private set; }

        // ──────────────────── Internal State ────────────────────
        private float _visionCheckTimer;
        private float _memoryTimer;
        private bool _hadTargetLastFrame;

        private const float VISION_CHECK_INTERVAL = 0.2f; // 5 Hz
        private const float ARRIVAL_THRESHOLD = 0.5f;

        // ──────────────────── Lifecycle ────────────────────

        private void OnEnable()
        {
            // Subscribe to weapon fire events for hearing perception
            StarChartController.OnWeaponFired += HandleWeaponFired;
        }

        private void OnDisable()
        {
            StarChartController.OnWeaponFired -= HandleWeaponFired;
        }

        private void Update()
        {
            if (_stats == null) return;

            float dt = Time.deltaTime;

            // --- Vision (throttled) ---
            _visionCheckTimer -= dt;
            if (_visionCheckTimer <= 0f)
            {
                _visionCheckTimer = VISION_CHECK_INTERVAL;
                PerformVisionCheck();
            }

            // --- Memory decay ---
            UpdateMemoryDecay(dt);

            // --- Update derived properties ---
            if (HasTarget)
            {
                DistanceToTarget = Vector2.Distance(
                    (Vector2)transform.position, LastKnownPlayerPosition);
            }
        }

        // ──────────────────── Vision ────────────────────

        /// <summary>
        /// Perform a cone + line-of-sight vision check at the configured frequency.
        /// Steps: 1) Distance check  2) Angle check  3) Raycast LoS check
        /// </summary>
        private void PerformVisionCheck()
        {
            CanSeePlayer = false;

            // Find all player colliders within sight range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, _stats.SightRange, _playerMask);

            if (hits.Length == 0) return;

            // Use the closest player collider
            Collider2D closest = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                float dist = Vector2.Distance(transform.position, hits[i].transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = hits[i];
                }
            }

            if (closest == null) return;

            Vector2 myPos = transform.position;
            Vector2 targetPos = closest.transform.position;
            Vector2 dirToTarget = (targetPos - myPos).normalized;

            // Get facing direction from EnemyEntity if available
            Vector2 facing = Vector2.down; // Default facing
            var entity = GetComponent<EnemyEntity>();
            if (entity != null)
                facing = entity.FacingDirection;

            // Angle check: is the target within the vision cone?
            float angle = Vector2.Angle(facing, dirToTarget);
            if (angle > _stats.SightAngle)
                return;

            // Line-of-sight raycast: check for obstacles between us and the target
            float distToTarget = Vector2.Distance(myPos, targetPos);
            RaycastHit2D losHit = Physics2D.Raycast(myPos, dirToTarget, distToTarget, _obstacleMask);

            if (losHit.collider != null)
                return; // Wall is blocking line of sight

            // All checks passed — we can see the player
            CanSeePlayer = true;
            HasTarget = true;
            LastKnownPlayerPosition = targetPos;
            PlayerTransform = closest.transform;
            _memoryTimer = _stats.MemoryDuration;
            HasHeardPlayer = false; // Visual confirmation supersedes hearing
        }

        // ──────────────────── Hearing ────────────────────

        /// <summary>
        /// Callback for weapon fire events broadcast by StarChartController.
        /// Checks if the sound source is within hearing range.
        /// </summary>
        private void HandleWeaponFired(Vector2 sourcePosition, float noiseRadius)
        {
            if (_stats == null) return;
            if (!isActiveAndEnabled) return;

            float effectiveRange = Mathf.Min(_stats.HearingRange, noiseRadius);
            float dist = Vector2.Distance((Vector2)transform.position, sourcePosition);

            if (dist <= effectiveRange)
            {
                HasHeardPlayer = true;
                HasTarget = true;
                LastKnownPlayerPosition = sourcePosition;
                _memoryTimer = _stats.MemoryDuration;
            }
        }

        // ──────────────────── Memory Decay ────────────────────

        /// <summary>
        /// When the enemy can no longer see the player, count down the memory timer.
        /// When it expires, forget the player entirely.
        /// </summary>
        private void UpdateMemoryDecay(float deltaTime)
        {
            if (!HasTarget) return;

            // If we can currently see the player, keep refreshing the timer
            if (CanSeePlayer)
            {
                _memoryTimer = _stats.MemoryDuration;
                return;
            }

            // Decay memory
            _memoryTimer -= deltaTime;

            if (_memoryTimer <= 0f)
            {
                // Memory expired — forget the player
                HasTarget = false;
                HasHeardPlayer = false;
                CanSeePlayer = false;
                PlayerTransform = null;
                DistanceToTarget = float.MaxValue;
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Force-reset all perception state. Called on pool return or manual reset.
        /// </summary>
        public void ResetPerception()
        {
            HasTarget = false;
            CanSeePlayer = false;
            HasHeardPlayer = false;
            LastKnownPlayerPosition = Vector2.zero;
            DistanceToTarget = float.MaxValue;
            PlayerTransform = null;
            _memoryTimer = 0f;
            _visionCheckTimer = 0f;
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_stats == null) return;

            Vector2 pos = transform.position;
            Vector2 facing = Vector2.down;
            var entity = GetComponent<EnemyEntity>();
            if (entity != null)
                facing = entity.FacingDirection;

            // Draw sight range circle
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(pos, _stats.SightRange);

            // Draw vision cone edges
            Gizmos.color = Color.yellow;
            float halfAngle = _stats.SightAngle;
            Vector2 leftEdge = RotateVector(facing, halfAngle) * _stats.SightRange;
            Vector2 rightEdge = RotateVector(facing, -halfAngle) * _stats.SightRange;
            Gizmos.DrawLine(pos, (Vector3)(pos + leftEdge));
            Gizmos.DrawLine(pos, (Vector3)(pos + rightEdge));

            // Draw hearing range
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(pos, _stats.HearingRange);

            // Draw last known position
            if (HasTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere((Vector3)LastKnownPlayerPosition, 0.2f);
            }
        }

        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
#endif
    }
}
