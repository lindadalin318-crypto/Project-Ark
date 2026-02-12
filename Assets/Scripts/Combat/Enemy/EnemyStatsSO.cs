using System.Collections.Generic;
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

        // ──────────────────── Faction ────────────────────
        [Header("Faction")]
        [Tooltip("Faction identifier. Enemies of different factions are hostile to each other. Player is hostile to all.")]
        public string FactionID = "Neutral";

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

        // ──────────────────── Ranged Attack (Shooter Type) ────────────────────
        [Header("Ranged Attack (Shooter Only)")]
        [Tooltip("Prefab for the enemy projectile. Leave null for melee-only enemies.")]
        public GameObject ProjectilePrefab;

        [Tooltip("Projectile travel speed in units/second.")]
        [Min(1f)]
        public float ProjectileSpeed = 8f;

        [Tooltip("Damage dealt per projectile hit.")]
        [Min(0f)]
        public float ProjectileDamage = 8f;

        [Tooltip("Knockback force applied by each projectile.")]
        [Min(0f)]
        public float ProjectileKnockback = 3f;

        [Tooltip("Projectile lifetime in seconds before auto-despawn.")]
        [Min(0.5f)]
        public float ProjectileLifetime = 4f;

        [Tooltip("Number of shots fired in a single burst.")]
        [Min(1)]
        public int ShotsPerBurst = 3;

        [Tooltip("Time interval between each shot in a burst (seconds).")]
        [Min(0.05f)]
        public float BurstInterval = 0.25f;

        [Tooltip("Ideal distance the shooter tries to maintain from the player.")]
        [Min(1f)]
        public float PreferredRange = 10f;

        [Tooltip("If player gets closer than this, shooter retreats.")]
        [Min(0.5f)]
        public float RetreatRange = 5f;

        // ──────────────────── Poise & Stagger ────────────────────
        [Header("Poise & Stagger")]
        [Tooltip("Duration of the stagger (硬直) state in seconds when poise is broken.")]
        [Min(0.1f)]
        public float StaggerDuration = 1.0f;

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

        // ──────────────────── Resistances ────────────────────
        [Header("Resistances")]
        [Tooltip("Physical damage resistance. 0 = no resistance, 1 = full immunity.")]
        [Range(0f, 1f)]
        public float Resist_Physical = 0f;

        [Tooltip("Fire damage resistance. 0 = no resistance, 1 = full immunity.")]
        [Range(0f, 1f)]
        public float Resist_Fire = 0f;

        [Tooltip("Ice damage resistance. 0 = no resistance, 1 = full immunity.")]
        [Range(0f, 1f)]
        public float Resist_Ice = 0f;

        [Tooltip("Lightning damage resistance. 0 = no resistance, 1 = full immunity.")]
        [Range(0f, 1f)]
        public float Resist_Lightning = 0f;

        [Tooltip("Void damage resistance. 0 = no resistance, 1 = full immunity.")]
        [Range(0f, 1f)]
        public float Resist_Void = 0f;

        // ──────────────────── Rewards & Drops ────────────────────
        [Header("Rewards & Drops")]
        [Tooltip("Reference ID for the drop table associated with this enemy.")]
        public string DropTableID = "";

        // ──────────────────── Spawn & Metadata ────────────────────
        [Header("Spawn & Metadata")]
        [Tooltip("Planet where this enemy first appears (P1/P2/P3…/Global). Used for filtering and debug.")]
        public string PlanetID = "";

        [Tooltip("Relative spawn weight for random encounters. Higher = more frequent.")]
        [Min(0f)]
        public float SpawnWeight = 1f;

        // ──────────────────── Attacks (Data-Driven) ────────────────────
        [Header("Attacks (Data-Driven)")]
        [Tooltip("Array of attack patterns this enemy can execute. If empty, legacy flat fields above are used.")]
        public AttackDataSO[] Attacks;

        /// <summary>
        /// Whether this enemy has data-driven attacks configured.
        /// When false, states should fall back to legacy flat fields.
        /// </summary>
        public bool HasAttackData => Attacks != null && Attacks.Length > 0;

        /// <summary>
        /// Select a random attack from the Attacks array using weighted selection.
        /// Returns null if no attacks are configured.
        /// </summary>
        public AttackDataSO SelectRandomAttack()
        {
            if (!HasAttackData) return null;
            if (Attacks.Length == 1) return Attacks[0];

            float totalWeight = 0f;
            for (int i = 0; i < Attacks.Length; i++)
            {
                if (Attacks[i] != null)
                    totalWeight += Attacks[i].SelectionWeight;
            }

            if (totalWeight <= 0f) return Attacks[0];

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < Attacks.Length; i++)
            {
                if (Attacks[i] == null) continue;
                cumulative += Attacks[i].SelectionWeight;
                if (roll <= cumulative) return Attacks[i];
            }

            return Attacks[Attacks.Length - 1];
        }

        // ──────────────────── Fear System ────────────────────
        [Header("Fear System")]
        [Tooltip("Fear value required to trigger fleeing. 0 = never flees.")]
        [Min(0f)]
        public float FearThreshold = 50f;

        [Tooltip("Fear added when a nearby ally dies.")]
        [Min(0f)]
        public float FearFromAllyDeath = 10f;

        [Tooltip("Fear added when this enemy's poise is broken.")]
        [Min(0f)]
        public float FearFromPoiseBroken = 20f;

        [Tooltip("Fear decay rate per second (passive calming).")]
        [Min(0f)]
        public float FearDecayRate = 5f;

        [Tooltip("Maximum duration of the flee state in seconds.")]
        [Min(0.5f)]
        public float FleeDuration = 4f;

        // ──────────────────── Dodge & Block ────────────────────
        [Header("Dodge & Block")]
        [Tooltip("Movement speed during dodge dash.")]
        [Min(1f)]
        public float DodgeSpeed = 8f;

        [Tooltip("Duration of the dodge dash in seconds.")]
        [Min(0.05f)]
        public float DodgeDuration = 0.3f;

        [Tooltip("Damage reduction multiplier when blocking (0.7 = take 30% damage).")]
        [Range(0f, 1f)]
        public float BlockDamageReduction = 0.7f;

        [Tooltip("Maximum duration of the block stance in seconds.")]
        [Min(0.1f)]
        public float BlockDuration = 1.5f;

        [Tooltip("Detection radius for incoming projectile threats.")]
        [Min(1f)]
        public float ThreatDetectionRadius = 5f;

        // ──────────────────── State Transitions (Data-Driven Override) ────────────────────
        [Header("State Transitions (Optional Override)")]
        [Tooltip("Data-driven transition rules. When non-empty, states check these FIRST before falling back to hardcoded logic. Enables per-enemy-type behavior without new state classes.")]
        [SerializeField] private StateTransitionRule[] _transitionOverrides;

        /// <summary> Data-driven transition overrides. Empty array = use hardcoded defaults. </summary>
        public StateTransitionRule[] TransitionOverrides => _transitionOverrides ?? System.Array.Empty<StateTransitionRule>();

        // ──────────────────── Behavior Tags ────────────────────
        [Header("Behavior Tags")]
        [Tooltip("Special behavior flags (e.g. SuperArmor, SelfDestruct, Invisible, Reflective). Queried by state machine at runtime.")]
        public List<string> BehaviorTags = new List<string>();
    }
}
