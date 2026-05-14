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
    public sealed class GroupDeflectorAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GroupDeflector1(builder);

            return builder.Build();
        }

        private static void GroupDeflector1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GroupDeflector1, PerkType.GroupDeflector)
                .Name("Group Deflector")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.GroupDeflector, 90f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(GroupDeflector1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GroupDeflector1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                HealPercent(activator, friendly, SkillType.Devices, 8);
                ApplyTemporaryHP(friendly, 8, 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
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
