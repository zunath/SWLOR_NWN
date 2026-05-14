using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class LastWordAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LastWord1(builder);

            return builder.Build();
        }

        private static void LastWord1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LastWord1, PerkType.LastWord)
                .Name("Last Word")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .HasImpactAction(LastWord1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void LastWord1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Pistol, 35, 3, typeof(DazedStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
