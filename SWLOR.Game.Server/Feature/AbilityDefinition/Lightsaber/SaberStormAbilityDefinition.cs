using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class SaberStormAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SaberStorm1(builder);

            return builder.Build();
        }

        private static void SaberStorm1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SaberStorm1, PerkType.SaberStorm)
                .Name("Saber Storm")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Lightsaber)
                .HasImpactAction(SaberStorm1ImpactAction)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void SaberStorm1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Lightsaber,
                30,
                45,
                typeof(SunderStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.25f,
                5f,
                centerOnActivator: true,
                statusEffectFactory: () => new SunderStatusEffect(10));
        }
    }
}
