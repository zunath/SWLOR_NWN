using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class AdrenalStimAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AdrenalStim1(builder);
            AdrenalStim2(builder);
            AdrenalStim3(builder);

            return builder.Build();
        }

        private static void AdrenalStim1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AdrenalStim1, PerkType.AdrenalStim)
                .Name("Adrenal Stim I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AdrenalStim, 120f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(AdrenalStim1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void AdrenalStim2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AdrenalStim2, PerkType.AdrenalStim)
                .Name("Adrenal Stim II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AdrenalStim, 120f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(AdrenalStim2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void AdrenalStim3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AdrenalStim3, PerkType.AdrenalStim)
                .Name("Adrenal Stim III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AdrenalStim, 120f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(AdrenalStim3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void AdrenalStim1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 12f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, PercentOf(Stat.GetMaxStamina(friendly), 10));
                StatusEffect.ApplyStatusEffect(activator, friendly, new AdrenalStimStatusEffect(1), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
            }
        }

        private static void AdrenalStim2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 12f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, PercentOf(Stat.GetMaxStamina(friendly), 18));
                StatusEffect.ApplyStatusEffect(activator, friendly, new AdrenalStimStatusEffect(1), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
        }

        private static void AdrenalStim3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 12f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, PercentOf(Stat.GetMaxStamina(friendly), 25));
                StatusEffect.ApplyStatusEffect(activator, friendly, new AdrenalStimStatusEffect(1), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
        }


        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
