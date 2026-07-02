using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PredatoryBondStatusEffect : BeastBondStatusEffect
    {
        protected override Type BeastStatusEffectType => typeof(PredatoryBondBeastStatusEffect);

        public override string Name => "Predatory Bond";
        public override EffectIconType Icon => EffectIconType.PredatoryBondStatusEffect;
    }
}
