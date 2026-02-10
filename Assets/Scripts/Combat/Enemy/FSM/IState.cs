namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Contract for a single state in the HFSM.
    /// Implement this interface for each discrete behavior (Idle, Chase, Engage, etc.).
    /// States are plain C# objects — not MonoBehaviours — for testability and zero GC.
    /// </summary>
    public interface IState
    {
        /// <summary> Called once when transitioning INTO this state. </summary>
        void OnEnter();

        /// <summary> Called every frame while this state is active. </summary>
        /// <param name="deltaTime">Time.deltaTime passed from the brain's Update.</param>
        void OnUpdate(float deltaTime);

        /// <summary> Called once when transitioning OUT of this state. </summary>
        void OnExit();
    }
}
