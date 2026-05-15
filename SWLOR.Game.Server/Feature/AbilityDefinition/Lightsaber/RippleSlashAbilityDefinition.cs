using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class RippleSlashAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RippleSlash1(builder);

            return builder.Build();
        }

        private static void RippleSlash1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RippleSlash1, PerkType.RippleSlash)
                .Name("Ripple Slash")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.RippleSlash, 120f)
                .SkillType(SkillType.Lightsaber)
                .HasImpactAction(RippleSlash1ImpactAction)
                .IsAreaAbility()
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void RippleSlash1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Lightsaber, 30, 0, null, false);
            ApplyStatusToNearbyEnemies(activator, target, targetLocation, typeof(DisorientedStatusEffect), 20f, false, 0);
        }
    }
}
