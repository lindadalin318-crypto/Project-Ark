using UnityEngine;

namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Evaluates <see cref="StateTransitionRule"/> overrides against current enemy state.
    /// Used by states (ChaseState, etc.) to check data-driven transitions BEFORE
    /// falling back to hardcoded logic. Keeps backward compatibility when no rules exist.
    /// </summary>
    public static class TransitionRuleEvaluator
    {
        /// <summary>
        /// Evaluate all transition overrides for the current state context.
        /// Returns the first matching rule, or null if no rule matches.
        /// Rules are checked in priority order (higher priority first).
        /// </summary>
        public static StateTransitionRule Evaluate(EnemyBrain brain, float timeInState = 0f)
        {
            var rules = brain.Stats?.TransitionOverrides;
            if (rules == null || rules.Length == 0) return null;

            var perception = brain.Perception;
            var entity = brain.Entity;

            StateTransitionRule bestMatch = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                if (rule.Priority < bestPriority) continue;

                bool conditionMet = EvaluateCondition(rule, perception, entity, timeInState);
                if (conditionMet && rule.Priority >= bestPriority)
                {
                    bestMatch = rule;
                    bestPriority = rule.Priority;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Resolve a <see cref="EnemyStateType"/> to the actual IState instance on the brain.
        /// Returns null if the state type is not available on this brain.
        /// </summary>
        public static IState ResolveState(EnemyBrain brain, EnemyStateType stateType)
        {
            return stateType switch
            {
                EnemyStateType.Idle    => brain.IdleState,
                EnemyStateType.Chase   => brain.ChaseState,
                EnemyStateType.Engage  => brain.EngageState,
                EnemyStateType.Return  => brain.ReturnState,
                EnemyStateType.Orbit   => brain.OrbitState,
                EnemyStateType.Flee    => brain.FleeState,
                EnemyStateType.Dodge   => brain.DodgeState,
                EnemyStateType.Block   => brain.BlockState,
                EnemyStateType.Stagger => brain.StaggerState,
                EnemyStateType.Shoot   => (brain is ShooterBrain sb) ? sb.ShootState : null,
                EnemyStateType.Retreat => (brain is ShooterBrain sb2) ? sb2.RetreatState : null,
                _ => null
            };
        }

        private static bool EvaluateCondition(StateTransitionRule rule,
                                               EnemyPerception perception,
                                               EnemyEntity entity,
                                               float timeInState)
        {
            return rule.Condition switch
            {
                TransitionCondition.TargetInRange =>
                    perception.HasTarget && perception.DistanceToTarget < rule.Threshold,

                TransitionCondition.TargetOutOfRange =>
                    !perception.HasTarget || perception.DistanceToTarget > rule.Threshold,

                TransitionCondition.TargetLost =>
                    !perception.HasTarget,

                TransitionCondition.HealthBelow =>
                    entity.RuntimeMaxHP > 0f &&
                    (entity.CurrentHP / entity.RuntimeMaxHP) < rule.Threshold,

                TransitionCondition.HealthAbove =>
                    entity.RuntimeMaxHP > 0f &&
                    (entity.CurrentHP / entity.RuntimeMaxHP) > rule.Threshold,

                TransitionCondition.PoiseBroken =>
                    entity.IsStaggered,

                TransitionCondition.TimeInState =>
                    timeInState > rule.Threshold,

                _ => false
            };
        }
    }
}
