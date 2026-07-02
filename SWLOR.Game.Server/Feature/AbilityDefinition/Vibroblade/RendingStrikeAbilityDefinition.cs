using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class RendingStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RendingStrike1(builder);
            RendingStrike2(builder);

            return builder.Build();
        }

        private static void RendingStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RendingStrike1, PerkType.RendingStrike)
                .Name("Rending Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasRecastDelay(RecastGroup.RendingStrike, 60f)
                .HasImpactAction(RendingStrike1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void RendingStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RendingStrike2, PerkType.RendingStrike)
                .Name("Rending Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .RequiresTarget()
                .IsSingleTargetAbility()
                .HasRecastDelay(RecastGroup.RendingStrike, 60f)
                .HasImpactAction(RendingStrike2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void RendingStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 18, 18, typeof(ExposedStatusEffect), false);
        }

        private static void RendingStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Vibroblade, 32, 18, typeof(ExposedStatusEffect), false, statusEffectFactory: () => new ExposedStatusEffect(-25));
        }
    }
}
