namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Idle state: enemy stands still at spawn point, waiting for perception to detect a target.
    /// Transitions to ChaseState when HasTarget becomes true.
    /// </summary>
    public class IdleState : IState
    {
        private readonly EnemyBrain _brain;

        public IdleState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void OnEnter()
        {
            _brain.Entity.StopMovement();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_brain.Perception.HasTarget)
            {
                _brain.StateMachine.TransitionTo(_brain.ChaseState);
            }
        }

        public void OnExit() { }
    }
}
