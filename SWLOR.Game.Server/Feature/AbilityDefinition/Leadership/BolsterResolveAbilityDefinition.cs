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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class BolsterResolveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BolsterResolve1(builder);
            BolsterResolve2(builder);

            return builder.Build();
        }

        private static void BolsterResolve1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BolsterResolve1, PerkType.BolsterResolve)
                .Name("Bolster Resolve I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.BolsterResolve, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BolsterResolve1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void BolsterResolve2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BolsterResolve2, PerkType.BolsterResolve)
                .Name("Bolster Resolve II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.BolsterResolve, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BolsterResolve2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void BolsterResolve1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyFieldStewardCommandDurationBonus(activator, 12f);
            var affectedCount = 0;

            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 8, 10),
                    duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly, duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void BolsterResolve2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyFieldStewardCommandDurationBonus(activator, 15f);
            var affectedCount = 0;

            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 12, 15),
                    duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(BolsterResolve2StatusEffect), duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly, duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
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
