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
    public sealed class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        private const int KnockdownDurationSeconds = 2;
        private const int ForcePush1HobbleDurationSeconds = 3;
        private const int ForcePush2HobbleDurationSeconds = 3;
        private const int ForcePush3HobbleDurationSeconds = 4;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);

            return builder.Build();
        }

        private static void ForcePush1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsSingleTargetAbility()
                .HasMaxRange(8f)
                .RequiresTarget()
                .HasImpactAction(ForcePush1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(2);
        }

        private static void ForcePush2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsAreaAbility()
                .HasImpactAction(ForcePush2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void ForcePush3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForcePush, 24f)
                .SkillType(SkillType.Force)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsAreaAbility()
                .HasImpactAction(ForcePush3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForcePush1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                false,
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush1HobbleDurationSeconds),
                playImpactAnimation: false);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForcePush2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Line,
                0f,
                8f,
                2.5f,
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 2,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush2HobbleDurationSeconds),
                playImpactAnimation: false);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForcePush3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                maxTargets: 3,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush3HobbleDurationSeconds),
                playImpactAnimation: false);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ApplyHobble(uint activator, uint target, int durationSeconds)
        {
            StatusEffect.ApplyStatusEffect(activator, target, typeof(HobbleStatusEffect), durationSeconds, CombatDamageType.Force);
        }

    }
}
