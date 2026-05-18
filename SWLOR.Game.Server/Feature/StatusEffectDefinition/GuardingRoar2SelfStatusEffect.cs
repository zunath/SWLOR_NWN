using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingRoar2SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding Roar II";
        public override EffectIconType Icon => EffectIconType.GuardingRoar2SelfStatusEffect;

        public GuardingRoar2SelfStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -10;
        }
    }
}
