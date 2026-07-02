using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class EvasiveChallengeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EvasiveChallenge1(builder);
            EvasiveChallenge2(builder);

            return builder.Build();
        }

        private static void EvasiveChallenge1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EvasiveChallenge1, PerkType.EvasiveChallenge)
                .Name("Evasive Challenge I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.EvasiveChallenge, 30f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(EvasiveChallenge1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void EvasiveChallenge2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EvasiveChallenge2, PerkType.EvasiveChallenge)
                .Name("Evasive Challenge II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.EvasiveChallenge, 30f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(EvasiveChallenge2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void EvasiveChallenge1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyGoad(activator, target);

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(EvasiveChallenge1SelfStatusEffect), 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

        private static void EvasiveChallenge2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var hostile in GetHostileTargets(activator, target, targetLocation, true, 5f))
            {
                ApplyGoad(activator, hostile);
            }

            RemoveMovementSlow(activator);
            StatusEffect.ApplyStatusEffect(activator, activator, typeof(EvasiveChallenge2SelfStatusEffect), 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), activator);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
        }

        private static IEnumerable<uint> GetHostileTargets(uint activator, uint target, Location targetLocation, bool centerOnActivator, float radius)
        {
            var location = centerOnActivator || !GetIsObjectValid(target)
                ? GetLocation(activator)
                : GetLocation(target);
            if (!GetIsObjectValid(GetAreaFromLocation(location)) && GetIsObjectValid(GetAreaFromLocation(targetLocation)))
                location = targetLocation;

            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            while (GetIsObjectValid(creature))
            {
                if (creature != activator && GetIsReactionTypeHostile(creature, activator))
                    yield return creature;

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }
        }

        private static void ApplyGoad(uint activator, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsReactionTypeHostile(target, activator))
                return;

            var enmity = Stat.ScaleEffect(700, GetAbilityScore(activator, AbilityType.Vitality));
            Enmity.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);
        }

        private static void RemoveMovementSlow(uint creature)
        {
            StatusEffect.RemoveStatusEffectsWithNegativeStat(creature, StatType.MovementSpeedPercentAdjustment, false);
        }
    }
}
