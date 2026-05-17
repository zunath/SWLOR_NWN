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
    public sealed class MarkTargetAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MarkTarget1(builder);
            MarkTarget2(builder);

            return builder.Build();
        }

        private static void MarkTarget1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MarkTarget1, PerkType.MarkTarget)
                .Name("Mark Target I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MarkTarget, 45f)
                .SkillType(SkillType.Leadership)
                .HasMaxRange(LeadershipAbilityRange.CommandTarget)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(MarkTarget1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void MarkTarget2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MarkTarget2, PerkType.MarkTarget)
                .Name("Mark Target II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.MarkTarget, 45f)
                .SkillType(SkillType.Leadership)
                .HasMaxRange(LeadershipAbilityRange.CommandTarget)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(MarkTarget2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void MarkTarget1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            const int Duration = 15;

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Leadership,
                0,
                Duration,
                typeof(MarkTarget1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                awardsCombatPoints: false);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static void MarkTarget2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            const int Duration = 15;

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Leadership,
                0,
                Duration,
                typeof(MarkTarget2StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                awardsCombatPoints: false);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

    }
}
