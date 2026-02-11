using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Turret-type enemy brain. Stationary area-denial enemy.
    /// Overrides the standard HFSM with turret-specific states:
    ///   Scan (rotate/idle) -> Lock (track target + aim line) -> Attack (fire laser/projectile) -> Cooldown
    /// Does NOT use Chase, Engage, Return, or Orbit states (MoveSpeed = 0).
    /// Supports two attack variants via AttackDataSO:
    ///   - Laser: sustained beam (EnemyLaserBeam)
    ///   - Projectile: single charged high-damage shot
    /// </summary>
    public class TurretBrain : EnemyBrain
    {
        // ──────────────────── Turret-Specific States ────────────────────
        private TurretScanState _scanState;
        private TurretLockState _lockState;
        private TurretAttackState _attackState;
        private TurretCooldownState _cooldownState;

        // ──────────────────── Public Accessors ────────────────────
        public TurretScanState ScanState => _scanState;
        public TurretLockState LockState => _lockState;
        public TurretAttackState TurretAttackState => _attackState;
        public TurretCooldownState CooldownState => _cooldownState;

        // ──────────────────── Turret Config ────────────────────

        [Header("Turret Settings")]
        [Tooltip("Rotation speed while scanning (degrees/second). Slower than lock tracking.")]
        [SerializeField] private float _scanRotationSpeed = 45f;

        [Tooltip("Time to lock on to target before firing (seconds).")]
        [SerializeField] [Min(0.1f)] private float _lockOnDuration = 0.8f;

        /// <summary> Scan rotation speed (degrees/second). </summary>
        public float ScanRotationSpeed => _scanRotationSpeed;

        /// <summary> Time to acquire lock before attacking. </summary>
        public float LockOnDuration => _lockOnDuration;

        // ──────────────────── Selected Attack (for current cycle) ────────────────────
        private AttackDataSO _selectedAttack;

        /// <summary>
        /// The attack selected for the current attack cycle.
        /// Set during Lock phase, consumed by Attack phase.
        /// </summary>
        public AttackDataSO SelectedAttack => _selectedAttack;

        /// <summary>
        /// Select and store the attack for the next firing cycle.
        /// Called by TurretLockState on entering lock.
        /// </summary>
        public void SelectAttackForCycle()
        {
            _selectedAttack = _stats.SelectRandomAttack();
        }

        // ──────────────────── HFSM Construction ────────────────────

        protected override void BuildStateMachine()
        {
            // We do NOT call base.BuildStateMachine() because Turret doesn't use
            // Chase/Engage/Return/Orbit states. We manually create what we need.

            // Create turret-specific states
            _scanState = new TurretScanState(this);
            _lockState = new TurretLockState(this);
            _attackState = new TurretAttackState(this);
            _cooldownState = new TurretCooldownState(this);

            // We still need stagger support — create it here
            // (Turrets can be staggered if they have poise)
            var staggerState = new StaggerState(this);

            // Build the state machine
            _stateMachine = new StateMachine { DebugName = "TurretOuter" };
            _stateMachine.Initialize(_scanState);

            // Subscribe to poise break (uses base brain's entity)
            _entity = GetComponent<EnemyEntity>();
            _perception = GetComponent<EnemyPerception>();
            _stats = _entity.Stats;

            // Subscribe poise event — turrets can be staggered
            _entity.OnPoiseBroken -= ForceStaggerTurret;
            _entity.OnPoiseBroken += ForceStaggerTurret;
        }

        private void ForceStaggerTurret()
        {
            if (_stats != null && _stats.BehaviorTags.Contains("SuperArmor"))
            {
                _entity.ResetPoise();
                return;
            }

            // Use a local stagger state — transition via state machine
            var stagger = new StaggerState(this);
            _stateMachine.TransitionTo(stagger);
        }

        protected override void OnDisable()
        {
            if (_entity != null)
                _entity.OnPoiseBroken -= ForceStaggerTurret;
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

                GUI.color = Color.yellow;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 250, 25),
                          $"[Turret: {stateName}]");
            }
        }
#endif
    }
}
