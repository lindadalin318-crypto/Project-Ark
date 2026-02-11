using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Fear system component. Accumulates fear from events (ally death, poise break)
    /// and triggers fleeing when threshold is crossed.
    /// Attach alongside EnemyEntity and EnemyBrain.
    /// Fear decays passively over time; enemies calm down eventually.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyFear : MonoBehaviour
    {
        // ──────────────────── Runtime State ────────────────────
        private float _fearValue;
        private EnemyEntity _entity;
        private EnemyBrain _brain;
        private EnemyStatsSO _stats;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Current fear value. </summary>
        public float FearValue => _fearValue;

        /// <summary> Whether this enemy is currently fleeing due to fear. </summary>
        public bool IsFleeing { get; set; }

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _brain = GetComponent<EnemyBrain>();
            _stats = _entity.Stats;
        }

        private void OnEnable()
        {
            // Subscribe to global death event (fear from ally deaths)
            EnemyEntity.OnAnyEnemyDeath += HandleAnyEnemyDeath;

            // Subscribe to own poise break (fear from being staggered)
            _entity.OnPoiseBroken += HandlePoiseBroken;
        }

        private void OnDisable()
        {
            EnemyEntity.OnAnyEnemyDeath -= HandleAnyEnemyDeath;

            if (_entity != null)
                _entity.OnPoiseBroken -= HandlePoiseBroken;
        }

        private void Update()
        {
            if (_stats == null || !_entity.IsAlive) return;

            // Passive fear decay
            if (_fearValue > 0f)
            {
                _fearValue = Mathf.Max(0f, _fearValue - _stats.FearDecayRate * Time.deltaTime);
            }

            // Check fear threshold (only trigger once per fear spike)
            if (!IsFleeing && _fearValue >= _stats.FearThreshold && _stats.FearThreshold > 0f)
            {
                TriggerFlee();
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Add fear from an external source. Used by game systems to inject fear.
        /// </summary>
        public void AddFear(float amount)
        {
            if (!_entity.IsAlive || amount <= 0f) return;
            _fearValue += amount;
        }

        /// <summary>
        /// Reset fear to zero. Called on pool return or when flee ends.
        /// </summary>
        public void ResetFear()
        {
            _fearValue = 0f;
            IsFleeing = false;
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleAnyEnemyDeath(Vector2 deathPosition, EnemyStatsSO deadEnemyStats)
        {
            if (_stats == null || !_entity.IsAlive) return;

            // Don't fear our own death
            if (!isActiveAndEnabled) return;

            // Check distance — fear propagates within hearing range
            float dist = Vector2.Distance((Vector2)transform.position, deathPosition);
            if (dist <= _stats.HearingRange)
            {
                AddFear(_stats.FearFromAllyDeath);
            }
        }

        private void HandlePoiseBroken()
        {
            if (_stats == null) return;
            AddFear(_stats.FearFromPoiseBroken);
        }

        // ──────────────────── Internal ────────────────────

        private void TriggerFlee()
        {
            IsFleeing = true;
            _brain.ForceFleeCheck();
        }
    }
}
