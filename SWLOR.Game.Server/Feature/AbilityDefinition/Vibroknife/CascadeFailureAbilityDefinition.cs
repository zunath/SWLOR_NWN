using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class CascadeFailureAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CascadeFailure1(builder);

            return builder.Build();
        }

        private static void CascadeFailure1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CascadeFailure1, PerkType.CascadeFailure)
                .Name("Cascade Failure")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CascadeFailure, 90f)
                .HasImpactAction(CascadeFailure1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void CascadeFailure1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 25, 12, null, CombatImpactAreaShape.Cone, 0.25f, 5f, 5f);
        }
    }
}
