using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Data-driven configuration for a single enemy type.
    /// All numerical values live here — no hardcoded constants in MonoBehaviours.
    /// Create via: Create > ProjectArk > Enemy > EnemyStats
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyStats_New", menuName = "ProjectArk/Enemy/EnemyStats", order = 0)]
    public class EnemyStatsSO : ScriptableObject
    {
        // ──────────────────── Identity ────────────────────
        [Header("Identity")]
        [Tooltip("Display name for debug and UI purposes.")]
        public string EnemyName = "Unnamed";

        [Tooltip("Unique identifier string for this enemy type.")]
        public string EnemyID = "enemy_default";

        // ──────────────────── Health ────────────────────
        [Header("Health")]
        [Tooltip("Maximum hit points.")]
        [Min(1f)]
        public float MaxHP = 100f;

        [Tooltip("Maximum poise (stagger threshold). When broken, enemy is staggered.")]
        [Min(0f)]
        public float MaxPoise = 50f;

        // ──────────────────── Movement ────────────────────
        [Header("Movement")]
        [Tooltip("Movement speed in units per second.")]
        [Min(0f)]
        public float MoveSpeed = 3f;

        [Tooltip("Rotation speed in degrees per second (for facing direction).")]
        [Min(0f)]
        public float RotationSpeed = 360f;

        // ──────────────────── Attack ────────────────────
        [Header("Attack")]
        [Tooltip("Damage dealt per attack hit.")]
        [Min(0f)]
        public float AttackDamage = 10f;

        [Tooltip("Range at which the enemy initiates an attack.")]
        [Min(0.1f)]
        public float AttackRange = 1.5f;

        [Tooltip("Cooldown between attack sequences (seconds).")]
        [Min(0f)]
        public float AttackCooldown = 1f;

        [Tooltip("Knockback force applied to the player on hit.")]
        [Min(0f)]
        public float AttackKnockback = 5f;

        // ──────────────────── Attack Phases (Signal-Window Model) ────────────────────
        [Header("Attack Phases")]
        [Tooltip("Duration of the telegraph (wind-up) phase in seconds. Player reads this signal.")]
        [Min(0.05f)]
        public float TelegraphDuration = 0.4f;

        [Tooltip("Duration of the active hitbox phase in seconds. Commitment window — no turning.")]
        [Min(0.05f)]
        public float AttackActiveDuration = 0.2f;

        [Tooltip("Duration of the recovery (cool-down) phase in seconds. Player punish window.")]
        [Min(0.05f)]
        public float RecoveryDuration = 0.6f;

        // ──────────────────── Perception ────────────────────
        [Header("Perception")]
        [Tooltip("Maximum distance for visual detection.")]
        [Min(0f)]
        public float SightRange = 10f;

        [Tooltip("Half-angle of the vision cone in degrees (e.g. 60 = 120° total cone).")]
        [Range(0f, 180f)]
        public float SightAngle = 60f;

        [Tooltip("Maximum distance for hearing weapon fire events.")]
        [Min(0f)]
        public float HearingRange = 15f;

        // ──────────────────── Leash & Memory ────────────────────
        [Header("Leash & Memory")]
        [Tooltip("If target moves beyond this distance, enemy gives up chase and returns.")]
        [Min(1f)]
        public float LeashRange = 20f;

        [Tooltip("Seconds after losing sight before the enemy forgets the player's last known position.")]
        [Min(0f)]
        public float MemoryDuration = 3f;

        // ──────────────────── Visuals & Feedback ────────────────────
        [Header("Visuals & Feedback")]
        [Tooltip("Duration of the white-flash hit feedback in seconds.")]
        [Min(0f)]
        public float HitFlashDuration = 0.1f;

        [Tooltip("Default sprite color (restored after hit flash).")]
        public Color BaseColor = Color.white;
    }
}
