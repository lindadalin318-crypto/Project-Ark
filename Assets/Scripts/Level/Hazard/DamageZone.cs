using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Persistent area-of-effect hazard (e.g., acid pool, radiation zone).
    /// Deals damage every _tickInterval seconds while the target remains in the zone.
    /// Uses OnTriggerStay2D with a timer to avoid per-frame damage.
    /// </summary>
    public class DamageZone : EnvironmentHazard
    {
        [Header("Zone Settings")]
        [Tooltip("Time interval (seconds) between damage ticks.")]
        [SerializeField] private float _tickInterval = 0.5f;

        // 追踪每个目标的下次伤害时间
        private readonly Dictionary<int, float> _nextDamageTime = new();

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsValidTarget(other.gameObject)) return;

            int id = other.gameObject.GetInstanceID();
            float now = Time.time;

            // 如果是第一次进入或已到达下次伤害时间
            if (!_nextDamageTime.TryGetValue(id, out float nextTime) || now >= nextTime)
            {
                ApplyDamage(other.gameObject);
                _nextDamageTime[id] = now + _tickInterval;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidTarget(other.gameObject)) return;
            _nextDamageTime.Remove(other.gameObject.GetInstanceID());
        }

        /// <summary>
        /// Clean up stale entries when the hazard is disabled (e.g., room deactivation).
        /// </summary>
        private void OnDisable()
        {
            _nextDamageTime.Clear();
        }
    }
}
