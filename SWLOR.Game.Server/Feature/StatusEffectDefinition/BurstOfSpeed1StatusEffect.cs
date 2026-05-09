namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BurstOfSpeed1StatusEffect : BurstOfSpeedStatusEffectBase
    {
        protected override int MovementSpeedPercentAdjustment => 15;
        protected override int DefenseBonus => 1;

        public BurstOfSpeed1StatusEffect()
        {
            MorePowerfulEffectTypes.Add(typeof(BurstOfSpeed2StatusEffect));
        }
    }
}
