using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardiansResolveStatusEffect : StatusEffectBase
    {
        public override string Name => "Guardian's Resolve";
        public override EffectIconType Icon => EffectIconType.TemporaryHitpoints;
    }
}
