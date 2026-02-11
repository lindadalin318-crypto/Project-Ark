namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Engage state: contains an inner sub-state machine for the attack sequence
    /// (Telegraph -> Attack -> Recovery). This is the "commitment" phase.
    /// On completion of the sub-state machine cycle, exits back to the outer FSM
    /// which decides Chase or Idle based on distance.
    /// Supports data-driven attacks via AttackDataSO (falls back to legacy EnemyStatsSO fields).
    /// </summary>
    public class EngageState : IState
    {
        private readonly EnemyBrain _brain;
        private StateMachine _subStateMachine;

        // Sub-states
        private TelegraphSubState _telegraphState;
        private AttackSubState _attackState;
        private RecoverySubState _recoveryState;

        /// <summary>
        /// The attack selected for this engagement cycle.
        /// Null when using legacy EnemyStatsSO flat fields.
        /// Sub-states read from this to get timings, damage, hitbox shape, etc.
        /// </summary>
        public AttackDataSO SelectedAttack { get; private set; }

        /// <summary>
        /// Set to true by RecoverySubState when the attack cycle is complete.
        /// The outer OnUpdate reads this to exit Engage.
        /// </summary>
        public bool IsAttackCycleComplete { get; set; }

        public EngageState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            IsAttackCycleComplete = false;

            // Select attack pattern (data-driven or null for legacy)
            SelectedAttack = _brain.Stats.SelectRandomAttack();

            // Stop movement — we are committing to attack
            _brain.Entity.StopMovement();

            // Build the inner sub-state machine
            _telegraphState = new TelegraphSubState(_brain, this);
            _attackState = new AttackSubState(_brain, this);
            _recoveryState = new RecoverySubState(_brain, this);

            _subStateMachine = new StateMachine { DebugName = "EngageSub" };
            _subStateMachine.Initialize(_telegraphState);
        }

        public void OnUpdate(float deltaTime)
        {
            // If the attack cycle completed, exit Engage
            if (IsAttackCycleComplete)
            {
                // Decide next outer state based on perception
                var perception = _brain.Perception;
                var stats = _brain.Stats;

                if (perception.HasTarget && perception.DistanceToTarget < stats.LeashRange)
                    _brain.StateMachine.TransitionTo(_brain.ChaseState);
                else
                    _brain.StateMachine.TransitionTo(_brain.ReturnState);
                return;
            }

            // Tick the inner sub-state machine
            _subStateMachine.Tick(deltaTime);
        }

        public void OnExit()
        {
            // Return attack token to the Director
            _brain.ReturnDirectorToken();

            // Cleanup sub-state machine
            _subStateMachine = null;
            _telegraphState = null;
            _attackState = null;
            _recoveryState = null;
            SelectedAttack = null;
        }

        // --- Accessors for sub-states ---
        public StateMachine SubStateMachine => _subStateMachine;
        public TelegraphSubState TelegraphState => _telegraphState;
        public AttackSubState AttackState => _attackState;
        public RecoverySubState RecoveryState => _recoveryState;
    }
}
