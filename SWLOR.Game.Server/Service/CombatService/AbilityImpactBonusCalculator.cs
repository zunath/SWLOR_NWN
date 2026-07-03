using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityImpactBonusCalculator
    {
        public static int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !GetIsObjectValid(activator))
                return 0;

            var bonus = 0;

            switch (skillType)
            {
                case SkillType.Lightsaber:
                    if (ability.IsAreaAbility)
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseAreaDamageBonus);
                    }

                    if (ability.IsSingleTargetAbility &&
                        GetIsObjectValid(target) &&
                        StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDebuffedTargetDamageBonus);
                    }
                    break;
                case SkillType.Vibroknife when ability.IsHostileAbility:
                    var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
                    if (toxicCoatingRank > 0)
                    {
                        bonus += toxicCoatingRank >= 2 ? 22 : 10;
                    }
                    break;
                case SkillType.Staff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDamageBonus);
                    break;
                case SkillType.Saberstaff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareDamageBonus);
                    break;
                case SkillType.TwinBlade when ability.IsHostileAbility &&
                    AbilityImpactEffects.AbilityMatchesReversalCutTrigger(activator, ability):
                    bonus += TemporaryStatModifier.Consume(
                        activator,
                        StatType.TwinBladeDuelistReversalCutDamageBonus,
                        StatType.TwinBladeDuelistReversalCut);
                    break;
            }

            bonus += GuardDeflection.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(activator, skillType);

            if (ability.IsHostileAbility &&
                AbilityImpactEffects.IsCurrentFPAndStaminaAtOrAbovePercent(
                    activator,
                    Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent)))
            {
                bonus += Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonus);
            }

            return bonus;
        }

        public static int GetCostlyAbilityDamageBonus(uint activator, SkillType skillType)
        {
            if (!CombatState.TryGetRecentAbilityStaminaCost(activator, 10f, out var staminaCost))
                return 0;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageBonusSkillType));
            var minimumCost = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitMinimumStaminaCost);
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                staminaCost < minimumCost)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(activator, StatType.CostlyAbilityDamageBonus);
        }

    }
}
