using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class RibBreakerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RibBreaker1(builder);
            RibBreaker2(builder);
            RibBreaker3(builder);

            return builder.Build();
        }

        private static void RibBreaker1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RibBreaker1, PerkType.RibBreaker)
                .Name("Rib Breaker I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RibBreaker, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void RibBreaker2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RibBreaker2, PerkType.RibBreaker)
                .Name("Rib Breaker II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RibBreaker, 45f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void RibBreaker3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RibBreaker3, PerkType.RibBreaker)
                .Name("Rib Breaker III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RibBreaker, 45f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Staff, 18, 15, typeof(WeakenedStatusEffect), false);
                    break;
                case 2:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Staff, 30, 15, typeof(WeakenedStatusEffect), false);
                    break;
                case 3:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Staff, 42, 15, typeof(WeakenedStatusEffect), false);
                    break;
            }
        }
    }
}
