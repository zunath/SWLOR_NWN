using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoldiersSpeedStatusEffect : LeadershipAuraStatusEffectBase
    {
        public override string Name => "Soldier's Speed";
        public override EffectIconType Icon => EffectIconType.SoldiersSpeed;

        public SoldiersSpeedStatusEffect()
            : base(StatType.Evasion, PerkType.SoldiersSpeed, 0.5f, 1f, 1.5f)
        {
        }
    }
}
