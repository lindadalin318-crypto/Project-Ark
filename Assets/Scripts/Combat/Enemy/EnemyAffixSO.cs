using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Special effect behavior that an affix can apply.
    /// </summary>
    public enum AffixEffect
    {
        /// <summary> No special effect, just stat changes. </summary>
        None,
        /// <summary> AoE damage explosion on death. </summary>
        ExplosiveOnDeath,
        /// <summary> Heal a percentage of damage dealt. </summary>
        VampiricOnHit,
        /// <summary> Passive shield regen over time. </summary>
        ShieldRegen,
        /// <summary> Stat boost when HP drops below threshold. </summary>
        BerserkOnLowHP,
        /// <summary> Reflect a percentage of damage taken back to attacker. </summary>
        ReflectOnHit
    }

    /// <summary>
    /// Data-driven affix definition for creating elite enemies.
    /// An affix modifies runtime stats, adds behavior tags, and optionally
    /// applies a special effect (explosive death, vampiric, etc.).
    /// One enemy can have 1-2 affixes stacked.
    /// Create via: Create > ProjectArk > Enemy > EnemyAffix
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAffix_New", menuName = "ProjectArk/Enemy/EnemyAffix", order = 2)]
    public class EnemyAffixSO : ScriptableObject
    {
        // ──────────────────── Identity ────────────────────
        [Header("Identity")]
        [Tooltip("Display name for the affix (e.g. 'Berserk', 'Shielded').")]
        public string AffixName = "New Affix";

        // ──────────────────── Stat Multipliers ────────────────────
        [Header("Stat Multipliers")]
        [Tooltip("HP multiplier. 1.0 = no change, 2.0 = double HP.")]
        [Min(0.1f)]
        public float HPMultiplier = 1f;

        [Tooltip("Damage multiplier for all attacks.")]
        [Min(0.1f)]
        public float DamageMultiplier = 1f;

        [Tooltip("Speed multiplier for movement.")]
        [Min(0.1f)]
        public float SpeedMultiplier = 1f;

        // ──────────────────── Visuals ────────────────────
        [Header("Visuals")]
        [Tooltip("Tint color override for the elite enemy sprite.")]
        public Color TintOverride = Color.white;

        [Tooltip("Scale multiplier for visual emphasis (1.0 = normal size).")]
        [Min(0.5f)]
        public float ScaleMultiplier = 1f;

        // ──────────────────── Behavior ────────────────────
        [Header("Behavior")]
        [Tooltip("Additional behavior tags to add at runtime (e.g. 'SuperArmor', 'CanBlock').")]
        public string[] AddBehaviorTags;

        [Tooltip("Special effect applied by this affix.")]
        public AffixEffect Effect = AffixEffect.None;

        // ──────────────────── Effect Parameters ────────────────────
        [Header("Effect Parameters")]
        [Tooltip("ExplosiveOnDeath: AoE radius. VampiricOnHit: heal % (0.2 = 20%). ShieldRegen: HP/sec. BerserkOnLowHP: HP threshold ratio (0.3 = 30%). ReflectOnHit: reflect ratio.")]
        [Min(0f)]
        public float EffectValue = 0f;

        [Tooltip("ExplosiveOnDeath: damage dealt. BerserkOnLowHP: damage multiplier boost.")]
        [Min(0f)]
        public float EffectSecondaryValue = 0f;
    }
}
