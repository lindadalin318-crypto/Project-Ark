using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Minishoot-style charger enemy brain.
    /// Uses the standard Project Ark body/perception/director stack, but replaces
    /// the normal melee EngageState with a committed ChargeState.
    /// </summary>
    public class ChargeRusherBrain : EnemyBrain
    {
        private ChargeState _chargeState;

        /// <summary> Dedicated dash attack state. </summary>
        public ChargeState ChargeState => _chargeState;

        protected override void BuildStateMachine()
        {
            base.BuildStateMachine();

            _chargeState = new ChargeState(this);

            _stateMachine = new StateMachine { DebugName = "ChargeRusherOuter" };
            _stateMachine.Initialize(IdleState);
        }

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

                bool hasToken = EnemyDirector.Instance != null &&
                                EnemyDirector.Instance.HasToken(this);
                if (hasToken)
                    stateName += " [T]";

                GUI.color = new Color(1f, 0.45f, 0.15f, 1f);
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 250, 25),
                          $"[ChargeRusher: {stateName}]");
            }
        }
#endif
    }
}
