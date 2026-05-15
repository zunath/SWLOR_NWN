using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class VitalStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            VitalStrike1(builder);

            return builder.Build();
        }

        private static void VitalStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.VitalStrike1, PerkType.VitalStrike)
                .Name("Vital Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction(VitalStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void VitalStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 55, 12, typeof(VitalStrikeStatusEffect), false);
        }
    }
}
