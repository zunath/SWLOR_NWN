using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeakenResolve1StatusEffect : StatusEffectBase
    {
        public override string Name => "Weaken Resolve I";
        public override EffectIconType Icon => EffectIconType.WeakenResolve1StatusEffect;

        public WeakenResolve1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = 5;
        }
    }
}
