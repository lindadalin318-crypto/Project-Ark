using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Instant-contact hazard (e.g., laser fence, electric arc, spike trap).
    /// Deals damage on first contact, then enters a per-target cooldown period.
    /// Does not deal continuous damage while staying inside (use DamageZone for that).
    /// </summary>
    public class ContactHazard : EnvironmentHazard
    {
        [Header("Contact Settings")]
        [Tooltip("Cooldown (seconds) before this hazard can damage the same target again.")]
        [SerializeField] private float _hitCooldown = 1.0f;

        // 追踪每个目标的冷却到期时间
        private readonly Dictionary<int, float> _cooldownExpiry = new();

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        /// <summary>
        /// Also check on stay for targets that were in cooldown when they entered.
        /// This handles the case where a target enters during cooldown and stays.
        /// </summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (!IsValidTarget(other.gameObject)) return;

            int id = other.gameObject.GetInstanceID();
            float now = Time.time;

            // 检查冷却
            if (_cooldownExpiry.TryGetValue(id, out float expiry) && now < expiry)
                return;

            ApplyDamage(other.gameObject);
            _cooldownExpiry[id] = now + _hitCooldown;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidTarget(other.gameObject)) return;
            _cooldownExpiry.Remove(other.gameObject.GetInstanceID());
        }

        private void OnDisable()
        {
            _cooldownExpiry.Clear();
        }
    }
}
