using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Abstract base class for all Satellite runtime behaviors.
    /// Implements the IF-THEN pattern: <see cref="EvaluateTrigger"/> checks the condition,
    /// <see cref="Execute"/> performs the response action.
    ///
    /// Lifecycle (managed by <see cref="SatelliteRunner"/>):
    /// 1. Initialize(context)           — called once after instantiation
    /// 2. EvaluateTrigger(context)      — called every frame (when cooldown ready)
    /// 3. Execute(context)              — called when trigger fires
    /// 4. Cleanup()                     — called before destruction on unequip
    ///
    /// Supports both polling-based (check state in EvaluateTrigger) and
    /// event-based (set flag in event handler, return it in EvaluateTrigger) patterns.
    /// </summary>
    public abstract class SatelliteBehavior : MonoBehaviour
    {
        /// <summary> Called once after instantiation. Subscribe to events, cache references here. </summary>
        public virtual void Initialize(StarChartContext context) { }

        /// <summary>
        /// Evaluate the trigger condition each frame. Return true to attempt execution.
        /// Only called when the internal cooldown is ready.
        /// </summary>
        public abstract bool EvaluateTrigger(StarChartContext context);

        /// <summary>
        /// Execute the response action. Only called when EvaluateTrigger returned true
        /// and the internal cooldown was ready. Cooldown resets after this call.
        /// </summary>
        public abstract void Execute(StarChartContext context);

        /// <summary> Called before the behavior GameObject is destroyed. Unsubscribe events here. </summary>
        public virtual void Cleanup() { }
    }
}
