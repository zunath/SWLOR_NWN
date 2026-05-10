using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class ClusterStormAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ClusterStorm1(builder);

            return builder.Build();
        }

        private static void ClusterStorm1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ClusterStorm1, PerkType.ClusterStorm)
                .Name("Cluster Storm")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ClusterStorm, 120f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 12, 0, null, true);
                    break;
            }
        }
    }
}
