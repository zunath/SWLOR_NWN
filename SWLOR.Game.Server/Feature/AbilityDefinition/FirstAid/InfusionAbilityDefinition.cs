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
    public sealed class InfusionAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Infusion1(builder);
            Infusion2(builder);

            return builder.Build();
        }

        private static void Infusion1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Infusion1, PerkType.Infusion)
                .Name("Infusion I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Infusion, 24f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Infusion1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("med_supplies");
        }

        private static void Infusion2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Infusion2, PerkType.Infusion)
                .Name("Infusion II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Infusion, 24f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Infusion2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8)
                .RequirementItem("med_supplies");
        }

        private static void Infusion1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInfusion(activator, target, "Infusion I", 15f);
        }

        private static void Infusion2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyInfusion(activator, target, "Infusion II", 25f);
        }

        private static void ApplyInfusion(uint activator, uint target, string name, float totalPercent)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            StatusEffect.ApplyStatusEffect(
                activator,
                friendly,
                new RegenerativeHealingStatusEffect(name, totalPercent, 5, true),
                30f);
            FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
            FirstAidTreatmentAdjustments.ApplyMedicalVisualEffect(friendly);
            FirstAidTreatmentAdjustments.GrantCombatPoint(activator);
        }

    }
}
