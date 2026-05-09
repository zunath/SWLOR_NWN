using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class BreachStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreachStrike1(builder);

            return builder.Build();
        }

        private static void BreachStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BreachStrike1, PerkType.BreachStrike)
                .Name("Breach Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BreachStrike, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Spear, 10, 30, 14, SavingThrow.Reflex, null, false);
                    break;
            }
        }
    }
}
