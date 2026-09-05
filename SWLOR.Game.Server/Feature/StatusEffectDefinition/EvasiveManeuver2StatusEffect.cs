using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver2StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Maneuver II";
        public override EffectIconType Icon => EffectIconType.EvasiveManeuver2StatusEffect;

        public EvasiveManeuver2StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(EvasiveManeuver1StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(EvasiveManeuver3StatusEffect));
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 10;
        }
    }
}
