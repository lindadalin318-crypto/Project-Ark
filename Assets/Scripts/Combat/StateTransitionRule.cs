using System;
using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Conditions that can trigger a state transition.
    /// Evaluated by the state machine during update ticks.
    /// </summary>
    public enum TransitionCondition
    {
        /// <summary> Target distance is less than Threshold. </summary>
        TargetInRange,

        /// <summary> Target distance is greater than Threshold. </summary>
        TargetOutOfRange,

        /// <summary> Target is no longer tracked (HasTarget == false). </summary>
        TargetLost,

        /// <summary> Current HP ratio (0..1) is below Threshold. </summary>
        HealthBelow,

        /// <summary> Current HP ratio (0..1) is above Threshold. </summary>
        HealthAbove,

        /// <summary> Poise is broken (IsStaggered == true). </summary>
        PoiseBroken,

        /// <summary> Time spent in current state exceeds Threshold (seconds). </summary>
        TimeInState
    }

    /// <summary>
    /// Target state type for data-driven transitions.
    /// Maps to named states on the brain.
    /// </summary>
    public enum EnemyStateType
    {
        Idle,
        Chase,
        Engage,
        Shoot,
        Retreat,
        Return,
        Orbit,
        Flee,
        Dodge,
        Block,
        Stagger
    }

    /// <summary>
    /// A single data-driven transition rule. Serialized on EnemyStatsSO
    /// so designers can override hardcoded state transitions per enemy type.
    /// </summary>
    [Serializable]
    public class StateTransitionRule
    {
        [Tooltip("Condition that triggers this transition.")]
        public TransitionCondition Condition;

        [Tooltip("Threshold value for the condition (distance, HP ratio, time, etc.).")]
        public float Threshold;

        [Tooltip("The state to transition to when the condition is met.")]
        public EnemyStateType TargetState;

        [Tooltip("Priority: higher priority rules are checked first. Default = 0.")]
        public int Priority;
    }
}
