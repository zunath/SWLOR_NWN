using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class AIUtilitySelector
    {
        public static (FeatType feat, uint target) SelectAction(
            AIContext context,
            IEnumerable<AIActionDefinition> actions,
            Func<uint, uint, FeatType, bool> canUseFeat)
        {
            var bestScore = float.MinValue;
            var bestPriority = int.MaxValue;
            var bestFeat = FeatType.Invalid;
            var bestTarget = OBJECT_INVALID;

            foreach (var action in actions)
            {
                var target = ResolveTarget(context, action.TargetType);
                if (!GetIsObjectValid(target)) continue;
                if (!canUseFeat(context.Self, target, action.Feat)) continue;

                var phaseBonus = action.Phase == context.Phase ? 1.25f : 1.0f;
                var hpUrgency = (100f - context.LowestHPAllyPercentage) / 100f;
                var score = action.BaseWeight * phaseBonus;

                if (action.Phase == AIPhaseType.Survival || action.Phase == AIPhaseType.Support)
                    score += hpUrgency;

                if (score > bestScore || (Math.Abs(score - bestScore) < 0.001f && action.Priority < bestPriority))
                {
                    bestScore = score;
                    bestPriority = action.Priority;
                    bestFeat = action.Feat;
                    bestTarget = target;
                }
            }

            return (bestFeat, bestTarget);
        }

        private static uint ResolveTarget(AIContext context, AITargetType targetType)
        {
            return targetType switch
            {
                AITargetType.Self => context.Self,
                AITargetType.CurrentTarget => context.CurrentTarget,
                AITargetType.LowestHPAlly => context.LowestHPAlly,
                AITargetType.AllyWithTreatmentKit1Status => context.AllyWithTreatmentKit1Status,
                AITargetType.AllyWithTreatmentKit2Status => context.AllyWithTreatmentKit2Status,
                _ => OBJECT_INVALID,
            };
        }
    }
}
