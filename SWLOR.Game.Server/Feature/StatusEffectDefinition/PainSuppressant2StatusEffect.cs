using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PainSuppressant2StatusEffect : StatusEffectBase
    {
        public override string Name => "Pain Suppressant II";
        public override EffectIconType Icon => EffectIconType.PainSuppressant2StatusEffect;

        public PainSuppressant2StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -15;
        }
    }
}
