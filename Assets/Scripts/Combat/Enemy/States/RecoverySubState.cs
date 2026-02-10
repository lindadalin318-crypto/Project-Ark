namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Recovery sub-state: post-attack cooldown (硬直).
    /// Enemy cannot move or attack during this phase — this is the player's punish window.
    /// After RecoveryDuration expires, signals the parent EngageState that the cycle is complete.
    /// </summary>
    public class RecoverySubState : IState
    {
        private readonly EnemyBrain _brain;
        private readonly EngageState _engage;
        private float _timer;

        public RecoverySubState(EnemyBrain brain, EngageState engage)
        {
            _brain = brain;
            _engage = engage;
        }

        public void OnEnter()
        {
            _timer = _brain.Stats.RecoveryDuration;

            // Ensure enemy is stationary during recovery
            _brain.Entity.StopMovement();
        }

        public void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;

            if (_timer <= 0f)
            {
                // Signal the parent EngageState that the full attack cycle is done
                _engage.IsAttackCycleComplete = true;
            }
        }

        public void OnExit() { }
    }
}
