using System.Collections.Generic;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Pure condition evaluator for dialogue entry rules and choices.
    /// </summary>
    public static class DialogueConditionEvaluator
    {
        public static bool AreAllSatisfied(IReadOnlyList<DialogueConditionData> conditions, DialogueContext context)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            if (context == null)
            {
                return false;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                if (!IsSatisfied(conditions[i], context))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSatisfied(DialogueConditionData condition, DialogueContext context)
        {
            if (condition == null || context == null)
            {
                return false;
            }

            return condition.ConditionType switch
            {
                DialogueConditionType.WorldStage => Compare(context.WorldStage, condition.CompareOp, condition.IntValue),
                DialogueConditionType.RelationshipValue => Compare(context.RelationshipValue, condition.CompareOp, condition.IntValue),
                DialogueConditionType.FlagPresent => context.HasFlag(condition.FlagKey),
                DialogueConditionType.FlagAbsent => !context.HasFlag(condition.FlagKey),
                _ => false,
            };
        }

        private static bool Compare(int left, DialogueCompareOp compareOp, int right)
        {
            return compareOp switch
            {
                DialogueCompareOp.Equal => left == right,
                DialogueCompareOp.NotEqual => left != right,
                DialogueCompareOp.GreaterThan => left > right,
                DialogueCompareOp.GreaterOrEqual => left >= right,
                DialogueCompareOp.LessThan => left < right,
                DialogueCompareOp.LessOrEqual => left <= right,
                _ => false,
            };
        }
    }
}
