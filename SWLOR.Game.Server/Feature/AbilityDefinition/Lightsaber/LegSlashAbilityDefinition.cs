using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class LegSlashAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LegSlash1(builder);

            return builder.Build();
        }

        private static void LegSlash1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LegSlash1, PerkType.LegSlash)
                .Name("Leg Slash")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.LegSlash, 60f)
                .RequiresTarget()
                .HasImpactAction(LegSlash1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void LegSlash1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 10, 20, typeof(DisorientedStatusEffect), false);
        }
    }
}
