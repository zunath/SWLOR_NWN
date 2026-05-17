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
    public sealed class OverloadBarrageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            OverloadBarrage1(builder);

            return builder.Build();
        }

        private static void OverloadBarrage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.OverloadBarrage1, PerkType.OverloadBarrage)
                .Name("Overload Barrage")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.OverloadBarrage, 120f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(OverloadBarrage1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void OverloadBarrage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var damageAdjustment = DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator);
            var hitChanceAdjustment = DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator);
            var criticalRateAdjustment = DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                12,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Fnf_Fireball,
                damagePercentAdjustment: damageAdjustment,
                hitChancePercentAdjustment: hitChanceAdjustment,
                criticalRatePercentAdjustment: criticalRateAdjustment);

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
                damagePercentAdjustment: damageAdjustment,
                hitChancePercentAdjustment: hitChanceAdjustment,
                criticalRatePercentAdjustment: criticalRateAdjustment);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                12,
                typeof(SonicBurst3StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Sonic,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                damagePercentAdjustment: damageAdjustment,
                afterSuccessfulHit: InterruptActivation,
                hitChancePercentAdjustment: hitChanceAdjustment,
                criticalRatePercentAdjustment: criticalRateAdjustment);
        }

        private static void InterruptActivation(uint target)
        {
            AssignCommand(target, () => ClearAllActions());
        }

    }
}
