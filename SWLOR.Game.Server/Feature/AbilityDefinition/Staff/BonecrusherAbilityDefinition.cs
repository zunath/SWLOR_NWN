using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class BonecrusherAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Bonecrusher1(builder);

            return builder.Build();
        }

        private static void Bonecrusher1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Bonecrusher1, PerkType.Bonecrusher)
                .Name("Bonecrusher")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.Bonecrusher, 120f)
                .SkillType(SkillType.Staff)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction(Bonecrusher1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void Bonecrusher1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var statusEffect = StatusEffect.HasStatusEffect(target, typeof(KnockdownStatusEffect))
                ? typeof(StunnedStatusEffect)
                : null;
            var duration = statusEffect == null ? 0 : 3;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Staff, 50, duration, statusEffect, false);
        }
    }
}
