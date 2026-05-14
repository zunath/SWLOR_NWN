using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class AITarget
    {
        private static readonly Dictionary<FeatType, AITargetSelector> _defaultOverrides = new();

        public static void RegisterDefault(FeatType feat, AITargetSelector selector)
        {
            _defaultOverrides[feat] = selector;
        }

        public static bool TryGetDefaultOverride(FeatType feat, out AITargetSelector selector)
        {
            return _defaultOverrides.TryGetValue(feat, out selector);
        }

        public static AITargetSelector Self()
        {
            return context => context.Self;
        }

        public static AITargetSelector HighestEnmity()
        {
            return context => context.CurrentEnemyTarget;
        }

        public static AITargetSelector Master()
        {
            return context => context.Master;
        }

        public static AITargetSelector LowestHealthAlly(bool includeSelf = true, float maxRange = 5f)
        {
            return context => context.GetLowestHealthAlly(includeSelf, maxRange);
        }

        public static AITargetSelector HostileCluster(float radius, int minimumTargets)
        {
            return context =>
            {
                var target = context.CurrentEnemyTarget;
                if (!GetIsObjectValid(target))
                    return OBJECT_INVALID;

                context.SetEvaluatedTarget(target);
                return context.CountHostilesNearTarget(radius) >= minimumTargets
                    ? target
                    : OBJECT_INVALID;
            };
        }

        public static AITargetSelector AllyAttacker()
        {
            return HighestEnmity();
        }

        public static AITargetSelector InferDefault(FeatType feat, AbilityDetail ability)
        {
            if (ability.IsHostileAbility || IsHostileFeat(feat))
            {
                return ability.IsAreaAbility
                    ? HostileCluster(ability.MaxRange, 2)
                    : HighestEnmity();
            }

            if (ability.RequiresTarget)
                return LowestHealthAlly(true, ability.MaxRange);

            if (IsTargetSelfFeat(feat))
                return Self();

            return Self();
        }

        private static bool IsTargetSelfFeat(FeatType feat)
        {
            return Is2DAFlagSet(feat, "TARGETSELF");
        }

        private static bool IsHostileFeat(FeatType feat)
        {
            return Is2DAFlagSet(feat, "HostileFeat");
        }

        private static bool Is2DAFlagSet(FeatType feat, string column)
        {
            var value = Get2DAString("feat", column, (int)feat);
            if (string.IsNullOrWhiteSpace(value) || value == "****")
                return false;

            return int.TryParse(value, out var intValue)
                ? intValue > 0
                : value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
