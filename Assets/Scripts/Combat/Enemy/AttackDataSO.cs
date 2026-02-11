using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Type of attack execution.
    /// </summary>
    public enum AttackType
    {
        /// <summary> Area-of-effect melee hit using physics overlap queries. </summary>
        Melee,
        /// <summary> Fires one or more projectiles from the enemy projectile pool. </summary>
        Projectile,
        /// <summary> Instant-hit laser beam using raycast + LineRenderer. </summary>
        Laser
    }

    /// <summary>
    /// Shape of the melee hitbox used by <see cref="HitboxResolver"/>.
    /// </summary>
    public enum HitboxShape
    {
        /// <summary> Radial OverlapCircle centered at offset from enemy. </summary>
        Circle,
        /// <summary> Rectangular OverlapBox oriented along facing direction. </summary>
        Box,
        /// <summary> OverlapCircle filtered by angular cone relative to facing direction. </summary>
        Cone
    }

    /// <summary>
    /// Data-driven definition of a single attack pattern.
    /// One enemy can reference multiple AttackDataSO assets for varied behavior.
    /// Create via: Create > ProjectArk > Enemy > AttackData
    /// </summary>
    [CreateAssetMenu(fileName = "AttackData_New", menuName = "ProjectArk/Enemy/AttackData", order = 1)]
    public class AttackDataSO : ScriptableObject
    {
        // ──────────────────── Identity ────────────────────
        [Header("Identity")]
        [Tooltip("Human-readable name for editor / debug display.")]
        public string AttackName = "New Attack";

        [Tooltip("Type of attack execution: Melee (overlap), Projectile (spawn), Laser (raycast).")]
        public AttackType Type = AttackType.Melee;

        // ──────────────────── Phases (Signal-Window Model) ────────────────────
        [Header("Phases (Signal-Window)")]
        [Tooltip("Duration of the telegraph (wind-up) phase in seconds.")]
        [Min(0.05f)]
        public float TelegraphDuration = 0.4f;

        [Tooltip("Duration of the active damage window in seconds.")]
        [Min(0.05f)]
        public float ActiveDuration = 0.2f;

        [Tooltip("Duration of the recovery (punish) phase in seconds.")]
        [Min(0.05f)]
        public float RecoveryDuration = 0.6f;

        // ──────────────────── Damage ────────────────────
        [Header("Damage")]
        [Tooltip("Base damage per hit.")]
        [Min(0f)]
        public float Damage = 10f;

        [Tooltip("Knockback force applied to the target on hit.")]
        [Min(0f)]
        public float Knockback = 5f;

        // ──────────────────── Hitbox (Melee Type) ────────────────────
        [Header("Hitbox (Melee Only)")]
        [Tooltip("Shape of the melee overlap query.")]
        public HitboxShape Shape = HitboxShape.Circle;

        [Tooltip("Circle: radius. Box: half-width. Cone: detection radius.")]
        [Min(0.1f)]
        public float HitboxRadius = 1.5f;

        [Tooltip("Box: half-length along facing direction. Cone: unused.")]
        [Min(0.1f)]
        public float HitboxLength = 1f;

        [Tooltip("Cone: half-angle in degrees (e.g. 45 = 90° total arc).")]
        [Range(5f, 180f)]
        public float HitboxAngle = 45f;

        [Tooltip("Forward offset from enemy position along facing direction.")]
        [Min(0f)]
        public float HitboxOffset = 0.5f;

        // ──────────────────── Ranged (Projectile Type) ────────────────────
        [Header("Ranged (Projectile Only)")]
        [Tooltip("Projectile prefab to spawn from the enemy object pool.")]
        public GameObject ProjectilePrefab;

        [Tooltip("Projectile travel speed in units/second.")]
        [Min(1f)]
        public float ProjectileSpeed = 8f;

        [Tooltip("Knockback applied by the projectile (overrides Knockback above for projectile hits).")]
        [Min(0f)]
        public float ProjectileKnockback = 3f;

        [Tooltip("Projectile auto-despawn lifetime in seconds.")]
        [Min(0.5f)]
        public float ProjectileLifetime = 4f;

        [Tooltip("Number of shots in a single burst.")]
        [Min(1)]
        public int ShotsPerBurst = 1;

        [Tooltip("Time between each shot in a burst (seconds).")]
        [Min(0.01f)]
        public float BurstInterval = 0.25f;

        // ──────────────────── Laser Type ────────────────────
        [Header("Laser Only")]
        [Tooltip("Prefab with EnemyLaserBeam component. Leave null for non-laser attacks.")]
        public GameObject LaserPrefab;

        [Tooltip("Maximum laser range in units.")]
        [Min(1f)]
        public float LaserRange = 15f;

        [Tooltip("How long the beam stays active (seconds).")]
        [Min(0.05f)]
        public float LaserDuration = 1f;

        [Tooltip("Visual width of the laser beam.")]
        [Min(0.01f)]
        public float LaserWidth = 0.3f;

        // ──────────────────── Visual ────────────────────
        [Header("Visuals")]
        [Tooltip("Sprite tint during the telegraph phase.")]
        public Color TelegraphColor = Color.red;

        // ──────────────────── Selection & Cooldown ────────────────────
        [Header("Selection")]
        [Tooltip("Relative weight for weighted-random selection when enemy has multiple attacks.")]
        [Min(0.01f)]
        public float SelectionWeight = 1f;

        [Tooltip("Per-attack cooldown in seconds. 0 = no individual cooldown (uses global AttackCooldown).")]
        [Min(0f)]
        public float Cooldown = 0f;
    }
}
