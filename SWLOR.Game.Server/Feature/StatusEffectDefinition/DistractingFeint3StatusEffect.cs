using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DistractingFeint3StatusEffect : StatusEffectBase
    {
        public override string Name => "Distracting Feint III";
        public override EffectIconType Icon => EffectIconType.DistractingFeint3StatusEffect;

        public DistractingFeint3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -12;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -12;
        }
    }
}
