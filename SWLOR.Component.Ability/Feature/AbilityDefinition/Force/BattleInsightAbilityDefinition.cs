using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

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
                .HasRecastDelay(RecastGroup.BattleInsight, 60f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.BattleInsight1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void BattleInsight2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BattleInsight2, PerkType.BattleInsight)
                .Name("Battle Insight II")
                .Level(2)
                .HasRecastDelay(RecastGroup.BattleInsight, 60f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.BattleInsight2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
