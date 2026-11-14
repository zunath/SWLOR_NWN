using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault2StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault II";
        public override EffectIconType Icon => EffectIconType.Assault2StatusEffect;

        public Assault2StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 10;
        }
    }
}
