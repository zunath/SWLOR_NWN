using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DampeningField1StatusEffect : StatusEffectBase
    {
        public override string Name => "Dampening Field I";
        public override EffectIconType Icon => EffectIconType.DampeningField1StatusEffect;

        public DampeningField1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -6;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -6;
        }
    }
}
