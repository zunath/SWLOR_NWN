using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class SkullRattleAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SkullRattle1(builder);

            return builder.Build();
        }

        private static void SkullRattle1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SkullRattle1, PerkType.SkullRattle)
                .Name("Skull Rattle")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SkullRattle, 90f)
                .RequiresTarget()
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Staff, 34, 3, 16, SavingThrow.Fortitude, typeof(DazedStatusEffect), false);
                    break;
            }
        }
    }
}
