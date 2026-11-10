using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WardingHowl1StatusEffect : StatusEffectBase
    {
        public override string Name => "Warding Howl I";
        public override EffectIconType Icon => EffectIconType.WardingHowl1StatusEffect;

        public WardingHowl1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
        }
    }
}
