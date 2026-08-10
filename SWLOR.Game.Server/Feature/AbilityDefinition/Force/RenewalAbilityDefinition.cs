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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class RenewalAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Renewal1(builder);
            Renewal2(builder);
            Renewal3(builder);

            return builder.Build();
        }

        private static void Renewal1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal1, PerkType.Renewal)
                .Name("Renewal I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Renewal, 15f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void Renewal2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal2, PerkType.Renewal)
                .Name("Renewal II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Renewal, 15f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void Renewal3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal3, PerkType.Renewal)
                .Name("Renewal III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Renewal, 15f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void Renewal1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal I", 20f);
        }

        private static void Renewal2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal II", 40f);
        }

        private static void Renewal3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal III", 60f);
        }

        private static void ApplyRenewal(uint activator, uint target, string name, float totalPercent)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            var targetWasBelowHalfHP = ForceControlHealingEffects.IsBelowHalfHP(friendly);
            var affinityAdjustedTotalPercent = totalPercent * Ability.GetActiveForceAffinityMagnitudeMultiplier(activator);
            StatusEffect.ApplyStatusEffect(
                activator,
                friendly,
                new RegenerativeHealingStatusEffect(name, affinityAdjustedTotalPercent, 10),
                30f);
            ForceControlHealingEffects.ApplyRestorativeControlPower(activator, friendly, targetWasBelowHalfHP);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
        }
    }
}
