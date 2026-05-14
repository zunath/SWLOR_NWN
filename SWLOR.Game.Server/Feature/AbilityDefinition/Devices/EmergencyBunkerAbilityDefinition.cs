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
    public sealed class EmergencyBunkerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EmergencyBunker1(builder);

            return builder.Build();
        }

        private static void EmergencyBunker1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EmergencyBunker1, PerkType.EmergencyBunker)
                .Name("Emergency Bunker")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.EmergencyBunker, 180f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(EmergencyBunker1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void EmergencyBunker1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            var duration = DeviceAbilityEffects.ApplyCapacitorRigDurationBonus(activator, 15f);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, 4f))
            {
                var temporaryHP = 120 + (int)Math.Ceiling(GetMaxHitPoints(friendly) * 0.10f);
                temporaryHP = DeviceAbilityEffects.ApplyCapacitorRigBonus(activator, temporaryHP);
                TemporaryHitPointEffects.ApplyFlat(friendly, temporaryHP, duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }

            DeviceAbilityEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                4f,
                duration,
                typeof(EmergencyBunker1StatusEffect),
                VisualEffect.Vfx_Imp_Ac_Bonus);
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

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectTemporaryHitpoints(PercentOf(GetMaxHitPoints(target), percent)),
                target,
                durationSeconds);
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
