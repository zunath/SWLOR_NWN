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
    public sealed class CryoSprayerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CryoSprayer1(builder);
            CryoSprayer2(builder);

            return builder.Build();
        }

        private static void CryoSprayer1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CryoSprayer1, PerkType.CryoSprayer)
                .Name("Cryo Sprayer I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CryoSprayer, 24f)
                .SkillType(SkillType.Devices)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(CryoSprayer1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void CryoSprayer2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CryoSprayer2, PerkType.CryoSprayer)
                .Name("Cryo Sprayer II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CryoSprayer, 24f)
                .SkillType(SkillType.Devices)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(CryoSprayer2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void CryoSprayer1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                10,
                5,
                typeof(HobbleStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void CryoSprayer2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                14,
                2,
                typeof(ImmobilizedStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

    }
}
