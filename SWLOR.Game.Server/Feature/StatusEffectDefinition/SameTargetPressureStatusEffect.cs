using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SameTargetPressureStatusEffect : StatusEffectBase
    {
        public override string Name => "Spotter's Rhythm";
        public override EffectIconType Icon => EffectIconType.SpotterStanceStatusEffect;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;
    }
}
