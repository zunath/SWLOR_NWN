using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver3StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Evasive Maneuver III";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver4StatusEffect),
            typeof(EvasiveManeuver5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver1StatusEffect),
            typeof(EvasiveManeuver2StatusEffect)
        };

        public EvasiveManeuver3StatusEffect()
            : base(StatType.Evasion, 15)
        {
        }
    }
}
