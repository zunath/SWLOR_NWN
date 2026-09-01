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
        private const int KnockdownDurationSeconds = 6;
        private const int ForcePush1BaseDamage = 8;
        private const int ForcePush2BaseDamage = 12;
        private const int ForcePush3BaseDamage = 18;
        private const int ForcePush1HobbleDurationSeconds = 12;
        private const int ForcePush2HobbleDurationSeconds = 12;
        private const int ForcePush3HobbleDurationSeconds = 12;
        private const float ForcePush1ConeLengthMeters = 5f;
        private const float ForcePush2ConeLengthMeters = 8f;
        private const float ForcePush3ConeLengthMeters = 10f;
        private const float ForcePushConeWidthMeters = 5f;

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
                .HasRecastDelay(RecastGroup.ForcePush, 45f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .PlaysSoundOnImpact("ksfx_frc_push")
                .IsAreaAbility()
                .HasTargetingCone(
                    Spell.ForcePush1,
                    ForcePush1ConeLengthMeters,
                    ForcePushConeWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
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
                .HasRecastDelay(RecastGroup.ForcePush, 45f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .PlaysSoundOnImpact("ksfx_frc_wave")
                .IsAreaAbility()
                .HasTargetingCone(
                    Spell.ForcePush2,
                    ForcePush2ConeLengthMeters,
                    ForcePushConeWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
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
                .HasRecastDelay(RecastGroup.ForcePush, 45f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .PlaysSoundOnImpact("ksfx_frc_wave")
                .IsAreaAbility()
                .HasTargetingCone(
                    Spell.ForcePush3,
                    ForcePush3ConeLengthMeters,
                    ForcePushConeWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction(ForcePush3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForcePush1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                ForcePush1BaseDamage,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                ForcePush1ConeLengthMeters,
                ForcePushConeWidthMeters,
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst_Silent,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 1,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush1HobbleDurationSeconds),
                playImpactAnimation: false,
                useUnscaledDamage: true);
        }

        private static void ForcePush2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                ForcePush2BaseDamage,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                ForcePush2ConeLengthMeters,
                ForcePushConeWidthMeters,
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst_Silent,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 2,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush2HobbleDurationSeconds),
                playImpactAnimation: false,
                useUnscaledDamage: true);
        }

        private static void ForcePush3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                ForcePush3BaseDamage,
                KnockdownDurationSeconds,
                typeof(KnockdownStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                ForcePush3ConeLengthMeters,
                ForcePushConeWidthMeters,
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst_Silent,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 3,
                afterSuccessfulHit: hitTarget => ApplyHobble(activator, hitTarget, ForcePush3HobbleDurationSeconds),
                playImpactAnimation: false,
                useUnscaledDamage: true);
        }

        private static void ApplyHobble(uint activator, uint target, int durationSeconds)
        {
            StatusEffect.ApplyStatusEffect(activator, target, typeof(HobbleStatusEffect), durationSeconds, CombatDamageType.Force);
        }

    }
}
