using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class SweepingAdvanceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SweepingAdvance1(builder);

            return builder.Build();
        }

        private static void SweepingAdvance1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SweepingAdvance1, PerkType.SweepingAdvance)
                .Name("Sweeping Advance")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SweepingAdvance, 60f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 24, 8, null, CombatImpactAreaShape.Line, 0.25f, 8f, 2.5f);
                    break;
            }
        }
    }
}
