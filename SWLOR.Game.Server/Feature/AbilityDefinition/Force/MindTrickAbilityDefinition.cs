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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class MindTrickAbilityDefinition : IAbilityListDefinition
    {
        private const float Radius = 5f;
        private const int MindTrick2MaxTargets = 2;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MindTrick1(builder);
            MindTrick2(builder);

            return builder.Build();
        }

        private static void MindTrick1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MindTrick1, PerkType.MindTrick)
                .Name("Mind Trick I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((_, target, _, _) => ValidateNonMechanicalTarget(target))
                .HasImpactAction(MindTrick1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void MindTrick2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MindTrick2, PerkType.MindTrick)
                .Name("Mind Trick II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MindTrick, 60f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((_, target, _, _) => ValidateNonMechanicalTarget(target))
                .HasImpactAction(MindTrick2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void MindTrick1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                5,
                typeof(FoggyMindStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
        }

        private static void MindTrick2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            foreach (var hostileTarget in AbilityTargeting.GetHostileTargetsNearLocation(activator, impactLocation, Radius, MindTrick2MaxTargets, target, IsNonMechanical))
            {
                Ability.ApplyCombatImpact(
                    activator,
                    hostileTarget,
                    GetLocation(hostileTarget),
                    SkillType.Force,
                    0,
                    5,
                    typeof(FoggyMindStatusEffect),
                    false,
                    Array.Empty<Type>(),
                    damageType: CombatDamageType.Force,
                    targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            }
        }

        private static bool IsNonMechanical(uint target)
        {
            var racialType = GetRacialType(target);
            return racialType != RacialType.Construct &&
                   racialType != RacialType.Robot &&
                   racialType != RacialType.Droid;
        }

        private static string ValidateNonMechanicalTarget(uint target)
        {
            return IsNonMechanical(target)
                ? string.Empty
                : "This ability cannot affect mechanical targets.";
        }

    }
}
