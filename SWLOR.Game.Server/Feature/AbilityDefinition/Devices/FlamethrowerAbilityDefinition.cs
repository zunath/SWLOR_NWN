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
    public sealed class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private const VisualEffect FlamethrowerVisualEffect = VisualEffect.Vfx_Flamethrower;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Flamethrower1(builder);
            Flamethrower2(builder);
            Flamethrower3(builder);

            return builder.Build();
        }

        private static void Flamethrower1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Flamethrower1, PerkType.Flamethrower)
                .Name("Flamethrower I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(Flamethrower1ImpactAction)
                .HasTargetingCone(
                    Spell.Flamethrower1,
                    6f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void Flamethrower2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Flamethrower2, PerkType.Flamethrower)
                .Name("Flamethrower II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(Flamethrower2ImpactAction)
                .HasTargetingCone(
                    Spell.Flamethrower2,
                    6f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void Flamethrower3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Flamethrower3, PerkType.Flamethrower)
                .Name("Flamethrower III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(Flamethrower3ImpactAction)
                .HasTargetingCone(
                    Spell.Flamethrower3,
                    6f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Flamethrower1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            PlayFlamethrowerVisualEffect(activator);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                10,
                12,
                null,
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void Flamethrower2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            PlayFlamethrowerVisualEffect(activator);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                14,
                12,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void Flamethrower3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            PlayFlamethrowerVisualEffect(activator);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                12,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void PlayFlamethrowerVisualEffect(uint activator)
        {
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectVisualEffect(FlamethrowerVisualEffect),
                activator,
                2f);
        }

    }
}
