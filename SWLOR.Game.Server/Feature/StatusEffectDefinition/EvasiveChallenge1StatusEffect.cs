using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveChallenge1StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Challenge I";
        public override EffectIconType Icon => EffectIconType.EvasiveChallenge1StatusEffect;

        public EvasiveChallenge1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -8;
        }
    }
}
