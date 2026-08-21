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
    public sealed class CleanseOrderAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CleanseOrder1(builder);
            CleanseOrder2(builder);

            return builder.Build();
        }

        private static void CleanseOrder1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CleanseOrder1, PerkType.CleanseOrder)
                .Name("Cleanse Order I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.CleanseOrder, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CleanseOrder1ImpactAction)
                .HasTargetingSphere(
                    Spell.CleanseOrder1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void CleanseOrder2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CleanseOrder2, PerkType.CleanseOrder)
                .Name("Cleanse Order II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.CleanseOrder, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CleanseOrder2ImpactAction)
                .HasTargetingSphere(
                    Spell.CleanseOrder2,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void CleanseOrder1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyFieldStewardCommandDurationBonus(activator, 30f);
            var affectedCount = 0;

            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.TreatmentKit2, false);
                StatusEffect.RemoveStatusEffect(friendly, typeof(CleanseOrder2StatusEffect), false);
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 6, 8),
                    duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly, duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
                affectedCount++;
            }

            LeadershipAbilityEffects.ApplyBolsterResolve(activator, duration);

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void CleanseOrder2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyFieldStewardCommandDurationBonus(activator, 30f);
            var affectedCount = 0;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.Purify, false);
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    typeof(CleanseOrder2StatusEffect),
                    duration);
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 12, 15),
                    duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly, duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
                affectedCount++;
            }

            LeadershipAbilityEffects.ApplyBolsterResolve(activator, duration);

            if (affectedCount > 0) CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            TemporaryHitPointEffects.ApplyFlat(
                target,
                CleanseOrder2StatusEffect.TemporaryHitPointEffectKey,
                GameMath.PercentOf(GetMaxHitPoints(target), percent),
                durationSeconds);
        }
    }
}
