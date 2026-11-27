using System;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The AI settings of active abilities. Creatures owns them, so <see cref="AbilityDetail"/> does not name AI types.
    /// The values are one <see cref="AbilityAISettingValues"/> object attached to each ability definition, which exists once per feat.
    /// </summary>
    public static class AbilityAISettings
    {
        public static AITargetSelector GetAITargetSelector(this AbilityDetail ability)
        {
            return ability.GetExtension<AbilityAISettingValues>().TargetSelector;
        }

        public static AIScoreCalculation GetAIScore(this AbilityDetail ability)
        {
            return ability.GetExtension<AbilityAISettingValues>().Score;
        }

        /// <summary>Maximum-HP percentage spent on activation, used to preserve an AI health reserve.</summary>
        public static Func<uint, int> GetAIHitPointCostPercent(this AbilityDetail ability)
        {
            return ability.GetExtension<AbilityAISettingValues>().HitPointCostPercent;
        }

        public static void SetAITargetSelector(this AbilityDetail ability, AITargetSelector selector)
        {
            ability.GetExtension<AbilityAISettingValues>().TargetSelector = selector;
        }

        public static void SetAIScore(this AbilityDetail ability, AIScoreCalculation score)
        {
            ability.GetExtension<AbilityAISettingValues>().Score = score;
        }

        public static void SetAIHitPointCostPercent(this AbilityDetail ability, Func<uint, int> costPercent)
        {
            ability.GetExtension<AbilityAISettingValues>().HitPointCostPercent = costPercent;
        }

        public static AbilityBuilder HasAITarget(this AbilityBuilder builder, AITargetSelector selector)
        {
            builder.ActiveAbility.SetAITargetSelector(selector);
            return builder;
        }

        public static AbilityBuilder HasAIScore(this AbilityBuilder builder, AIScoreCalculation score)
        {
            builder.ActiveAbility.SetAIScore(score);
            return builder;
        }

        public static AbilityBuilder HasAIHitPointCostPercent(this AbilityBuilder builder, Func<uint, int> costPercent)
        {
            builder.ActiveAbility.SetAIHitPointCostPercent(costPercent);
            return builder;
        }
    }
}
