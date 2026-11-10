using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark3StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Spark III";
        public override EffectIconType Icon => EffectIconType.ForceSpark3StatusEffect;

        public ForceSpark3StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -8;
        }
    }
}
