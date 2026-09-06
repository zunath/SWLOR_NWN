using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver1StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Maneuver I";
        public override EffectIconType Icon => EffectIconType.EvasiveManeuver1StatusEffect;

        public EvasiveManeuver1StatusEffect()
        {
            MorePowerfulEffectTypes.Add(typeof(EvasiveManeuver2StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(EvasiveManeuver3StatusEffect));
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 6;
        }
    }
}
