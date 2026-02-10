using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Lightweight hierarchical finite state machine (HFSM).
    /// Pure C# class — attach to no GameObject. Tick it manually from a MonoBehaviour.
    /// Supports nesting: any IState implementation can own a child StateMachine
    /// and call childMachine.Tick() inside its own OnUpdate.
    /// </summary>
    public class StateMachine
    {
        /// <summary> The currently active state. Read-only for external query / debug. </summary>
        public IState CurrentState { get; private set; }

        /// <summary> Optional label for debug logging (e.g. "Outer", "EngageSub"). </summary>
        public string DebugName { get; set; }

        /// <summary>
        /// Initialize the state machine with a starting state.
        /// Calls OnEnter on the initial state immediately.
        /// </summary>
        public void Initialize(IState startingState)
        {
            CurrentState = startingState;
            CurrentState.OnEnter();
        }

        /// <summary>
        /// Drive the current state forward by one frame.
        /// Call this from MonoBehaviour.Update() or from a parent state's OnUpdate().
        /// </summary>
        public void Tick(float deltaTime)
        {
            CurrentState?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// Transition from the current state to a new state.
        /// Execution order: currentState.OnExit() → update reference → newState.OnEnter().
        /// Safe to call from within OnUpdate — the transition happens immediately.
        /// </summary>
        public void TransitionTo(IState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning($"[StateMachine:{DebugName}] Attempted to transition to null state.");
                return;
            }

            if (newState == CurrentState)
                return; // No-op: already in this state

            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();
        }
    }
}
