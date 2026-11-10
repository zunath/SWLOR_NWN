using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell1StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell I";
        public override EffectIconType Icon => EffectIconType.PowerCell1StatusEffect;

        public PowerCell1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 4;
        }
    }
}
