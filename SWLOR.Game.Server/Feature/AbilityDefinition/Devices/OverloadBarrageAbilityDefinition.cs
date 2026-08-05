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
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(OverloadBarrage1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void OverloadBarrage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var damageAdjustment = DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator);
            var baseDamageAdjustment = DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator);
            var hitChanceAdjustment = DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator);
            var criticalRateAdjustment = DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator);
            var duration = (int)CapstoneAbility.ActiveDurationSeconds;
            var blastRadius = DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 5f);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                duration,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                blastRadius,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Fnf_Fireball,
                damagePercentAdjustment: damageAdjustment,
                baseDamageAdjustment: baseDamageAdjustment,
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
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
                baseDamageAdjustment: baseDamageAdjustment,
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
                hitChancePercentAdjustment: hitChanceAdjustment,
                criticalRatePercentAdjustment: criticalRateAdjustment);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                duration,
                typeof(SonicBurst3StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                blastRadius,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Sonic,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                damagePercentAdjustment: damageAdjustment,
                baseDamageAdjustment: baseDamageAdjustment,
                afterSuccessfulHit: hitTarget =>
                {
                    InterruptActivation(hitTarget);
                    DeviceAbilityEffects.ApplyTacticalUplink(activator);
                },
                hitChancePercentAdjustment: hitChanceAdjustment,
                criticalRatePercentAdjustment: criticalRateAdjustment);
        }

        private static void InterruptActivation(uint target)
        {
            AssignCommand(target, () => ClearAllActions());
        }

    }
}
