using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AssaultStatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Assault";
        public override EffectIconType Icon => EffectIconType.ACIncrease;

        public AssaultStatusEffect()
            : base(StatType.Evasion, 10)
        {
        }
    }
}
