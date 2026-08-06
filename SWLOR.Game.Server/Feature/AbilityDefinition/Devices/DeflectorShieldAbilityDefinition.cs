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
                .Name("Deflector Shield I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
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
                .Name("Deflector Shield II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
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
                .Name("Deflector Shield III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_act_shield")
                .HasRecastDelay(RecastGroup.DeflectorShield, 24f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
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
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyShieldTemporaryHP(activator, friendly, 35, 6, 45f);
            }
        }

        private static void DeflectorShield2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyShieldTemporaryHP(activator, friendly, 65, 9, 45f);
            }
        }

        private static void DeflectorShield3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                ApplyShieldTemporaryHP(activator, friendly, 100, 12, 45f);
            }
        }

        private static void ApplyShieldTemporaryHP(
            uint activator,
            uint target,
            int flatAmount,
            int percent,
            float durationSeconds)
        {
            var amount = Math.Max(1, flatAmount + GameMath.PercentOf(GetMaxHitPoints(target), percent));
            amount = Ability.ApplyCombatReadinessMagnitude(activator, amount);
            var duration = durationSeconds;

            TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(target, "DEFLECTOR_SHIELD", amount, duration);
            DeviceAbilityEffects.ApplyFieldSupportAllyBuffRiders(activator, target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);
        }
    }
}
