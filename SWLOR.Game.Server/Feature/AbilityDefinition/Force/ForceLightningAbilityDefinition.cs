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
    public sealed class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);

            return builder.Build();
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceLightning, 15f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_lightn")
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLightning1ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceLightning1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceLightning, 15f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_lightn")
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLightning2ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceLightning2,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceLightning3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceLightning, 15f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_lightn")
                .IsAreaAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLightning3ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceLightning3,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void ForceLightning1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceLightning(activator, target, targetLocation, 10, 30, 1, 2);
        }

        private static void ForceLightning2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceLightning(activator, target, targetLocation, 18, 30, 2, 3);
        }

        private static void ForceLightning3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceLightning(activator, target, targetLocation, 40, 30, 3, 3);
        }

        private static void ApplyForceLightning(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int shockDuration,
            int shockLevel,
            int maxArcTargets)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                shockDuration,
                typeof(ShockStatusEffect),
                false,
                Array.Empty<Type>(),
                statusEffectFactory: () => new ShockStatusEffect(shockLevel),
                damageType: CombatDamageType.Force,
                effectDamageType: DamageType.Electrical,
                afterSuccessfulHit: hitTarget => ApplyLightningHitEffects(activator, hitTarget));

            var center = GetIsObjectValid(target)
                ? GetLocation(target)
                : targetLocation;

            foreach (var arcTarget in AbilityTargeting.GetHostileTargetsNearLocation(
                         activator,
                         center,
                         5f,
                         maxArcTargets,
                         predicate: candidate => candidate != target))
            {
                Ability.ApplyCombatImpact(
                    activator,
                    arcTarget,
                    GetLocation(arcTarget),
                    SkillType.Force,
                    baseDamage,
                    shockDuration,
                    typeof(ShockStatusEffect),
                    false,
                    Array.Empty<Type>(),
                    statusEffectFactory: () => new ShockStatusEffect(shockLevel),
                    damageType: CombatDamageType.Force,
                    effectDamageType: DamageType.Electrical,
                    damagePercentAdjustment: _ => -50,
                    afterSuccessfulHit: hitTarget => ApplyLightningHitEffects(activator, hitTarget),
                    playImpactAnimation: false);
            }
        }

        private static void ApplyLightningHitEffects(uint activator, uint target)
        {
            var lightningBeam = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand);
            var lightningBurst = EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, lightningBeam, target, 2.5f);
                ApplyEffectToObject(DurationType.Instant, lightningBurst, target);
            });
            ForcePressureEffects.ApplyUnstablePressure(activator, target);
        }

    }
}
