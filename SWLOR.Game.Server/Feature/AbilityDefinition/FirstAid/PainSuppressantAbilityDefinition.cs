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
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.PainSuppressant, 30f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(PainSuppressant1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("stim_pack", preserveChanceStatType: StatType.StimPackPreserveChance);
        }

        private static void PainSuppressant2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PainSuppressant2, PerkType.PainSuppressant)
                .Name("Pain Suppressant II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.PainSuppressant, 30f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(PainSuppressant2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("stim_pack", preserveChanceStatType: StatType.StimPackPreserveChance);
        }

        private static void PainSuppressant1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 30f);
            var applied = false;
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyTemporaryHP(activator, friendly, 10, duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant1StatusEffect), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(friendly);
                applied = true;
            }

            FirstAidTreatmentAdjustments.GrantCombatPointIfApplied(activator, applied);
        }

        private static void PainSuppressant2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 30f);
            var applied = false;
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyTemporaryHP(activator, friendly, 15, duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant2StatusEffect), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(friendly);
                applied = true;
            }

            FirstAidTreatmentAdjustments.GrantCombatPointIfApplied(activator, applied);
        }


        private static void ApplyTemporaryHP(uint activator, uint target, int percent, float durationSeconds)
        {
            AbilityEffectScaling.ApplyTemporaryHPPercent(activator, target, "PAIN_SUPPRESSANT", percent, durationSeconds);
        }
    }
}
