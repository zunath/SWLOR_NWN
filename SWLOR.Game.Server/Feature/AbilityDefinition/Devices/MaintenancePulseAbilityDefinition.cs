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
    public sealed class MaintenancePulseAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MaintenancePulse1(builder);
            MaintenancePulse2(builder);

            return builder.Build();
        }

        private static void MaintenancePulse1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MaintenancePulse1, PerkType.MaintenancePulse)
                .Name("Maintenance Pulse I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MaintenancePulse, 18f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) => ValidateMaintenanceTarget(activator, target))
                .HasImpactAction(MaintenancePulse1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void MaintenancePulse2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MaintenancePulse2, PerkType.MaintenancePulse)
                .Name("Maintenance Pulse II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MaintenancePulse, 18f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) => ValidateMaintenanceTarget(activator, target))
                .HasImpactAction(MaintenancePulse2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void MaintenancePulse1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMaintenancePulse(activator, target, 12, 3f, false);
        }

        private static void MaintenancePulse2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyMaintenancePulse(activator, target, 20, 5f, true);
        }

        private static string ValidateMaintenanceTarget(uint activator, uint target)
        {
            var validation = AbilityTargeting.ValidateFriendlyTarget(activator, target);
            if (!string.IsNullOrWhiteSpace(validation))
                return validation;

            return IsMechanicalTarget(target)
                ? string.Empty
                : "Maintenance Pulse can only target friendly droids or mechanical allies.";
        }

        private static void ApplyMaintenancePulse(
            uint activator,
            uint target,
            int healPercent,
            float extensionSeconds,
            bool removeShock)
        {
            if (!string.IsNullOrWhiteSpace(ValidateMaintenanceTarget(activator, target)))
                return;

            if (removeShock)
            {
                StatusEffect.RemoveStatusEffect(target, typeof(ShockStatusEffect), false);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), target);
            }

            HealPercent(target, healPercent);
            DeviceAbilityEffects.ExtendActiveFieldEngineerPulses(activator, extensionSeconds);
        }

        private static bool IsMechanicalTarget(uint target)
        {
            var race = GetRacialType(target);
            return Droid.IsDroid(target) ||
                   race == RacialType.Droid ||
                   race == RacialType.Construct;
        }


        private static void HealPercent(uint target, int percent)
        {
            var amount = PercentOf(GetMaxHitPoints(target), percent);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
