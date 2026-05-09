using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver4StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Evasive Maneuver IV";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver1StatusEffect),
            typeof(EvasiveManeuver2StatusEffect),
            typeof(EvasiveManeuver3StatusEffect)
        };

        public EvasiveManeuver4StatusEffect()
            : base(StatType.Evasion, 20)
        {
        }
    }
}
