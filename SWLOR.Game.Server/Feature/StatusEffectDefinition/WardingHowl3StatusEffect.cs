using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WardingHowl3StatusEffect : StatusEffectBase
    {
        public override string Name => "Warding Howl III";
        public override EffectIconType Icon => EffectIconType.WardingHowl3StatusEffect;

        public WardingHowl3StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -12;
        }
    }
}
