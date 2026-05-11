using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver2StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Evasive Maneuver II";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver1StatusEffect)
        };

        public EvasiveManeuver2StatusEffect()
            : base(StatType.Evasion, 10)
        {
        }
    }
}
