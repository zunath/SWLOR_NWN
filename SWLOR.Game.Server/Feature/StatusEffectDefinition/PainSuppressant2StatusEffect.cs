using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PainSuppressant2StatusEffect : StatusEffectBase
    {
        public override string Name => "Pain Suppressant II";
        public override EffectIconType Icon => EffectIconType.PainSuppressant2StatusEffect;

        public PainSuppressant2StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -15;
        }
    }
}
