using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingBondStatusEffect : BeastBondStatusEffect
    {
        protected override Type BeastStatusEffectType => typeof(GuardingBondBeastStatusEffect);

        public override string Name => "Guarding Bond";
        public override EffectIconType Icon => EffectIconType.GuardingBondStatusEffect;
    }
}
