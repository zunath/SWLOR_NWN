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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class SonicBurstAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SonicBurst1(builder);
            SonicBurst2(builder);
            SonicBurst3(builder);

            return builder.Build();
        }

        private static void SonicBurst1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SonicBurst1, PerkType.SonicBurst)
                .Name("Sonic Burst I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.SonicBurst, 18f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_sonic_wave")
                .IsAreaAbility()
                .HasImpactAction(SonicBurst1ImpactAction)
                .HasTargetingSphere(
                    Spell.SonicBurst1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void SonicBurst2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SonicBurst2, PerkType.SonicBurst)
                .Name("Sonic Burst II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.SonicBurst, 18f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_sonic_wave")
                .IsAreaAbility()
                .HasImpactAction(SonicBurst2ImpactAction)
                .HasTargetingSphere(
                    Spell.SonicBurst2,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void SonicBurst3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SonicBurst3, PerkType.SonicBurst)
                .Name("Sonic Burst III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.SonicBurst, 18f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_sonic_wave")
                .IsAreaAbility()
                .HasImpactAction(SonicBurst3ImpactAction)
                .HasTargetingSphere(
                    Spell.SonicBurst3,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void SonicBurst1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                10,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Sonic,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: hitTarget =>
                {
                    InterruptActivation(hitTarget);
                    DeviceAbilityEffects.ApplyTacticalUplink(activator);
                },
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void SonicBurst2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                14,
                30,
                typeof(SonicBurst2StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Sonic,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: hitTarget =>
                {
                    InterruptActivation(hitTarget);
                    DeviceAbilityEffects.ApplyTacticalUplink(activator);
                },
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void SonicBurst3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                30,
                typeof(SonicBurst3StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Sonic,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: hitTarget =>
                {
                    InterruptActivation(hitTarget);
                    DeviceAbilityEffects.ApplyTacticalUplink(activator);
                },
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void InterruptActivation(uint target)
        {
            AssignCommand(target, () => ClearAllActions());
        }

    }
}
