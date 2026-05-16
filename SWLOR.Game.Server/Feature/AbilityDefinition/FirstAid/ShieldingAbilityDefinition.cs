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
    public sealed class ShieldingAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Shielding1(builder);
            Shielding2(builder);
            Shielding3(builder);

            return builder.Build();
        }

        private static void Shielding1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Shielding1, PerkType.Shielding)
                .Name("Shielding I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Shielding1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Shielding2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Shielding2, PerkType.Shielding)
                .Name("Shielding II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Shielding2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Shielding3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Shielding3, PerkType.Shielding)
                .Name("Shielding III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Shielding3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Shielding1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 180f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Shielding1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void Shielding2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 180f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Shielding2StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void Shielding3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 180f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Shielding3StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
