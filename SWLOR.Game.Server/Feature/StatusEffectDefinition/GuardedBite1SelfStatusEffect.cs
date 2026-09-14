using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
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
