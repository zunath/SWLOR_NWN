using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain3StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Drain III";
        public override EffectIconType Icon => EffectIconType.ForceDrain3StatusEffect;

        public ForceDrain3StatusEffect()
        {
        }
    }
}
