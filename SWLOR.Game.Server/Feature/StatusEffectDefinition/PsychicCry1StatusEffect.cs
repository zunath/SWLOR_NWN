using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
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
