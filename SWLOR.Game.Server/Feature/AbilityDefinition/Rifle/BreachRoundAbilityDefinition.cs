using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class BreachRoundAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreachRound1(builder);

            return builder.Build();
        }

        private static void BreachRound1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreachRound1, PerkType.BreachRound)
                .Name("Breach Round")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BreachRound, 90f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(BreachRound1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void BreachRound1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 35, 0, null, false);
        }
    }
}
