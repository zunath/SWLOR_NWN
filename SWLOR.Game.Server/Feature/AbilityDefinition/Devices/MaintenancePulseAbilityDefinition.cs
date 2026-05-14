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
                .IsAreaAbility()
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
                .IsAreaAbility()
                .HasImpactAction(MaintenancePulse2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void MaintenancePulse1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, SkillType.Devices, 12);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
        }

        private static void MaintenancePulse2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                foreach (var statusEffect in new[] { typeof(ShockStatusEffect) })
                {
                    StatusEffect.RemoveStatusEffect(friendly, statusEffect, false);
                }

                HealPercent(activator, friendly, SkillType.Devices, 20);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }


        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)
        {
            var ability = skill switch
            {
                SkillType.Leadership => AbilityType.Social,
                SkillType.Devices => AbilityType.Perception,
                SkillType.BeastMastery => AbilityType.Might,
                _ => AbilityType.Willpower
            };
            var baseAmount = PercentOf(GetMaxHitPoints(target), percent);
            var amount = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(baseAmount, GetAbilityScore(activator, ability));
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
