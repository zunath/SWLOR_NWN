using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DisruptionFieldStatusEffect : StatusEffectBase
    {
        public override string Name => "Disruption Field";
        public override EffectIconType Icon => EffectIconType.SpellResistanceDecrease;
    }
}
