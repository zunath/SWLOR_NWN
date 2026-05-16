using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
    public sealed class CoagulantAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Coagulant1(builder);
            Coagulant2(builder);

            return builder.Build();
        }

        private static void Coagulant1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Coagulant1, PerkType.Coagulant)
                .Name("Coagulant I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Coagulant, 45f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Coagulant1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Coagulant2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Coagulant2, PerkType.Coagulant)
                .Name("Coagulant II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Coagulant, 45f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Coagulant2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Coagulant1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 120f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Coagulant1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void Coagulant2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 120f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Coagulant2StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
