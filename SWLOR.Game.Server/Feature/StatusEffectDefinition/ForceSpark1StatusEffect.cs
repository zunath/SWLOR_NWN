using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Spark I";
        public override EffectIconType Icon => EffectIconType.ForceSpark1StatusEffect;

        public ForceSpark1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -4;
        }
    }
}
