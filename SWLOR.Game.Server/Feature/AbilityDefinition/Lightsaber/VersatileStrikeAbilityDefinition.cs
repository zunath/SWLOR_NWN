using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class VersatileStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            VersatileStrike1(builder);
            VersatileStrike2(builder);
            VersatileStrike3(builder);

            return builder.Build();
        }

        private static void VersatileStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.VersatileStrike1, PerkType.VersatileStrike)
                .Name("Versatile Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.VersatileStrike, 45f)
                .HasImpactAction(VersatileStrike1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void VersatileStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.VersatileStrike2, PerkType.VersatileStrike)
                .Name("Versatile Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.VersatileStrike, 45f)
                .HasImpactAction(VersatileStrike2ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void VersatileStrike3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.VersatileStrike3, PerkType.VersatileStrike)
                .Name("Versatile Strike III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.VersatileStrike, 45f)
                .HasImpactAction(VersatileStrike3ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void VersatileStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 10, 30, typeof(SunderStatusEffect), false, statusEffectFactory: () => new SunderStatusEffect(10));
        }

        private static void VersatileStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 25, 30, typeof(SunderStatusEffect), false);
        }

        private static void VersatileStrike3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 40, 30, typeof(SunderStatusEffect), false, statusEffectFactory: () => new SunderStatusEffect(20));
        }
    }
}
