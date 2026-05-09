using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FrenziedShoutStatusEffect : LeadershipAuraStatusEffectBase
    {
        public override string Name => "Frenzied Shout";
        public override EffectIconType Icon => EffectIconType.FrenziedShout;

        public FrenziedShoutStatusEffect()
            : base(StatType.PhysicalDefense, PerkType.FrenziedShout, -1f, -1.5f, -2f)
        {
        }
    }
}
