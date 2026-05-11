using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class EdgeOfDarknessAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EdgeOfDarkness1(builder);

            return builder.Build();
        }

        private static void EdgeOfDarkness1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EdgeOfDarkness1, PerkType.EdgeOfDarkness)
                .Name("Edge of Darkness")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EdgeOfDarkness, 300f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(16);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 15, 0, null, true);
                    break;
            }
        }
    }
}
