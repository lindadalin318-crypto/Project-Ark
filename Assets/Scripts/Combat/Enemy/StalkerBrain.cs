using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Stalker-type enemy brain. Stealth assassin that flanks and backstabs.
    /// Overrides the standard HFSM with stalker-specific states:
    ///   Stealth (semi-transparent, drifting) -> Flank (move to player's rear arc)
    ///   -> Strike (reveal + fast melee) -> Disengage (dash away, fade to stealth)
    /// Forces player awareness of surroundings — punishes tunnel-vision.
    /// </summary>
    public class StalkerBrain : EnemyBrain
    {
        // ──────────────────── Stalker-Specific States ────────────────────
        private StealthState _stealthState;
        private FlankState _flankState;
        private StalkerStrikeState _strikeState;
        private DisengageState _disengageState;

        // ──────────────────── Public Accessors ────────────────────
        public StealthState StealthState => _stealthState;
        public FlankState FlankState => _flankState;
        public StalkerStrikeState StrikeState => _strikeState;
        public DisengageState DisengageState => _disengageState;

        // ──────────────────── Stalker Config ────────────────────

        [Header("Stalker Settings")]
        [Tooltip("Sprite alpha when in stealth mode (0 = invisible, 1 = fully visible).")]
        [SerializeField] [Range(0f, 1f)] private float _stealthAlpha = 0.1f;

        [Tooltip("Speed at which the sprite fades in from stealth (alpha per second).")]
        [SerializeField] [Min(0.1f)] private float _revealSpeed = 5f;

        [Tooltip("Distance from player at which disengage completes and stealth resumes.")]
        [SerializeField] [Min(1f)] private float _disengageDistance = 8f;

        [Tooltip("Speed multiplier during disengage dash (relative to MoveSpeed).")]
        [SerializeField] [Min(1f)] private float _disengageSpeedMultiplier = 2f;

        /// <summary> Stealth alpha (low = nearly invisible). </summary>
        public float StealthAlpha => _stealthAlpha;

        /// <summary> Speed of fade-in reveal (alpha/sec). </summary>
        public float RevealSpeed => _revealSpeed;

        /// <summary> Distance threshold to finish disengage. </summary>
        public float DisengageDistance => _disengageDistance;

        /// <summary> Speed multiplier for disengage. </summary>
        public float DisengageSpeedMultiplier => _disengageSpeedMultiplier;

        // ──────────────────── Cached Renderer ────────────────────
        private SpriteRenderer _spriteRenderer;

        /// <summary> Cached sprite renderer for alpha manipulation. </summary>
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        // ──────────────────── HFSM Construction ────────────────────

        protected override void Awake()
        {
            base.Awake();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void BuildStateMachine()
        {
            // Do NOT call base — Stalker doesn't use Chase/Engage/Return/Orbit
            _entity = GetComponent<EnemyEntity>();
            _perception = GetComponent<EnemyPerception>();
            _stats = _entity.Stats;

            // Create stalker-specific states
            _stealthState = new StealthState(this);
            _flankState = new FlankState(this);
            _strikeState = new StalkerStrikeState(this);
            _disengageState = new DisengageState(this);

            // Build the state machine starting in Stealth
            _stateMachine = new StateMachine { DebugName = "StalkerOuter" };
            _stateMachine.Initialize(_stealthState);

            // Subscribe poise event — stalkers can be staggered (breaks stealth)
            _entity.OnPoiseBroken -= ForceStaggerStalker;
            _entity.OnPoiseBroken += ForceStaggerStalker;
        }

        // ──────────────────── Poise / Stagger ────────────────────

        private void ForceStaggerStalker()
        {
            if (_stats != null && _stats.BehaviorTags.Contains("SuperArmor"))
            {
                _entity.ResetPoise();
                return;
            }

            ReturnDirectorToken();

            // Breaking poise reveals the stalker
            SetAlpha(1f);

            var stagger = new StaggerState(this);
            _stateMachine.TransitionTo(stagger);
        }

        protected override void OnDisable()
        {
            if (_entity != null)
                _entity.OnPoiseBroken -= ForceStaggerStalker;

            ReturnDirectorToken();
        }

        // ──────────────────── Alpha Helpers ────────────────────

        /// <summary>
        /// Set sprite alpha directly. Used by all stalker states.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = Mathf.Clamp01(alpha);
                _spriteRenderer.color = c;
            }
        }

        /// <summary>
        /// Get current sprite alpha.
        /// </summary>
        public float GetAlpha()
        {
            return _spriteRenderer != null ? _spriteRenderer.color.a : 1f;
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        protected override void OnGUI()
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null) return;

            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                string stateName = _stateMachine.CurrentState.GetType().Name;
                float alpha = GetAlpha();

                GUI.color = new Color(0.8f, 0.2f, 0.8f, 1f); // Purple for stalker
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 250, 25),
                          $"[Stalker: {stateName} a={alpha:F1}]");
            }
        }
#endif
    }
}
