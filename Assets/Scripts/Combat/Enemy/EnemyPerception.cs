using System;
using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Target type detected by the perception system.
    /// </summary>
    public enum TargetType
    {
        /// <summary> No target detected. </summary>
        None,
        /// <summary> Player ship is the target. </summary>
        Player,
        /// <summary> An enemy from a hostile faction is the target. </summary>
        FactionEnemy
    }

    /// <summary>
    /// Perception system for enemy AI. Provides vision (cone + LoS raycast),
    /// hearing (subscribes to weapon fire events), faction scanning, and memory decay.
    /// Results are exposed as public properties for HFSM state transition queries.
    /// Faction system: enemies can detect and target hostile faction entities.
    /// Player always takes priority over faction enemies.
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

        /// <summary> Last known target position (world space). Generalized for player or faction enemy. </summary>
        public Vector2 LastKnownTargetPosition { get; private set; }

        /// <summary> Backward-compatible alias for LastKnownTargetPosition. </summary>
        public Vector2 LastKnownPlayerPosition => LastKnownTargetPosition;

        /// <summary> Whether the player is currently visible this detection tick. </summary>
        public bool CanSeePlayer { get; private set; }

        /// <summary> Whether the enemy recently heard the player fire. </summary>
        public bool HasHeardPlayer { get; private set; }

        /// <summary> Distance from this enemy to the last known target position. </summary>
        public float DistanceToTarget { get; private set; }

        /// <summary> Direct reference to the detected player transform (null if targeting faction enemy). </summary>
        public Transform PlayerTransform { get; private set; }

        /// <summary> Type of the current target. </summary>
        public TargetType CurrentTargetType { get; private set; }

        /// <summary> Reference to the targeted EnemyEntity (non-null only when targeting a faction enemy). </summary>
        public EnemyEntity CurrentTargetEntity { get; private set; }

        // ──────────────────── Internal State ────────────────────
        private float _visionCheckTimer;
        private float _memoryTimer;

        private const float VISION_CHECK_INTERVAL = 0.2f; // 5 Hz
        private const float ARRIVAL_THRESHOLD = 0.5f;

        // NonAlloc buffers for faction scanning
        private static readonly Collider2D[] _factionBuffer = new Collider2D[16];

        // Cached enemy layer mask
        private static int _enemyLayerMask = -1;
        private static int EnemyLayerMask
        {
            get
            {
                if (_enemyLayerMask < 0)
                    _enemyLayerMask = LayerMask.GetMask("Enemy");
                return _enemyLayerMask;
            }
        }

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

            // --- Vision + Faction scan (throttled) ---
            _visionCheckTimer -= dt;
            if (_visionCheckTimer <= 0f)
            {
                _visionCheckTimer = VISION_CHECK_INTERVAL;
                PerformVisionCheck();

                // Faction scan only if no player target (player takes priority)
                if (!CanSeePlayer)
                    PerformFactionScan();
            }

            // --- Memory decay ---
            UpdateMemoryDecay(dt);

            // --- Update distance ---
            if (HasTarget)
            {
                // If targeting a faction enemy, update position from live transform
                if (CurrentTargetType == TargetType.FactionEnemy && CurrentTargetEntity != null)
                {
                    if (CurrentTargetEntity.IsAlive)
                    {
                        LastKnownTargetPosition = CurrentTargetEntity.transform.position;
                    }
                    else
                    {
                        // Target died — clear it
                        ClearFactionTarget();
                    }
                }

                DistanceToTarget = Vector2.Distance(
                    (Vector2)transform.position, LastKnownTargetPosition);
            }
        }

        // ──────────────────── Vision (Player) ────────────────────

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
            LastKnownTargetPosition = targetPos;
            PlayerTransform = closest.transform;
            CurrentTargetType = TargetType.Player;
            CurrentTargetEntity = null; // Player, not a faction enemy
            _memoryTimer = _stats.MemoryDuration;
            HasHeardPlayer = false; // Visual confirmation supersedes hearing
        }

        // ──────────────────── Faction Scan ────────────────────

        /// <summary>
        /// Scan for hostile faction enemies within sight range.
        /// Only runs when no player target is visible (player always takes priority).
        /// Uses NonAlloc for zero GC allocation.
        /// </summary>
        private void PerformFactionScan()
        {
            if (string.IsNullOrEmpty(_stats.FactionID)) return;

            Vector2 myPos = transform.position;
            int count = Physics2D.OverlapCircleNonAlloc(myPos, _stats.SightRange, _factionBuffer, EnemyLayerMask);

            if (count == 0) return;

            EnemyEntity bestTarget = null;
            float bestDist = float.MaxValue;

            var myEntity = GetComponent<EnemyEntity>();

            for (int i = 0; i < count; i++)
            {
                if (_factionBuffer[i].gameObject == gameObject) continue; // Skip self

                var otherEntity = _factionBuffer[i].GetComponent<EnemyEntity>();
                if (otherEntity == null || !otherEntity.IsAlive) continue;

                // Check faction — hostile if different faction
                if (otherEntity.Stats == null) continue;
                if (otherEntity.Stats.FactionID == _stats.FactionID) continue; // Same faction = ally

                // Distance check
                float dist = Vector2.Distance(myPos, _factionBuffer[i].transform.position);

                // LoS check (optional for faction — can see through minor obstacles)
                Vector2 dirToTarget = ((Vector2)_factionBuffer[i].transform.position - myPos).normalized;
                RaycastHit2D losHit = Physics2D.Raycast(myPos, dirToTarget, dist, _obstacleMask);
                if (losHit.collider != null) continue; // Can't see through walls

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = otherEntity;
                }
            }

            if (bestTarget != null)
            {
                HasTarget = true;
                LastKnownTargetPosition = bestTarget.transform.position;
                CurrentTargetType = TargetType.FactionEnemy;
                CurrentTargetEntity = bestTarget;
                PlayerTransform = null;
                _memoryTimer = _stats.MemoryDuration;
            }
        }

        /// <summary>
        /// Clear faction target when the enemy dies or is invalid.
        /// Falls back to no-target state unless player or memory still active.
        /// </summary>
        private void ClearFactionTarget()
        {
            if (CurrentTargetType == TargetType.FactionEnemy)
            {
                CurrentTargetEntity = null;
                CurrentTargetType = TargetType.None;
                // Don't clear HasTarget immediately — let memory decay handle it
            }
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
                LastKnownTargetPosition = sourcePosition;
                CurrentTargetType = TargetType.Player;
                CurrentTargetEntity = null;
                _memoryTimer = _stats.MemoryDuration;
            }
        }

        // ──────────────────── Memory Decay ────────────────────

        /// <summary>
        /// When the enemy can no longer see the player, count down the memory timer.
        /// When it expires, forget the target entirely.
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

            // If tracking a live faction enemy, keep refreshing
            if (CurrentTargetType == TargetType.FactionEnemy &&
                CurrentTargetEntity != null && CurrentTargetEntity.IsAlive)
            {
                _memoryTimer = _stats.MemoryDuration;
                return;
            }

            // Decay memory
            _memoryTimer -= deltaTime;

            if (_memoryTimer <= 0f)
            {
                // Memory expired — forget everything
                HasTarget = false;
                HasHeardPlayer = false;
                CanSeePlayer = false;
                PlayerTransform = null;
                CurrentTargetType = TargetType.None;
                CurrentTargetEntity = null;
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
            LastKnownTargetPosition = Vector2.zero;
            DistanceToTarget = float.MaxValue;
            PlayerTransform = null;
            CurrentTargetType = TargetType.None;
            CurrentTargetEntity = null;
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

            // Draw last known target position
            if (HasTarget)
            {
                Gizmos.color = CurrentTargetType == TargetType.FactionEnemy
                    ? Color.magenta
                    : Color.red;
                Gizmos.DrawSphere((Vector3)LastKnownTargetPosition, 0.2f);
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
