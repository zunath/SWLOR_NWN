using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class SeverFocusAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SeverFocus1(builder);
            SeverFocus2(builder);

            return builder.Build();
        }

        private static void SeverFocus1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SeverFocus1, PerkType.SeverFocus)
                .Name("Sever Focus I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SeverFocus, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void SeverFocus2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SeverFocus2, PerkType.SeverFocus)
                .Name("Sever Focus II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SeverFocus, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 18, 20, typeof(FracturedFocusStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, 28, 30, typeof(FracturedFocusStatusEffect), false);
                    break;
            }
        }
    }
}
