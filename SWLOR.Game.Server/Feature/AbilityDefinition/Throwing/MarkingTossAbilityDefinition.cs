using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class MarkingTossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            MarkingToss1(builder);

            return builder.Build();
        }

        private static void MarkingToss1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.MarkingToss1, PerkType.MarkingToss)
                .Name("Marking Toss")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.MarkingToss, 60f)
                .RequiresTarget()
                .HasImpactAction(MarkingToss1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void MarkingToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 18, 12, typeof(MarkingTossStatusEffect), false);
        }
    }
}
