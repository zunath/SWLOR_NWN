using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class FocusStimAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FocusStim1(builder);
            FocusStim2(builder);

            return builder.Build();
        }

        private static void FocusStim1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FocusStim1, PerkType.FocusStim)
                .Name("Focus Stim I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_frc_buff")
                .HasRecastDelay(RecastGroup.FocusStim, 45f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(FocusStim1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("stim_pack", preserveChanceStatType: StatType.StimPackPreserveChance);
        }

        private static void FocusStim2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FocusStim2, PerkType.FocusStim)
                .Name("Focus Stim II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_frc_buff")
                .HasRecastDelay(RecastGroup.FocusStim, 45f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(FocusStim2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("stim_pack", preserveChanceStatType: StatType.StimPackPreserveChance);
        }

        private static void FocusStim1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 120f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(FocusStim1StatusEffect), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void FocusStim2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 120f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(FocusStim2StatusEffect), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
