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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class GuardingRoarAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardingRoar1(builder);
            GuardingRoar2(builder);
            GuardingRoar3(builder);

            return builder.Build();
        }

        private static void GuardingRoar1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardingRoar1, PerkType.GuardingRoar)
                .Name("Guarding Roar I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardingRoar, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(GuardingRoar1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void GuardingRoar2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardingRoar2, PerkType.GuardingRoar)
                .Name("Guarding Roar II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardingRoar, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(GuardingRoar2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GuardingRoar3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GuardingRoar3, PerkType.GuardingRoar)
                .Name("Guarding Roar III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardingRoar, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(GuardingRoar3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void GuardingRoar1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var hostile in GetHostileTargets(activator, target, targetLocation, true, 5f))
            {
                ApplyGoad(activator, hostile);
            }

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardingRoar1SelfStatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

        private static void GuardingRoar2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var hostile in GetHostileTargets(activator, target, targetLocation, true, 5f))
            {
                ApplyGoad(activator, hostile);
            }

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardingRoar2SelfStatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
        }

        private static void GuardingRoar3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var hostile in GetHostileTargets(activator, target, targetLocation, true, 5f))
            {
                ApplyGoad(activator, hostile);
            }

            foreach (var friendly in new[] { activator })
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(GuardingRoar3SelfStatusEffect), 12f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
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
    }
}
