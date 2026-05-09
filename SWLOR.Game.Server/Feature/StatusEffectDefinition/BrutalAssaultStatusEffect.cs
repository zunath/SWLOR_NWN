using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BrutalAssaultStatusEffect : StatusEffectBase
    {
        public override string Name => "Brutal Assault";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
    }
}
