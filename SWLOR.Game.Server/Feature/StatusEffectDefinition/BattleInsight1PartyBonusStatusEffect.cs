namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BattleInsight1PartyBonusStatusEffect : BattleInsightModifierStatusEffectBase
    {
        protected override int AccuracyAdjustment => 3;
        protected override int DefenseAdjustment => 3;
    }
}
