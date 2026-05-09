namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BurstOfSpeed2StatusEffect : BurstOfSpeedStatusEffectBase
    {
        protected override int MovementSpeedPercentAdjustment => 25;
        protected override int DefenseBonus => 2;

        public BurstOfSpeed2StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(BurstOfSpeed1StatusEffect));
        }
    }
}
