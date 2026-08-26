using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class AIScore
    {
        public static AIScoreCalculation Fixed(int score)
        {
            return _ => score;
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
        /// Scores a defensive self-buff only while the creature is in combat and its active rank is
        /// below the rank supplied by the ability.
        /// </summary>
        public static AIScoreCalculation SelfStatBelow(StatType activeRankStat, int requiredRank, int abilityLevel)
        {
            return context => context.CurrentEnmityTarget != OBJECT_INVALID &&
                              Stat.GetStatAdjustment(context.Self, activeRankStat) < requiredRank
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

            return context => context.CurrentEnmityTarget != OBJECT_INVALID
                ? AIScoreBand.Defensive + ability.AbilityLevel
                : 0;
        }
    }
}
