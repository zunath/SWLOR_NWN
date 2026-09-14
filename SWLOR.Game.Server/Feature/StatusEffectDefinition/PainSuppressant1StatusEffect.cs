using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PainSuppressant1StatusEffect : StatusEffectBase
    {
        public override string Name => "Pain Suppressant I";
        public override EffectIconType Icon => EffectIconType.PainSuppressant1StatusEffect;

        public PainSuppressant1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -10;
        }
    }
}
