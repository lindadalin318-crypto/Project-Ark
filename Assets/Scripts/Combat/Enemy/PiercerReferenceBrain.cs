using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// ReferenceOnly brain for the Minishoot Piercer evidence chain.
    /// It exposes a distinct charge-state name for visuals without driving production AI decisions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PiercerReferenceBrain : EnemyBrain
    {
        protected override void BuildStateMachine()
        {
            _stateMachine = new StateMachine { DebugName = "PiercerReference" };
            _stateMachine.Initialize(new PiercerReferenceChargeState());
        }

        protected override void Update()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.Tick(Time.deltaTime);
        }

        protected override void OnDisable()
        {
            // ReferenceOnly brain never requests director tokens, so it does not need token cleanup.
        }
    }
}
