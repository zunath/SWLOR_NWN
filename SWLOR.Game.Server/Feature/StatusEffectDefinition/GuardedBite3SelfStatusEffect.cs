using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
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
