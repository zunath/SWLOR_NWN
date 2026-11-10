using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PsychicCry1StatusEffect : StatusEffectBase
    {
        public override string Name => "Psychic Cry I";
        public override EffectIconType Icon => EffectIconType.PsychicCry1StatusEffect;

        public PsychicCry1StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -5;
        }
    }
}
