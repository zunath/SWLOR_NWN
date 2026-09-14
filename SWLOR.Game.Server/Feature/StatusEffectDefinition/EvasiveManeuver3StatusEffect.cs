using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver3StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Maneuver III";
        public override EffectIconType Icon => EffectIconType.EvasiveManeuver3StatusEffect;

        public EvasiveManeuver3StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(EvasiveManeuver1StatusEffect));
            LessPowerfulEffectTypes.Add(typeof(EvasiveManeuver2StatusEffect));
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 14;
        }
    }
}
