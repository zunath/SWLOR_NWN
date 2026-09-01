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
    public sealed class BreakMoraleAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreakMorale1(builder);
            BreakMorale2(builder);

            return builder.Build();
        }

        private static void BreakMorale1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreakMorale1, PerkType.BreakMorale)
                .Name("Break Morale I")
                .Level(1)
                .HasActivationDelay(0.5f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.BreakMorale, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BreakMorale1ImpactAction)
                .HasTargetingSphere(
                    Spell.BreakMorale1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void BreakMorale2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreakMorale2, PerkType.BreakMorale)
                .Name("Break Morale II")
                .Level(2)
                .HasActivationDelay(0.5f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.BreakMorale, 45f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BreakMorale2ImpactAction)
                .HasTargetingSphere(
                    Spell.BreakMorale2,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies,
                    LeadershipAbilityEffects.ApplyLeadershipCommandRadiusBonus)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void BreakMorale1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBreakMorale(activator, 10, 12, 0, 0);
        }

        private static void BreakMorale2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBreakMorale(activator, 15, 18, 12, 15);
        }

        private static void ApplyBreakMorale(
            uint activator,
            int flashBasePenalty,
            int flashMaximumPenalty,
            int weakenedBasePenalty,
            int weakenedMaximumPenalty)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var location = GetLocation(activator);
            const float Duration = 30f;
            var affectedCount = 0;

            foreach (var hostile in AbilityTargeting.GetHostileTargetsNearLocation(activator, location, radius, 0))
            {
                var applied = StatusEffect.ApplyStatusEffect(
                    activator,
                    hostile,
                    new FlashStatusEffect(ScaleSocialPenalty(activator, flashBasePenalty, flashMaximumPenalty)),
                    Duration,
                    CombatDamageType.Force);

                if (weakenedBasePenalty > 0)
                {
                    applied |= StatusEffect.ApplyStatusEffect(
                        activator,
                        hostile,
                        new WeakenedStatusEffect(ScaleSocialPenalty(activator, weakenedBasePenalty, weakenedMaximumPenalty)),
                        Duration,
                        CombatDamageType.Force);
                }

                if (!applied)
                    continue;

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), hostile);
                affectedCount++;
            }

            if (affectedCount <= 0)
                return;

            Combat.ApplyLeadershipVanguardImpactRiders(activator);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), location);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static int ScaleSocialPenalty(uint activator, int baseValue, int maximumValue)
        {
            return AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseValue, maximumValue);
        }
    }
}
