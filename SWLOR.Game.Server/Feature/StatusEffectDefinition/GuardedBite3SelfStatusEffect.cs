using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedBite3SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarded Bite III";
        public override EffectIconType Icon => EffectIconType.GuardedBite3SelfStatusEffect;

        public GuardedBite3SelfStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -12;
        }
    }
}
