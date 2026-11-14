using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell3StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell III";
        public override EffectIconType Icon => EffectIconType.PowerCell3StatusEffect;

        public PowerCell3StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 6;
        }
    }
}
