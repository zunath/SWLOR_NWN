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
    public sealed class ForceDrainAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceDrain1(builder);
            ForceDrain2(builder);
            ForceDrain3(builder);

            return builder.Build();
        }

        private static void ForceDrain1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceDrain1, PerkType.ForceDrain)
                .Name("Force Drain I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceDrain, 18f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceDrain1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceDrain2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceDrain2, PerkType.ForceDrain)
                .Name("Force Drain II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceDrain, 18f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceDrain2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceDrain3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceDrain3, PerkType.ForceDrain)
                .Name("Force Drain III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceDrain, 18f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceDrain3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void ForceDrain1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceDrain(activator, target, targetLocation, 14, 30, 40);
        }

        private static void ForceDrain2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceDrain(activator, target, targetLocation, 24, 35, 45);
        }

        private static void ForceDrain3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyForceDrain(activator, target, targetLocation, 36, 40, 50);
        }

        private static void ApplyForceDrain(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int healPercent,
            int lowHPHealPercent)
        {
            var damage = Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);

            if (damage <= 0)
                return;

            ApplyDrainVisual(activator, target);

            var effectiveHealPercent = IsBelowHalfHP(target)
                ? lowHPHealPercent
                : healPercent;
            var healAmount = Math.Max(1, (int)Math.Ceiling(damage * (effectiveHealPercent / 100f)));
            healAmount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, healAmount);
            healAmount = Stat.ApplyHealingReceivedAdjustment(activator, healAmount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(healAmount), activator);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Evil_Help), activator);
        }

        private static bool IsBelowHalfHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * 0.5f;
        }

        private static void ApplyDrainVisual(uint activator, uint target)
        {
            var drainBeam = EffectBeam(VisualEffect.Vfx_Beam_Drain, activator, BodyNode.Hand, true);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Temporary, drainBeam, target, 2.0f));
        }

    }
}
