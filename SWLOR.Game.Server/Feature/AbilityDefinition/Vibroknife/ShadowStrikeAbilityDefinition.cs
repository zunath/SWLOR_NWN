using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class ShadowStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ShadowStrike1(builder);
            ShadowStrike2(builder);

            return builder.Build();
        }

        private static void ShadowStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ShadowStrike1, PerkType.ShadowStrike)
                .Name("Shadow Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ShadowStrike, 60f)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction(ShadowStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void ShadowStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ShadowStrike2, PerkType.ShadowStrike)
                .Name("Shadow Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ShadowStrike, 60f)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasImpactAction(ShadowStrike2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ShadowStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroknife,
                30,
                8,
                typeof(ShadowStrikeStatusEffect),
                false,
                statusEffectFactory: () => new ShadowStrikeStatusEffect(-30));
        }

        private static void ShadowStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroknife,
                48,
                12,
                typeof(ShadowStrikeStatusEffect),
                false,
                statusEffectFactory: () => new ShadowStrikeStatusEffect(-40));
        }
    }
}
