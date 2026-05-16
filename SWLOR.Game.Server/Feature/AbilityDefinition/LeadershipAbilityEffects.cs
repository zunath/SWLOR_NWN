using System;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class LeadershipAbilityEffects
    {
        private const float BaseCommandRadius = 5f;

        private static readonly Type[][] LeadershipAuraFamilies =
        {
            new[]
            {
                typeof(RallyingStandard1StatusEffect),
                typeof(RallyingStandard2StatusEffect),
            },
            new[]
            {
                typeof(CoordinatedFocus1StatusEffect),
                typeof(CoordinatedFocus2StatusEffect),
                typeof(CoordinatedFocus3StatusEffect),
            },
            new[]
            {
                typeof(ChargeOrder1StatusEffect),
                typeof(ChargeOrder2StatusEffect),
            },
            new[]
            {
                typeof(WatchfulPresence1StatusEffect),
                typeof(WatchfulPresence2StatusEffect),
                typeof(WatchfulPresence3StatusEffect),
            },
            new[]
            {
                typeof(SteadyFormation1StatusEffect),
                typeof(SteadyFormation2StatusEffect),
            },
            new[]
            {
                typeof(FieldRecovery1StatusEffect),
                typeof(FieldRecovery2StatusEffect),
            },
        };

        public static float GetLeadershipCommandRadius(uint activator)
        {
            return BaseCommandRadius + Stat.GetStatAdjustment(activator, StatType.LeadershipCommandRadiusBonusMeters);
        }

        public static bool ToggleVanguardCommandAura(uint activator, Type statusEffectType)
        {
            return ToggleLeadershipAura(activator, statusEffectType);
        }

        public static bool ToggleFieldStewardAura(uint activator, Type statusEffectType)
        {
            return ToggleLeadershipAura(activator, statusEffectType);
        }

        public static float ApplyLeadershipCommandDurationBonus(uint activator, float durationSeconds)
        {
            var baseBonus = Stat.GetStatAdjustment(activator, StatType.LeadershipCommandDurationBonusBaseSeconds);
            var maximumBonus = Stat.GetStatAdjustment(activator, StatType.LeadershipCommandDurationBonusMaximumSeconds);
            if (maximumBonus <= 0)
                return durationSeconds;

            var bonusSeconds = maximumBonus > baseBonus
                ? AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseBonus, maximumBonus)
                : baseBonus;

            return durationSeconds + bonusSeconds;
        }

        public static float ApplyFieldStewardDurationBonus(uint activator, float durationSeconds)
        {
            return durationSeconds + Stat.GetStatAdjustment(activator, StatType.FieldStewardDurationBonusSeconds);
        }

        public static float ApplyFieldStewardCommandDurationBonus(uint activator, float durationSeconds)
        {
            durationSeconds = ApplyLeadershipCommandDurationBonus(activator, durationSeconds);
            return ApplyFieldStewardDurationBonus(activator, durationSeconds);
        }

        public static void ApplyTriageProtocol(uint activator, uint target, float durationSeconds)
        {
            var level = Stat.GetStatAdjustment(activator, StatType.FieldStewardTriageProtocolLevel);
            if (level <= 0)
                return;

            var statusEffect = level >= 2
                ? typeof(TriageProtocol2StatusEffect)
                : typeof(TriageProtocol1StatusEffect);

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, durationSeconds);
        }

        private static bool ToggleLeadershipAura(uint activator, Type statusEffectType)
        {
            if (!Ability.ToggleAura(activator, statusEffectType))
                return false;

            foreach (var auraType in GetAuraFamily(statusEffectType))
            {
                if (auraType != statusEffectType)
                    Ability.RemoveAura(activator, auraType);
            }

            Ability.ApplyAura(activator, statusEffectType, true, true, false);
            return true;
        }

        private static Type[] GetAuraFamily(Type statusEffectType)
        {
            foreach (var family in LeadershipAuraFamilies)
            {
                foreach (var auraType in family)
                {
                    if (auraType == statusEffectType)
                        return family;
                }
            }

            return Array.Empty<Type>();
        }
    }
}
