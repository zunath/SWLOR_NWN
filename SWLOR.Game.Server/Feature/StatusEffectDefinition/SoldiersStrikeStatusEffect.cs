using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoldiersStrikeStatusEffect : LeadershipAuraStatusEffectBase
    {
        public override string Name => "Soldier's Strike";
        public override EffectIconType Icon => EffectIconType.SoldiersStrike;

        public SoldiersStrikeStatusEffect()
            : base(StatType.Attack, PerkType.SoldiersStrike, 1f, 1.5f, 2f)
        {
        }
    }
}
