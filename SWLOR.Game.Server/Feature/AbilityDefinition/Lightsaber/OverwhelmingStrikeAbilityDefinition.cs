using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class OverwhelmingStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            OverwhelmingStrike1(builder);

            return builder.Build();
        }

        private static void OverwhelmingStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.OverwhelmingStrike1, PerkType.OverwhelmingStrike)
                .Name("Overwhelming Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.OverwhelmingStrike, 90f)
                .SkillType(SkillType.Lightsaber)
                .HasImpactAction(OverwhelmingStrike1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void OverwhelmingStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 15, 30, typeof(SunderStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
