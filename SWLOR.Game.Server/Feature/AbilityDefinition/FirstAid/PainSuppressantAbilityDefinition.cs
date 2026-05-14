using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class PainSuppressantAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PainSuppressant1(builder);
            PainSuppressant2(builder);

            return builder.Build();
        }

        private static void PainSuppressant1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PainSuppressant1, PerkType.PainSuppressant)
                .Name("Pain Suppressant I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.PainSuppressant, 60f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PainSuppressant1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void PainSuppressant2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PainSuppressant2, PerkType.PainSuppressant)
                .Name("Pain Suppressant II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.PainSuppressant, 60f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PainSuppressant2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void PainSuppressant1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 18f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, SkillType.FirstAid, 10);
                ApplyTemporaryHP(friendly, 10, duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
        }

        private static void PainSuppressant2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 18f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, SkillType.FirstAid, 15);
                ApplyTemporaryHP(friendly, 15, duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant2StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
        }


        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)
        {
            var ability = skill switch
            {
                SkillType.Leadership => AbilityType.Social,
                SkillType.Devices => AbilityType.Perception,
                SkillType.BeastMastery => AbilityType.Might,
                _ => AbilityType.Willpower
            };
            var baseAmount = PercentOf(GetMaxHitPoints(target), percent);
            var amount = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(baseAmount, GetAbilityScore(activator, ability));
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectTemporaryHitpoints(PercentOf(GetMaxHitPoints(target), percent)),
                target,
                durationSeconds);
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
