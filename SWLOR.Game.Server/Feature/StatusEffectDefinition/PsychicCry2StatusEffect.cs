using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PsychicCry2StatusEffect : StatusEffectBase
    {
        public override string Name => "Psychic Cry II";
        public override EffectIconType Icon => EffectIconType.PsychicCry2StatusEffect;

        public PsychicCry2StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -8;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = 5;
        }
    }
}
