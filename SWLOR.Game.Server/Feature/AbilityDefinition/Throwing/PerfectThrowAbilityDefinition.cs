using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class PerfectThrowAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PerfectThrow1(builder);

            return builder.Build();
        }

        private static void PerfectThrow1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PerfectThrow1, PerkType.PerfectThrow)
                .Name("Perfect Throw")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Throwing)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(PerfectThrow1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void PerfectThrow1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var statusEffect = StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding)
                ? typeof(HemorrhageStatusEffect)
                : null;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Throwing, 80, 15, statusEffect, false);
        }
    }
}
