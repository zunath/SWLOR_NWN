using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingBondStanceStatusEffect : BeastBondStatusEffect
    {
        protected override Type BeastStatusEffectType => typeof(GuardingBondStanceBeastStatusEffect);

        public override string Name => "Guarding Bond Stance";
        public override EffectIconType Icon => EffectIconType.GuardingBondStanceStatusEffect;
    }
}
