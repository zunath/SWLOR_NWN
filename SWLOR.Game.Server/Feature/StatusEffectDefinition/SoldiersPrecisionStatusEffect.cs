using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoldiersPrecisionStatusEffect : LeadershipAuraStatusEffectBase
    {
        public override string Name => "Soldier's Precision";
        public override EffectIconType Icon => EffectIconType.SoldiersPrecision;

        public SoldiersPrecisionStatusEffect()
            : base(StatType.Accuracy, PerkType.SoldiersPrecision, 0.5f, 1f, 1.5f)
        {
        }
    }
}
