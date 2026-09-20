using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PredatoryBondStanceStatusEffect : BeastBondStatusEffect
    {
        protected override Type BeastStatusEffectType => typeof(PredatoryBondStanceBeastStatusEffect);

        public override string Name => "Predatory Bond Stance";
        public override EffectIconType Icon => EffectIconType.PredatoryBondStanceStatusEffect;
    }
}
