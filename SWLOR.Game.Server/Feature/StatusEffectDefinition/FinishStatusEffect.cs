using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FinishStatusEffect : StatusEffectBase
    {
        public override string Name => "Finish";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override EffectIconType Icon => EffectIconType.FinishStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
