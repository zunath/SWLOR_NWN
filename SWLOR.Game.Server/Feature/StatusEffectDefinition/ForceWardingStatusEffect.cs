using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceWardingStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Warding";
        public override EffectIconType Icon => EffectIconType.ForceWardingStatusEffect;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
    }
}
