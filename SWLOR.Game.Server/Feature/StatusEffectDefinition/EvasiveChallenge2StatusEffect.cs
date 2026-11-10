using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveChallenge2StatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Challenge II";
        public override EffectIconType Icon => EffectIconType.EvasiveChallenge2StatusEffect;

        public EvasiveChallenge2StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -14;
        }
    }
}
