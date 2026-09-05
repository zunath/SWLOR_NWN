using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronHide3StatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Hide III";
        public override EffectIconType Icon => EffectIconType.IronHide3StatusEffect;

        public IronHide3StatusEffect()
        {
            StatGroup.Stats[StatType.IronHideRank] = 3;
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -12;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -12;
        }
    }
}
