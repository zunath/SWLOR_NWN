using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight1StatusEffect : BattleInsightStatusEffectBase
    {
        public override string Name => "Battle Insight I";
        protected override int EnmityAmount => 80;
        protected override Type SelfModifierStatusEffectType => typeof(BattleInsight1SelfPenaltyStatusEffect);
        protected override Type PartyModifierStatusEffectType => typeof(BattleInsight1PartyBonusStatusEffect);

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BattleInsight2StatusEffect)
        };
    }
}
