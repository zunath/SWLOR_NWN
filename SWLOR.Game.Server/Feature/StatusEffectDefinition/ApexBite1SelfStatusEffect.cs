using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ApexBite1SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Apex Bite";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override bool PersistsOnLogout => false;

        public ApexBite1SelfStatusEffect()
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 25;
        }
    }
}
