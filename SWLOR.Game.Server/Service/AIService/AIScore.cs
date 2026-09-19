using SWLOR.Game.Server.Service.AbilityService;
using System.Linq;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class AIScore
    {
        public const int MinimumHealthAfterHitPointCostPercent = 50;

        public static AIScoreCalculation WithHitPointCostReserve(Func<uint, int> costPercent, AIScoreCalculation score)
        {
            return context =>
            {
                var currentHP = context.SelfHitPoints;
                var maximumHP = context.SelfMaxHitPoints;
                if (maximumHP <= 0 || currentHP <= 1 ||
                    (long)currentHP * 100 <= (long)maximumHP * MinimumHealthAfterHitPointCostPercent)
                    return 0;

                // Match the upward rounding used when the ability spends HP.
                var cost = GameMath.PercentOf(maximumHP, Math.Max(0, costPercent(context.Self)));
                return (long)(currentHP - cost) * 100 >= (long)maximumHP * MinimumHealthAfterHitPointCostPercent
                    ? score(context)
                    : 0;
            };
        }

        public static AIScoreCalculation Fixed(int score)
        {
            return _ => score;
        }

        public static AIScoreCalculation WithTarget(int score)
        {
            return context => context.EvaluatedTarget != OBJECT_INVALID ? score : 0;
        }

        public static AIScoreCalculation SelfHealthBelow(int thresholdPercent, int score)
        {
            return context => context.SelfHealthPercent <= thresholdPercent
                ? score + thresholdPercent - context.SelfHealthPercent
                : 0;
        }

        public static AIScoreCalculation TargetHealthBelow(int thresholdPercent, int score)
        {
            return context => context.TargetHealthPercent <= thresholdPercent
                ? score + thresholdPercent - context.TargetHealthPercent
                : 0;
        }

        /// <summary>
        /// Scores a self-buff using the same replacement relationships as status application.
        /// The ability supplies its effect definition; AI has no ability-specific rank markers.
        /// </summary>
        public static AIScoreCalculation SelfBuff<T>(int abilityLevel) where T : IStatusEffect, new()
        {
            var effect = new T();
            var redundantEffects = effect.MorePowerfulEffectTypes.Append(typeof(T)).ToHashSet();
            return context => context.CurrentEnmityTarget != OBJECT_INVALID &&
                              !StatusEffect.HasAnyActiveEffect(context.Self, redundantEffects)
                ? AIScoreBand.Defensive + abilityLevel
                : 0;
        }

        public static AIScoreCalculation Cluster(int baseScore, int perTarget, float radius = 10f)
        {
            return context =>
            {
                var count = context.CountHostilesNearTarget(radius);
                return count <= 0
                    ? 0
                    : baseScore + count * perTarget;
            };
        }

        public static AIScoreCalculation ThreatControl(int abilityLevel)
        {
            return context => context.EvaluatedTarget != OBJECT_INVALID
                ? AIScoreBand.ThreatControl + abilityLevel
                : 0;
        }

        public static AIScoreCalculation AreaThreatControl(int abilityLevel, float radius)
        {
            return context =>
            {
                var count = context.CountHostilesNearTarget(radius);
                return count <= 0
                    ? 0
                    : AIScoreBand.ThreatControl + abilityLevel + count * 25;
            };
        }

        public static AIScoreCalculation Ability(AbilityDetail ability)
        {
            var score = ability.AIScore ?? DefaultAbility(ability);
            return ability.AIHitPointCostPercent == null
                ? score
                : WithHitPointCostReserve(ability.AIHitPointCostPercent, score);
        }

        private static AIScoreCalculation DefaultAbility(AbilityDetail ability)
        {
            if (ability.IsHostileAbility && ability.IsAreaAbility)
            {
                return Cluster(
                    AIScoreBand.AreaDamage + ability.AbilityLevel,
                    25,
                    ability.MaxRange);
            }

            if (ability.IsHostileAbility)
            {
                return Fixed(AIScoreBand.SingleTargetDamage + ability.AbilityLevel);
            }

            if (ability.RequiresTarget)
            {
                return context =>
                {
                    if (context.TargetHealthPercent > 80)
                        return 0;

                    return AIScoreBand.Healing + ability.AbilityLevel + 100 - context.TargetHealthPercent;
                };
            }

            // Avoid recasting active buffs or replacing an active, mutually exclusive stance.
            var selfEffects = ability.StatusEffectTypesRemovedOnPerkRefund.ToHashSet();
            var isStance = selfEffects.Any(effect =>
                StatusEffect.GetStatusEffectSourceType(effect) == StatusEffectSourceType.Stance);
            return context => context.CurrentEnmityTarget != OBJECT_INVALID &&
                              (!isStance || !StatusEffect.HasAnyActiveEffect(context.Self, StatusEffectSourceType.Stance)) &&
                              !StatusEffect.HasAnyActiveEffect(context.Self, selfEffects)
                ? AIScoreBand.Defensive + ability.AbilityLevel
                : 0;
        }
    }
}
