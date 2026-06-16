using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class BladeVortexAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BladeVortex1(builder);
            BladeVortex2(builder);

            return builder.Build();
        }

        private static void BladeVortex1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BladeVortex1, PerkType.BladeVortex)
                .Name("Blade Vortex I")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.BladeVortex, 75f)
                .HasImpactAction(BladeVortex1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void BladeVortex2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BladeVortex2, PerkType.BladeVortex)
                .Name("Blade Vortex II")
                .Level(2)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.BladeVortex, 75f)
                .HasImpactAction(BladeVortex2ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void BladeVortex1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 18, 0, null, CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
        }

        private static void BladeVortex2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.TwinBlade, 26, 12, typeof(ExposedStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
        }
    }
}
