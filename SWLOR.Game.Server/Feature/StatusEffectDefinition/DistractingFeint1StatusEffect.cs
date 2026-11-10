using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint1StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint I";
        public override EffectIconType Icon => EffectIconType.DistractingFeint1StatusEffect;

        public DistractingFeint1StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -4;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -4;
        }
    }
}
