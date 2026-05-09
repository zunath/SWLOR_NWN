using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight2StatusEffect : BattleInsightStatusEffectBase
    {
        public override string Name => "Battle Insight II";
        protected override int EnmityAmount => 120;
        protected override Type SelfModifierStatusEffectType => typeof(BattleInsight2SelfPenaltyStatusEffect);
        protected override Type PartyModifierStatusEffectType => typeof(BattleInsight2PartyBonusStatusEffect);

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BattleInsight1StatusEffect)
        };
    }
}
