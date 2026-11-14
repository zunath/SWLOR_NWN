using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingRoar3SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding Roar III";
        public override EffectIconType Icon => EffectIconType.GuardingRoar3SelfStatusEffect;

        public GuardingRoar3SelfStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -15;
        }
    }
}
