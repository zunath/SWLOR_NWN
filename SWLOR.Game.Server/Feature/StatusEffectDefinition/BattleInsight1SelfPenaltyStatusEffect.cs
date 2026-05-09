namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight1SelfPenaltyStatusEffect : BattleInsightModifierStatusEffectBase
    {
        protected override int AccuracyAdjustment => -5;
        protected override int DefenseAdjustment => -5;
    }
}
