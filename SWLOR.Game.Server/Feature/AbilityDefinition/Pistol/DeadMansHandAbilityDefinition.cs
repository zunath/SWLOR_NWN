using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class DeadMansHandAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DeadMansHand1(builder);

            return builder.Build();
        }

        private static void DeadMansHand1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DeadMansHand1, PerkType.DeadMansHand)
                .Name("Dead Man's Hand")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.DeadMansHand, 1800f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Pistol, 10, 0, null, true);
                    break;
            }
        }
    }
}
