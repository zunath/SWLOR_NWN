using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingRoar1SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding Roar I";
        public override EffectIconType Icon => EffectIconType.GuardingRoar1SelfStatusEffect;

        public GuardingRoar1SelfStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -6;
        }
    }
}
