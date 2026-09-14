using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class AngerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Anger1(builder);
            Anger2(builder);

            return builder.Build();
        }

        private static void Anger1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Anger1, PerkType.Anger)
                .Name("Anger I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.Anger, activator => GetAngerRecastDelay(activator))
                .SkillType(SkillType.BeastMastery)
                .HasAITarget(AITarget.AllyAttacker())
                .HasAIScore(AIScore.ThreatControl(1))
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Anger1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void Anger2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Anger2, PerkType.Anger)
                .Name("Anger II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.Anger, activator => GetAngerRecastDelay(activator))
                .SkillType(SkillType.BeastMastery)
                .HasAITarget(AITarget.AllyAttacker())
                .HasAIScore(AIScore.ThreatControl(2))
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(Anger2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Anger1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGoad(activator, target);
        }

        private static void Anger2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGoad(activator, target);

            ApplyTemporaryHP(activator, 15, 12f);
        }


        private static void ApplyGoad(uint activator, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsReactionTypeHostile(target, activator))
                return;

            var enmity = Stat.ScaleEffect(700, GetAbilityScore(activator, AbilityType.Vitality));
            Enmity.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);
        }

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            TemporaryHitPointEffects.ApplyFlat(
                target,
                "ANGER",
                GameMath.PercentOf(GetMaxHitPoints(target), percent),
                durationSeconds);
        }

        private static float GetAngerRecastDelay(uint activator)
        {
            return Math.Max(0f, 12f + Combat.GetAbilityRecastDelayFlatAdjustment(activator, PerkType.Anger));
        }
    }
}
