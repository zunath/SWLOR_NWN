using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronShellStatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Iron Shell";
        public override EffectIconType Icon => EffectIconType.ElementalShield;

        public IronShellStatusEffect()
            : base(StatType.PhysicalDefense, 20)
        {
        }
    }
}
