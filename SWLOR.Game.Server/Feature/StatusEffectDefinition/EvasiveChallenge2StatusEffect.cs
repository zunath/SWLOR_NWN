using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveChallenge2StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Challenge II";
        public override EffectIconType Icon => EffectIconType.EvasiveChallenge2StatusEffect;

        public EvasiveChallenge2StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -14;
        }
    }
}
