using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Enemy brain layer (大脑层). Owns the outer HFSM and wires up all states.
    /// References EnemyEntity (body) and EnemyPerception (senses).
    /// States access shared data through the public properties exposed here.
    /// Subclass (e.g. ShooterBrain) can override BuildStateMachine() to wire different states.
    /// </summary>
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(EnemyPerception))]
    public class EnemyBrain : MonoBehaviour
    {
        // ──────────────────── Cached References ────────────────────
        protected EnemyEntity _entity;
        protected EnemyPerception _perception;
        protected EnemyStatsSO _stats;

        // ──────────────────── HFSM ────────────────────
        protected StateMachine _stateMachine;

        // Pre-built state instances (reused across the enemy's lifetime)
        private IdleState _idleState;
        private ChaseState _chaseState;
        private EngageState _engageState;
        private ReturnState _returnState;
        private StaggerState _staggerState;

        // ──────────────────── Spawn Position ────────────────────
        protected Vector2 _spawnPosition;

        // ──────────────────── Public Properties (read by states) ────────────────────

        /// <summary> The body layer — movement, HP, damage. </summary>
        public EnemyEntity Entity => _entity;

        /// <summary> The perception system — vision, hearing, memory. </summary>
        public EnemyPerception Perception => _perception;

        /// <summary> The stats SO driving this enemy's configuration. </summary>
        public EnemyStatsSO Stats => _stats;

        /// <summary> World position where this enemy was spawned / placed. </summary>
        public Vector2 SpawnPosition => _spawnPosition;

        /// <summary> The outer state machine. States use this to request transitions. </summary>
        public StateMachine StateMachine => _stateMachine;

        // --- State accessors (states reference each other for transitions) ---
        public IdleState IdleState => _idleState;
        public ChaseState ChaseState => _chaseState;
        public EngageState EngageState => _engageState;
        public ReturnState ReturnState => _returnState;
        public StaggerState StaggerState => _staggerState;

        // ──────────────────── Lifecycle ────────────────────

        protected virtual void Awake()
        {
            _entity = GetComponent<EnemyEntity>();
            _perception = GetComponent<EnemyPerception>();
            _stats = _entity.Stats;

            // Record the spawn position (where the prefab was placed in the scene)
            _spawnPosition = transform.position;
        }

        protected virtual void Start()
        {
            BuildStateMachine();
        }

        protected virtual void Update()
        {
            // Don't tick the FSM if dead
            if (!_entity.IsAlive) return;

            _stateMachine.Tick(Time.deltaTime);
        }

        // ──────────────────── HFSM Construction ────────────────────

        /// <summary>
        /// Build the outer state machine with all four tactical states.
        /// Called once on Start(). States are plain C# objects — zero allocation per frame.
        /// Override in subclass (e.g. ShooterBrain) to wire different state graphs.
        /// </summary>
        protected virtual void BuildStateMachine()
        {
            // Create state instances (they hold a reference back to this brain)
            _idleState = new IdleState(this);
            _chaseState = new ChaseState(this);
            _engageState = new EngageState(this);
            _returnState = new ReturnState(this);
            _staggerState = new StaggerState(this);

            // Initialize the outer state machine starting in Idle
            _stateMachine = new StateMachine { DebugName = "Outer" };
            _stateMachine.Initialize(_idleState);

            // Subscribe to poise-break event for stagger transitions
            SubscribePoiseBroken();
        }

        // ──────────────────── Poise / Stagger ────────────────────

        private void SubscribePoiseBroken()
        {
            // Unsubscribe first to prevent double-subscribe on ResetBrain
            _entity.OnPoiseBroken -= ForceStagger;
            _entity.OnPoiseBroken += ForceStagger;
        }

        /// <summary>
        /// Force the state machine into Stagger state. Called when poise breaks.
        /// Enemies with "SuperArmor" behavior tag are immune.
        /// </summary>
        private void ForceStagger()
        {
            // SuperArmor（霸体）: immune to stagger
            if (_stats != null && _stats.BehaviorTags.Contains("SuperArmor"))
            {
                // Poise is still broken, but reset it immediately — no stagger animation
                _entity.ResetPoise();
                return;
            }

            if (_stateMachine != null && _staggerState != null)
                _stateMachine.TransitionTo(_staggerState);
        }

        protected virtual void OnDisable()
        {
            if (_entity != null)
                _entity.OnPoiseBroken -= ForceStagger;
        }

        // ──────────────────── Pool Support ────────────────────

        /// <summary>
        /// Called by EnemyEntity.OnGetFromPool() flow or manually to reset brain state.
        /// Re-records spawn position and rebuilds the FSM from Idle.
        /// </summary>
        public virtual void ResetBrain(Vector2 newSpawnPosition)
        {
            _spawnPosition = newSpawnPosition;
            _stats = _entity.Stats;
            BuildStateMachine();
        }

        // ──────────────────── Debug ────────────────────

#if UNITY_EDITOR
        protected virtual void OnGUI()
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null) return;

            // Show current state name above the enemy (editor only)
            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f)
                : Vector3.zero;

            if (screenPos.z > 0)
            {
                string stateName = _stateMachine.CurrentState.GetType().Name;

                // If in EngageState, also show sub-state
                if (_stateMachine.CurrentState is EngageState engage &&
                    engage.SubStateMachine?.CurrentState != null)
                {
                    stateName += $" > {engage.SubStateMachine.CurrentState.GetType().Name}";
                }

                GUI.color = Color.white;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 200, 25),
                          $"[{stateName}]");
            }
        }
#endif
    }
}
