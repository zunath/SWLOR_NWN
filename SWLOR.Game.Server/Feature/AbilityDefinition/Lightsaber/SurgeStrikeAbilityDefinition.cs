using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class SurgeStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SurgeStrike1(builder);

            return builder.Build();
        }

        private static void SurgeStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SurgeStrike1, PerkType.SurgeStrike)
                .Name("Surge Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SurgeStrike, 30f)
                .HasImpactAction(SurgeStrike1ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SurgeStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Lightsaber,
                15,
                8,
                null,
                false,
                statusEffectFactory: () => new ForceDisruptionStatusEffect(true));
        }
    }
}
