using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SonicBurst3StatusEffect : StatusEffectBase
    {
        public override string Name => "Sonic Burst III";
        public override EffectIconType Icon => EffectIconType.SonicBurst3StatusEffect;

        public SonicBurst3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -10;
        }
    }
}
