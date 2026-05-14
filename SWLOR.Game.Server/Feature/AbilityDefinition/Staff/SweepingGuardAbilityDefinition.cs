using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class SweepingGuardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SweepingGuard1(builder);

            return builder.Build();
        }

        private static void SweepingGuard1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SweepingGuard1, PerkType.SweepingGuard)
                .Name("Sweeping Guard")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SweepingGuard, 90f)
                .HasImpactAction(SweepingGuard1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SweepingGuard1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Staff, 18, 2, typeof(KnockdownStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
        }
    }
}
