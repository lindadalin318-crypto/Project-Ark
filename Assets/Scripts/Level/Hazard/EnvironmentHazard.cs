using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Abstract base class for all environment hazards (damage zones, contact hazards, timed traps).
    /// Handles shared configuration: damage, type, knockback, target layer filtering.
    /// Subclasses implement specific trigger/timing behavior.
    /// 
    /// All hazards use the unified DamagePayload → IDamageable.TakeDamage pipeline.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnvironmentHazard : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Damage")]
        [Tooltip("Base damage per hit.")]
        [SerializeField] protected float _damage = 10f;

        [Tooltip("Damage element type.")]
        [SerializeField] protected DamageType _damageType = DamageType.Physical;

        [Tooltip("Knockback force applied on hit.")]
        [SerializeField] protected float _knockbackForce = 5f;

        [Header("Targeting")]
        [Tooltip("Layers this hazard can damage. Set explicitly — no defaults.")]
        [SerializeField] protected LayerMask _targetLayer;

        // ──────────────────── Cached ────────────────────

        protected Collider2D _collider;

        // ──────────────────── Lifecycle ────────────────────

        protected virtual void Awake()
        {
            _collider = GetComponent<Collider2D>();

            if (!_collider.isTrigger)
            {
                _collider.isTrigger = true;
                Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }
        }

        // ──────────────────── Shared Helpers ────────────────────

        /// <summary>
        /// Check if the given GameObject is on the target layer.
        /// </summary>
        protected bool IsValidTarget(GameObject obj)
        {
            return (_targetLayer.value & (1 << obj.layer)) != 0;
        }

        /// <summary>
        /// Apply damage to the target using the unified damage pipeline.
        /// </summary>
        protected void ApplyDamage(GameObject target)
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) return;

            // 计算击退方向：从 hazard 中心指向目标
            Vector2 knockbackDir = (target.transform.position - transform.position).normalized;

            var payload = new DamagePayload(
                baseDamage: _damage,
                type: _damageType,
                knockbackDirection: knockbackDir,
                knockbackForce: _knockbackForce,
                source: gameObject
            );

            damageable.TakeDamage(payload);
        }
    }
}
