using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class OpeningStatusEffect : StatusEffectBase
    {
        public override string Name => "Opening";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override EffectIconType Icon => EffectIconType.OpeningStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
