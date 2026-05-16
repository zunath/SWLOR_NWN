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
    public sealed class TreatmentKitAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TreatmentKit1(builder);
            TreatmentKit2(builder);
            TreatmentKit3(builder);

            return builder.Build();
        }

        private static void TreatmentKit1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TreatmentKit1, PerkType.TreatmentKit)
                .Name("Treatment Kit I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.TreatmentKit, 8f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(TreatmentKit1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("med_supplies");
        }

        private static void TreatmentKit2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TreatmentKit2, PerkType.TreatmentKit)
                .Name("Treatment Kit II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.TreatmentKit, 8f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(TreatmentKit2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("med_supplies");
        }

        private static void TreatmentKit3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TreatmentKit3, PerkType.TreatmentKit)
                .Name("Treatment Kit III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.TreatmentKit, 18f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(TreatmentKit3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void TreatmentKit1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                foreach (var statusEffect in new[] { typeof(PoisonStatusEffect), typeof(BleedStatusEffect) })
                    StatusEffect.RemoveStatusEffect(friendly, statusEffect, false);

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }

        private static void TreatmentKit2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveCleanseableStatusEffects(friendly, StatusEffectCleanseType.TreatmentKit2, false);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }

        private static void TreatmentKit3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveCleanseableStatusEffects(friendly, StatusEffectCleanseType.TreatmentKit2, false);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(TreatmentKit3StatusEffect), 8f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }


    }
}
