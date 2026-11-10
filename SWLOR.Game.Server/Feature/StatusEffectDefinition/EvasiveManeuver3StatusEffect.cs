using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
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
