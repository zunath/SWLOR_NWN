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
            return context => context.CurrentEnmityTarget;
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
                var target = context.CurrentEnmityTarget;
                if (target == OBJECT_INVALID || !GetIsObjectValid(target))
                    return OBJECT_INVALID;

                context.SetEvaluatedTarget(target);
                return context.CountHostilesNearTarget(radius) >= minimumTargets
                    ? target
                    : OBJECT_INVALID;
            };
        }

        private static AITargetSelector SelfCenteredHostileArea(AbilityDetail ability, int minimumTargets)
        {
            return context =>
            {
                if (!GetIsObjectValid(context.CurrentEnmityTarget))
                    return OBJECT_INVALID;

                var radius = ability.Targeting?.ResolveSizeX(context.Self, true) ?? 0f;
                if (radius <= 0f)
                    radius = ability.MaxRange;

                // Self-centered areas must evaluate the creatures actually inside the caster's
                // effect, not a cluster around the current target. Returning self also keeps the
                // activation location anchored on the caster.
                context.SetEvaluatedTarget(context.Self);
                return radius > 0f && context.CountHostilesNearTarget(radius) >= minimumTargets
                    ? context.Self
                    : OBJECT_INVALID;
            };
        }

        public static AITargetSelector AllyAttacker(float maxRange = 10f)
        {
            return context =>
            {
                var bestTarget = OBJECT_INVALID;
                var bestEnmity = 0;

                foreach (var ally in context.Allies)
                {
                    if (ally == context.Self || !GetIsObjectValid(ally))
                        continue;

                    foreach (var (enemy, amount) in Enmity.GetEnmityTowardsAllEnemies(ally))
                    {
                        if (amount <= bestEnmity ||
                            enemy == context.Self ||
                            !GetIsObjectValid(enemy) ||
                            !GetIsEnemy(enemy, context.Self) ||
                            maxRange > 0f && GetDistanceBetween(context.Self, enemy) > maxRange ||
                            !LineOfSightObject(context.Self, enemy) ||
                            Enmity.GetHighestEnmityTarget(enemy) == context.Self)
                        {
                            continue;
                        }

                        bestTarget = enemy;
                        bestEnmity = amount;
                    }
                }

                return bestTarget != OBJECT_INVALID
                    ? bestTarget
                    : context.CurrentEnmityTarget;
            };
        }

        public static AITargetSelector InferDefault(FeatType feat, AbilityDetail ability)
        {
            if (ability.IsHostileAbility)
            {
                if (!ability.IsAreaAbility)
                    return HighestEnmity();

                return ability.Targeting?.Shape == AbilityTargetingShapeType.Sphere &&
                       ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf)
                    ? SelfCenteredHostileArea(ability, 2)
                    : HostileCluster(ability.MaxRange, 2);
            }

            if (ability.RequiresTarget)
                return LowestHealthAlly(true, ability.MaxRange);

            return Self();
        }
    }
}
