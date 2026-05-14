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
                .HasRecastDelay(RecastGroup.CleanseOrder, 90f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(CleanseOrder1ImpactAction)
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
                .HasRecastDelay(RecastGroup.CleanseOrder, 90f)
                .SkillType(SkillType.Leadership)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(CleanseOrder2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void CleanseOrder1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.TreatmentKit2, false);
                var duration = LeadershipAbilityEffects.ApplyFieldStewardDurationBonus(activator, 8f);
                ApplyTemporaryHP(
                    friendly,
                    AbilityEffectScaling.ScaleValueBySourceSocial(activator, 3, 5),
                    duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }

        private static void CleanseOrder2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveFirstCleanseableStatusEffect(friendly, StatusEffectCleanseType.Purify, false);
                var duration = LeadershipAbilityEffects.ApplyFieldStewardDurationBonus(activator, 8f);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(CleanseOrder2StatusEffect), duration);
                LeadershipAbilityEffects.ApplyTriageProtocol(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
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
