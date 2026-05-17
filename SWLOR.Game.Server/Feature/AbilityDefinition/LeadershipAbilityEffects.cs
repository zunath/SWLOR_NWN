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

        public static float GetLeadershipCommandRadius(uint activator)
        {
            return BaseCommandRadius + Stat.GetStatAdjustment(activator, StatType.LeadershipCommandRadiusBonusMeters);
        }

        public static bool ToggleVanguardCommandAura(uint activator, StatType auraLevelStatType, params Type[] statusEffectTypes)
        {
            return ToggleLeadershipAura(activator, auraLevelStatType, statusEffectTypes);
        }

        public static bool ToggleFieldStewardAura(uint activator, StatType auraLevelStatType, params Type[] statusEffectTypes)
        {
            return ToggleLeadershipAura(activator, auraLevelStatType, statusEffectTypes);
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

        private static bool ToggleLeadershipAura(uint activator, StatType auraLevelStatType, Type[] statusEffectTypes)
        {
            var auraLevel = Stat.GetStatAdjustment(activator, auraLevelStatType);
            if (auraLevel <= 0 || statusEffectTypes.Length <= 0)
                return false;

            var statusEffectType = statusEffectTypes[Math.Clamp(auraLevel, 1, statusEffectTypes.Length) - 1];

            if (!Ability.ToggleAura(activator, statusEffectType))
                return false;

            foreach (var auraType in statusEffectTypes)
            {
                if (auraType != statusEffectType)
                    Ability.RemoveAura(activator, auraType);
            }

            Ability.ApplyAura(activator, statusEffectType, true, true, false);
            return true;
        }
    }
}
