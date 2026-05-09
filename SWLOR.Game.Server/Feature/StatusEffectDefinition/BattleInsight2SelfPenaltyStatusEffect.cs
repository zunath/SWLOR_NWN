namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight2SelfPenaltyStatusEffect : BattleInsightModifierStatusEffectBase
    {
        protected override int AccuracyAdjustment => -8;
        protected override int DefenseAdjustment => -8;
    }
}
