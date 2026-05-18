using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveChallenge1SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Challenge I";
        public override EffectIconType Icon => EffectIconType.EvasiveChallenge1SelfStatusEffect;

        public EvasiveChallenge1SelfStatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 8;
            StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestoreChance] = 100;
            StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore] = 1;
        }
    }
}
