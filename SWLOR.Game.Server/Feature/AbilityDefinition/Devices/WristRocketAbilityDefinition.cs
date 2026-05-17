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
    public sealed class WristRocketAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WristRocket1(builder);
            WristRocket2(builder);
            WristRocket3(builder);

            return builder.Build();
        }

        private static void WristRocket1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WristRocket1, PerkType.WristRocket)
                .Name("Wrist Rocket I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WristRocket, 18f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WristRocket1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(2);
        }

        private static void WristRocket2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WristRocket2, PerkType.WristRocket)
                .Name("Wrist Rocket II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WristRocket, 18f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WristRocket2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void WristRocket3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WristRocket3, PerkType.WristRocket)
                .Name("Wrist Rocket III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WristRocket, 18f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WristRocket3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void WristRocket1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                12,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void WristRocket2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                16,
                2,
                typeof(KnockdownStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void WristRocket3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                20,
                3,
                typeof(KnockdownStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

    }
}
