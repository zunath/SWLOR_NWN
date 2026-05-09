using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceCapacitorStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Capacitor";
        public override EffectIconType Icon => EffectIconType.Haste;
    }
}
