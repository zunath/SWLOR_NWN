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
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(HoldTheLine1ImpactAction)
                .HasTargetingSphere(
                    Spell.HoldTheLine1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void HoldTheLine1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var affectedCount = 0;

            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 18, 22),
                    CapstoneAbility.ActiveDurationSeconds);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(HoldTheLine1StatusEffect), CapstoneAbility.ActiveDurationSeconds);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
                affectedCount++;
            }

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 3);
        }

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            TemporaryHitPointEffects.ApplyFlat(
                target,
                "HOLD_THE_LINE",
                GameMath.PercentOf(GetMaxHitPoints(target), percent),
                durationSeconds);
        }
    }
}
