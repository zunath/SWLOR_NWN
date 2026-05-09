using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class TempestReleaseAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TempestRelease1(builder);

            return builder.Build();
        }

        private static void TempestRelease1(AbilityBuilder builder)
        {
            builder.Create(FeatType.TempestRelease1, PerkType.TempestRelease)
                .Name("Tempest Release")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.TempestRelease, 120f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 20, 0, 0, SavingThrow.Will, null, true);
                    break;
            }
        }
    }
}
