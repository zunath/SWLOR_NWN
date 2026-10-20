using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
    public sealed class DeflectorShieldAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DeflectorShield1(builder);
            DeflectorShield2(builder);
            DeflectorShield3(builder);

            return builder.Build();
        }

        private static void DeflectorShield1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DeflectorShield1, PerkType.DeflectorShield)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_DeflectorShield)
                .UsesImmediateAuthoredAnimation()
                .Name("Deflector Shield I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(DeflectorShield1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void DeflectorShield2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DeflectorShield2, PerkType.DeflectorShield)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_DeflectorShield)
                .UsesImmediateAuthoredAnimation()
                .Name("Deflector Shield II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(DeflectorShield2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void DeflectorShield3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DeflectorShield3, PerkType.DeflectorShield)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_DeflectorShield)
                .UsesImmediateAuthoredAnimation()
                .Name("Deflector Shield III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .AllowsSelfQuickCast()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(DeflectorShield3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void DeflectorShield1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDeflectorShield(activator, target, new HealAmount(41, 1.5f));
        }

        private static void DeflectorShield2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDeflectorShield(activator, target, new HealAmount(85, 2.25f));
        }

        private static void DeflectorShield3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyDeflectorShield(activator, target, new HealAmount(154, 3f));
        }

        private static void ApplyDeflectorShield(uint activator, uint target, HealAmount shield)
        {
            var shielded = new List<uint>();
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyShieldTemporaryHP(activator, friendly, shield, 45f);
                shielded.Add(friendly);
            }

            if (shielded.Count > 0)
                CombatPoint.AddSupportCombatPoint(activator, shielded, SkillType.Devices);
        }

        private static void ApplyShieldTemporaryHP(
            uint activator,
            uint target,
            HealAmount shield,
            float durationSeconds)
        {
            var amount = shield.ForMaxHitPoints(GetMaxHitPoints(target));
            amount = Ability.ApplyCombatReadinessMagnitude(activator, amount);
            var duration = durationSeconds;

            TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(target, "DEFLECTOR_SHIELD", amount, duration);
            Ability.PlaySuccessfulImpactVisualEffect(activator, target);
            DeviceAbilityEffects.ApplyFieldSupportAllyBuffRiders(activator, target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);
        }
    }
}
