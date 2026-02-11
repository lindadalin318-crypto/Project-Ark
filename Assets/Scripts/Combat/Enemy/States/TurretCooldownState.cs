namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Turret cooldown state: post-attack recovery period.
    /// Turret is vulnerable during this phase (similar to RecoverySubState for melee enemies).
    /// After AttackCooldown expires, transitions back to Scan or Lock based on target visibility.
    /// Transitions:
    ///   - Cooldown done + HasTarget -> TurretLockState
    ///   - Cooldown done + no target -> TurretScanState
    /// </summary>
    public class TurretCooldownState : IState
    {
        private readonly TurretBrain _brain;
        private float _timer;

        public TurretCooldownState(TurretBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.AttackCooldown;
            _brain.Entity.StopMovement();
        }

        public void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                // Check if target is still visible
                if (_brain.Perception.HasTarget)
                {
                    _brain.StateMachine.TransitionTo(_brain.LockState);
                }
                else
                {
                    _brain.StateMachine.TransitionTo(_brain.ScanState);
                }
            }
        }

        public void OnExit() { }
    }
}
