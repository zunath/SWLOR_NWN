using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class CarveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Carve1(builder);

            return builder.Build();
        }

        private static void Carve1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Carve1, PerkType.Carve)
                .Name("Carve")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasRecastDelay(RecastGroup.Carve, 36f)
                .HasImpactAction(Carve1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void Carve1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 35, 18, typeof(HemorrhageStatusEffect), false);
        }
    }
}
