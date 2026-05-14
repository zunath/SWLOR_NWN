using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class ReversalCutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ReversalCut1(builder);

            return builder.Build();
        }

        private static void ReversalCut1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ReversalCut1, PerkType.ReversalCut)
                .Name("Reversal Cut")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ReversalCut, 60f)
                .RequiresTarget()
                .HasImpactAction(ReversalCut1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ReversalCut1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 40, 3, typeof(DazedStatusEffect), false);
        }
    }
}
