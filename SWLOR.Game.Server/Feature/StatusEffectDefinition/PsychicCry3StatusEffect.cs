using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PsychicCry3StatusEffect : StatusEffectBase
    {
        public override string Name => "Psychic Cry III";
        public override EffectIconType Icon => EffectIconType.PsychicCry3StatusEffect;

        public PsychicCry3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -12;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = 8;
        }
    }
}
