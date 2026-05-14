using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class GroundQuakeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GroundQuake1(builder);
            GroundQuake2(builder);

            return builder.Build();
        }

        private static void GroundQuake1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GroundQuake1, PerkType.GroundQuake)
                .Name("Ground Quake I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GroundQuake, 60f)
                .HasImpactAction(GroundQuake1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void GroundQuake2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.GroundQuake2, PerkType.GroundQuake)
                .Name("Ground Quake II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GroundQuake, 60f)
                .HasImpactAction(GroundQuake2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void GroundQuake1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Staff, 18, 2, typeof(KnockdownStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
        }

        private static void GroundQuake2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Staff, 28, 3, typeof(KnockdownStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
        }
    }
}
