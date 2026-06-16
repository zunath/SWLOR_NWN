using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class SweepingFlankAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SweepingFlank1(builder);

            return builder.Build();
        }

        private static void SweepingFlank1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SweepingFlank1, PerkType.SweepingFlank)
                .Name("Sweeping Flank")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.SweepingFlank, 60f)
                .HasImpactAction(SweepingFlank1ImpactAction)
                .HasTargetingCone(
                    Spell.SweepingFlank1,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .SkillType(SkillType.Spear)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SweepingFlank1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Spear, 18, 30, typeof(ExposedStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
