using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Drain II";
        public override EffectIconType Icon => EffectIconType.ForceDrain2StatusEffect;

        public ForceDrain2StatusEffect()
        {
        }
    }
}
