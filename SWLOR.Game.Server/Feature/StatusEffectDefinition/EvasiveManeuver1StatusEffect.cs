using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveManeuver1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Evasive Maneuver I";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(EvasiveManeuver2StatusEffect),
            typeof(EvasiveManeuver3StatusEffect),
        };

        public EvasiveManeuver1StatusEffect()
            : base(StatType.Evasion, 5)
        {
        }
    }
}
