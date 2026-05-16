using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class RainOfSteelAbilityDefinition : IAbilityListDefinition
    {
        private const float Radius = 8f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RainOfSteel1(builder);

            return builder.Build();
        }

        private static void RainOfSteel1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RainOfSteel1, PerkType.RainOfSteel)
                .Name("Rain of Steel")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Throwing)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(RainOfSteel1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void RainOfSteel1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Throwing,
                35,
                60,
                typeof(BleedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                Radius);
        }
    }
}
