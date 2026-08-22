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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class GuardianWardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardianWard1(builder);
            GuardianWard2(builder);
            GuardianWard3(builder);
            GuardianWard4(builder);

            return builder.Build();
        }

        private static void GuardianWard1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardianWard1, PerkType.GuardianWard)
                .Name("Guardian Ward I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.GuardianWard, 12f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(GuardianWard1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(2);
        }

        private static void GuardianWard2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardianWard2, PerkType.GuardianWard)
                .Name("Guardian Ward II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.GuardianWard, 12f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(GuardianWard2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void GuardianWard3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardianWard3, PerkType.GuardianWard)
                .Name("Guardian Ward III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.GuardianWard, 12f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(GuardianWard3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void GuardianWard4(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardianWard4, PerkType.GuardianWard)
                .Name("Guardian Ward IV")
                .Level(4)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.GuardianWard, 12f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(GuardianWard4ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void GuardianWard1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, "GUARDIAN_WARD", 6, 30f);
                LightGuardianPowerSupport.ApplyTemporaryHPPowerRiders(activator, friendly, 30f);
            }
        }

        private static void GuardianWard2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, "GUARDIAN_WARD", 9, 30f);
                LightGuardianPowerSupport.ApplyTemporaryHPPowerRiders(activator, friendly, 30f);
            }
        }

        private static void GuardianWard3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, "GUARDIAN_WARD", 12, 30f);
                LightGuardianPowerSupport.ApplyTemporaryHPPowerRiders(activator, friendly, 30f);
            }
        }

        private static void GuardianWard4ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, "GUARDIAN_WARD", 15, 30f);
                LightGuardianPowerSupport.ApplyTemporaryHPPowerRiders(activator, friendly, 30f);
            }
        }
    }
}
