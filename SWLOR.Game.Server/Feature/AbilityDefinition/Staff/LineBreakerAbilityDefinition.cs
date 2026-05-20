using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class LineBreakerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LineBreaker1(builder);

            return builder.Build();
        }

        private static void LineBreaker1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LineBreaker1, PerkType.LineBreaker)
                .Name("Line Breaker")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.LineBreaker, 60f)
                .HasImpactAction(LineBreaker1ImpactAction)
                .HasTargetingLine(
                    Spell.LineBreaker1,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .SkillType(SkillType.Staff)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void LineBreaker1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Staff, 18, 12, typeof(DisorientedStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
        }
    }
}
