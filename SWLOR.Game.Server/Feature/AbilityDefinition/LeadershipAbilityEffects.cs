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

        private static readonly Type[] VanguardCommandAuraTypes =
        {
            typeof(RallyingStandard1StatusEffect),
            typeof(RallyingStandard2StatusEffect),
            typeof(CoordinatedFocus1StatusEffect),
            typeof(CoordinatedFocus2StatusEffect),
            typeof(CoordinatedFocus3StatusEffect),
            typeof(ChargeOrder1StatusEffect),
            typeof(ChargeOrder2StatusEffect),
        };

        private static readonly Type[] FieldStewardAuraTypes =
        {
            typeof(WatchfulPresence1StatusEffect),
            typeof(WatchfulPresence2StatusEffect),
            typeof(WatchfulPresence3StatusEffect),
            typeof(SteadyFormation1StatusEffect),
            typeof(SteadyFormation2StatusEffect),
            typeof(FieldRecovery1StatusEffect),
            typeof(FieldRecovery2StatusEffect),
        };

        public static float GetLeadershipCommandRadius(uint activator)
        {
            return BaseCommandRadius + Stat.GetStatAdjustment(activator, StatType.LeadershipCommandRadiusBonusMeters);
        }

        public static void ToggleVanguardCommandAura(uint activator, Type statusEffectType)
        {
            ToggleLeadershipAura(activator, statusEffectType, VanguardCommandAuraTypes);
        }

        public static void ToggleFieldStewardAura(uint activator, Type statusEffectType)
        {
            ToggleLeadershipAura(activator, statusEffectType, FieldStewardAuraTypes);
        }

        public static float ApplyVanguardCommandDurationBonus(uint activator, float durationSeconds)
        {
            var baseBonus = Stat.GetStatAdjustment(activator, StatType.VanguardCommandDurationBonusBaseSeconds);
            var maximumBonus = Stat.GetStatAdjustment(activator, StatType.VanguardCommandDurationBonusMaximumSeconds);
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

        public static void ApplyTriageProtocol(uint activator, uint target)
        {
            var level = Stat.GetStatAdjustment(activator, StatType.FieldStewardTriageProtocolLevel);
            if (level <= 0)
                return;

            var statusEffect = level >= 2
                ? typeof(TriageProtocol2StatusEffect)
                : typeof(TriageProtocol1StatusEffect);
            var duration = level >= 2 ? 10f : 8f;

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration);
        }

        private static void ToggleLeadershipAura(uint activator, Type statusEffectType, Type[] auraTypes)
        {
            if (!Ability.ToggleAura(activator, statusEffectType))
                return;

            foreach (var auraType in auraTypes)
            {
                if (auraType != statusEffectType)
                    Ability.RemoveAura(activator, auraType);
            }

            Ability.ApplyAura(activator, statusEffectType, true, true, false);
        }
    }
}
