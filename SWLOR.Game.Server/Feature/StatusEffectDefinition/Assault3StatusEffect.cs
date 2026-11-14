using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault3StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault III";
        public override EffectIconType Icon => EffectIconType.Assault3StatusEffect;

        public Assault3StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 14;
        }
    }
}
