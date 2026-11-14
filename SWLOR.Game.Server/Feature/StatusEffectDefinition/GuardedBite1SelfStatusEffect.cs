using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedBite1SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarded Bite I";
        public override EffectIconType Icon => EffectIconType.GuardedBite1SelfStatusEffect;

        public GuardedBite1SelfStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -5;
        }
    }
}
