using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver5StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Evasive Maneuver V";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver1StatusEffect),
            typeof(EvasiveManeuver2StatusEffect),
            typeof(EvasiveManeuver3StatusEffect),
            typeof(EvasiveManeuver4StatusEffect)
        };

        public EvasiveManeuver5StatusEffect()
            : base(StatType.Evasion, 25)
        {
        }
    }
}
