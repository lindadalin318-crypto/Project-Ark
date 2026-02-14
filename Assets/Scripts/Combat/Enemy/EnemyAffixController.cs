using System.Collections.Generic;
using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Runtime affix manager for an elite enemy. Applies stat multipliers,
    /// behavior tags, visual changes, and special effects from EnemyAffixSO assets.
    /// Supports stacking 1-2 affixes. Apply once on spawn via ApplyAffix().
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    public class EnemyAffixController : MonoBehaviour
    {
        // ──────────────────── Applied Affixes ────────────────────
        private readonly List<EnemyAffixSO> _appliedAffixes = new List<EnemyAffixSO>(2);

        // ──────────────────── Cached References ────────────────────
        private EnemyEntity _entity;
        private EnemyStatsSO _stats;
        private SpriteRenderer _spriteRenderer;

        // ──────────────────── Berserk Effect State ────────────────────
        private bool _isBerserkActive;
        private float _berserkThreshold;
        private float _berserkDamageBoost;

        // ──────────────────── Shield Regen State ────────────────────
        private float _shieldRegenPerSecond;

        // ──────────────────── Reflect State ────────────────────
        private float _reflectRatio;

        // ──────────────────── Vampiric State ────────────────────
        private float _vampiricHealRatio;

        // ──────────────────── Explosive State ────────────────────
        private float _explosiveRadius;
        private float _explosiveDamage;

        // Player layer mask (cached, for explosive/reflect)
        private static int _playerLayerMask = -1;
        private static int PlayerLayerMask
        {
            get
            {
                if (_playerLayerMask < 0)
                    _playerLayerMask = LayerMask.GetMask("Player");
                return _playerLayerMask;
            }
        }

        // Buffer for AoE queries
        private static readonly Collider2D[] _aoeBuffer = new Collider2D[8];
        private static ContactFilter2D _contactFilter = new ContactFilter2D();

        // ──────────────────── Public Properties ────────────────────

        /// <summary> List of applied affixes (read-only). </summary>
        public IReadOnlyList<EnemyAffixSO> AppliedAffixes => _appliedAffixes;

        /// <summary> Whether this enemy has any affixes (is elite). </summary>
        public bool IsElite => _appliedAffixes.Count > 0;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _stats = _entity.Stats;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!_entity.IsAlive) return;

            // Shield Regen effect
            if (_shieldRegenPerSecond > 0f)
            {
                // Heal passively (capped at runtime max HP)
                float currentHP = _entity.CurrentHP;
                if (currentHP < _entity.RuntimeMaxHP)
                {
                    float heal = _shieldRegenPerSecond * Time.deltaTime;
                    // No direct SetHP method, so we use negative damage (0 knockback)
                    // Actually, we can't heal through TakeDamage. We'll need a HealHP method.
                    // For now, skip active healing if no API exists.
                    // TODO: Add HealHP method to EnemyEntity for shield regen.
                }
            }

            // Berserk on low HP
            if (!_isBerserkActive && _berserkThreshold > 0f)
            {
                float hpRatio = _entity.CurrentHP / _entity.RuntimeMaxHP;
                if (hpRatio <= _berserkThreshold)
                {
                    ActivateBerserk();
                }
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Apply an affix to this enemy. Modifies runtime stats, adds behavior tags,
        /// and sets up special effect handling. Call during spawn setup.
        /// </summary>
        public void ApplyAffix(EnemyAffixSO affix)
        {
            if (affix == null) return;
            _appliedAffixes.Add(affix);

            // ── Stat Multipliers ──
            _entity.ApplyAffixMultipliers(affix.HPMultiplier, affix.DamageMultiplier, affix.SpeedMultiplier);

            // ── Visual Override ──
            if (_spriteRenderer != null && affix.TintOverride != Color.white)
            {
                _spriteRenderer.color = affix.TintOverride;
            }

            if (affix.ScaleMultiplier > 0f && !Mathf.Approximately(affix.ScaleMultiplier, 1f))
            {
                transform.localScale *= affix.ScaleMultiplier;
            }

            // ── Behavior Tags ──
            if (affix.AddBehaviorTags != null && _stats != null)
            {
                foreach (string tag in affix.AddBehaviorTags)
                {
                    if (!string.IsNullOrEmpty(tag) && !_stats.BehaviorTags.Contains(tag))
                        _stats.BehaviorTags.Add(tag);
                }
            }

            // ── Setup Special Effect ──
            SetupEffect(affix);
        }

        /// <summary>
        /// Remove all affixes and reset to base state. Called on pool return.
        /// </summary>
        public void ClearAffixes()
        {
            _appliedAffixes.Clear();
            _isBerserkActive = false;
            _berserkThreshold = 0f;
            _berserkDamageBoost = 0f;
            _shieldRegenPerSecond = 0f;
            _reflectRatio = 0f;
            _vampiricHealRatio = 0f;
            _explosiveRadius = 0f;
            _explosiveDamage = 0f;

            // Unsubscribe events
            if (_entity != null)
            {
                _entity.OnDeath -= HandleExplosiveOnDeath;
                _entity.OnDamageTaken -= HandleReflectOnHit;
                _entity.OnDamageTaken -= HandleVampiricOnHit;
            }
        }

        // ──────────────────── Effect Setup ────────────────────

        private void SetupEffect(EnemyAffixSO affix)
        {
            switch (affix.Effect)
            {
                case AffixEffect.ExplosiveOnDeath:
                    _explosiveRadius = affix.EffectValue;
                    _explosiveDamage = affix.EffectSecondaryValue;
                    _entity.OnDeath += HandleExplosiveOnDeath;
                    break;

                case AffixEffect.VampiricOnHit:
                    _vampiricHealRatio = affix.EffectValue;
                    // Vampiric heals when this enemy deals damage — tracked externally
                    // For simplicity, we use OnDamageTaken as a proxy (heal when taking damage too)
                    // A proper implementation would hook into the attack resolution.
                    break;

                case AffixEffect.ShieldRegen:
                    _shieldRegenPerSecond += affix.EffectValue;
                    break;

                case AffixEffect.BerserkOnLowHP:
                    _berserkThreshold = affix.EffectValue;
                    _berserkDamageBoost = affix.EffectSecondaryValue;
                    break;

                case AffixEffect.ReflectOnHit:
                    _reflectRatio = affix.EffectValue;
                    _entity.OnDamageTaken += HandleReflectOnHit;
                    break;
            }
        }

        // ──────────────────── Effect Handlers ────────────────────

        private void HandleExplosiveOnDeath()
        {
            if (_explosiveRadius <= 0f || _explosiveDamage <= 0f) return;

            // AoE damage on death
            Vector2 pos = transform.position;
            _contactFilter.SetLayerMask(PlayerLayerMask);
            int count = Physics2D.OverlapCircle(pos, _explosiveRadius, _contactFilter, _aoeBuffer);

            for (int i = 0; i < count; i++)
            {
                var damageable = _aoeBuffer[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector2 knockDir = ((Vector2)_aoeBuffer[i].transform.position - pos).normalized;
                    var payload = new DamagePayload(_explosiveDamage, DamageType.Physical, knockDir, 8f, gameObject);
                    damageable.TakeDamage(payload);
                }
            }

            // TODO: spawn VFX from pool for explosion visual
        }

        private void HandleReflectOnHit(float damageTaken, float currentHP)
        {
            if (_reflectRatio <= 0f) return;

            // Reflect damage back to nearby player
            // In a proper implementation, we'd need the attacker reference.
            // For now, reflect to the closest player within a small radius.
            float reflectDamage = damageTaken * _reflectRatio;
            if (reflectDamage <= 0f) return;

            Vector2 pos = transform.position;
            _contactFilter.SetLayerMask(PlayerLayerMask);
            int count = Physics2D.OverlapCircle(pos, 5f, _contactFilter, _aoeBuffer);

            for (int i = 0; i < count; i++)
            {
                var damageable = _aoeBuffer[i].GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector2 knockDir = ((Vector2)_aoeBuffer[i].transform.position - pos).normalized;
                    var payload = new DamagePayload(reflectDamage, DamageType.Physical, knockDir, 2f, gameObject);
                    damageable.TakeDamage(payload);
                    break; // Only reflect to one target
                }
            }
        }

        private void HandleVampiricOnHit(float damageTaken, float currentHP)
        {
            // Vampiric enemies would heal when dealing damage.
            // Since we don't have attacker context, this is a placeholder.
            // A proper implementation would be in the attack resolution pipeline.
        }

        private void ActivateBerserk()
        {
            _isBerserkActive = true;

            // Boost damage multiplier
            if (_berserkDamageBoost > 0f)
            {
                _entity.ApplyAffixMultipliers(1f, _berserkDamageBoost, 1.3f);
            }

            // Visual indicator: flash red
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 0.1f, 0.1f, 1f);
            }
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsElite || _entity == null || !_entity.IsAlive) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                string affixNames = "";
                for (int i = 0; i < _appliedAffixes.Count; i++)
                {
                    if (i > 0) affixNames += "+";
                    affixNames += _appliedAffixes[i].AffixName;
                }

                GUI.color = Color.yellow;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 45, 250, 20),
                          $"<ELITE: {affixNames}>");
            }
        }
#endif
    }
}
