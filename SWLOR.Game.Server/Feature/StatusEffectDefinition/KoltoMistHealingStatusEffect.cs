using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KoltoMistHealingStatusEffect : StatusEffectBase
    {
        public override string Name => "Kolto Mist";
        public override EffectIconType Icon => EffectIconType.KoltoMistHealingStatusEffect;
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        // Zone pulses re-apply this every few seconds and it carries nothing but its duration.
        public override bool CanRefreshInPlace(IStatusEffect active) => active is KoltoMistHealingStatusEffect;
    }
}
