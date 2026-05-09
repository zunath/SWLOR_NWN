using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeflectivePresenceStatusEffect : StatusEffectBase
    {
        public override string Name => "Deflective Presence";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
    }
}
