using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
                .UsesAnimation(Animation.Backstab)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction(VitalStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void VitalStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 35, 45, typeof(VitalStrikeStatusEffect), false);
        }
    }
}
