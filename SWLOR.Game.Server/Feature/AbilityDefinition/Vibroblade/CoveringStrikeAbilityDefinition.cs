using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class CoveringStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CoveringStrike1(builder);

            return builder.Build();
        }

        private static void CoveringStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CoveringStrike1, PerkType.CoveringStrike)
                .Name("Covering Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CoveringStrike, 45f)
                .HasImpactAction(CoveringStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void CoveringStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 20, 12, typeof(CoveringStrikeStatusEffect), CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
        }
    }
}
