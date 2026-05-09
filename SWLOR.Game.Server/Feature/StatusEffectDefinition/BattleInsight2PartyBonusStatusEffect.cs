namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight2PartyBonusStatusEffect : BattleInsightModifierStatusEffectBase
    {
        protected override int AccuracyAdjustment => 6;
        protected override int DefenseAdjustment => 6;
    }
}
