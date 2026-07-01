using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class FlankingBarrageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FlankingBarrage1(builder);

            return builder.Build();
        }

        private static void FlankingBarrage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FlankingBarrage1, PerkType.FlankingBarrage)
                .Name("Flanking Barrage")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.FlankingBarrage, 120f)
                .RequiresTarget()
                .HasImpactAction(FlankingBarrage1ImpactAction)
                .SkillType(SkillType.Spear)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void FlankingBarrage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var isBesideTarget = Combat.IsAttackerBesideTarget(activator, target);
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Spear,
                isBesideTarget ? 20 : 16,
                isBesideTarget ? 8 : 0,
                isBesideTarget ? typeof(FlankingBarrageStatusEffect) : null,
                false);
        }
    }
}
