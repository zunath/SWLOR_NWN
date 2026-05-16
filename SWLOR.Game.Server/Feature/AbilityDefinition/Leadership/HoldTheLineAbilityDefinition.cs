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
    public sealed class HoldTheLineAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            HoldTheLine1(builder);

            return builder.Build();
        }

        private static void HoldTheLine1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.HoldTheLine1, PerkType.HoldTheLine)
                .Name("Hold the Line")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(HoldTheLine1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(15);
        }

        private static void HoldTheLine1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var affectedCount = 0;

            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                const float duration = 20f;
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 25, 30),
                    duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(HoldTheLine1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 3);
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
