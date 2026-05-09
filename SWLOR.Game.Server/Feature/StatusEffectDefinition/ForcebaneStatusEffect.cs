using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForcebaneStatusEffect : StatusEffectBase
    {
        public override string Name => "Forcebane";
        public override EffectIconType Icon => EffectIconType.SpellResistanceDecrease;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
    }
}
