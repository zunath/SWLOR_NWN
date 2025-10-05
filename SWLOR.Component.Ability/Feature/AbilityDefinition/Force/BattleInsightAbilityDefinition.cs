using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class BattleInsightAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            BattleInsight1(builder);
            BattleInsight2(builder);

            return builder.Build();
        }

        private void BattleInsight1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BattleInsight1, PerkType.BattleInsight)
                .Name("Battle Insight I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.BattleInsight, 60f)
                .RequirementFP(3)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void BattleInsight2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BattleInsight2, PerkType.BattleInsight)
                .Name("Battle Insight II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.BattleInsight, 60f)
                .RequirementFP(5)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
