using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronHide1StatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Hide I";
        public override EffectIconType Icon => EffectIconType.IronHide1StatusEffect;

        public IronHide1StatusEffect()
        {
            StatGroup.Stats[StatType.IronHideRank] = 1;
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -5;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
        }
    }
}
