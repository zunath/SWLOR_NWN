using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class FractureStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FractureStrike1(builder);

            return builder.Build();
        }

        private static void FractureStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FractureStrike1, PerkType.FractureStrike)
                .Name("Fracture Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FractureStrike, 90f)
                .HasImpactAction(FractureStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void FractureStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Spear, 12, 30, typeof(FracturedFocusStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
        }
    }
}
