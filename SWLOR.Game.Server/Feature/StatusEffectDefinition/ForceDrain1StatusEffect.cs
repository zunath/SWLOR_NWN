using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Drain I";
        public override EffectIconType Icon => EffectIconType.ForceDrain1StatusEffect;

        public ForceDrain1StatusEffect()
        {
        }
    }
}
