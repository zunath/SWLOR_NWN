using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Spark II";
        public override EffectIconType Icon => EffectIconType.ForceSpark2StatusEffect;

        public ForceSpark2StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -6;
        }
    }
}
