using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveChallenge2SelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Challenge II";
        public override EffectIconType Icon => EffectIconType.EvasiveChallenge2SelfStatusEffect;

        public EvasiveChallenge2SelfStatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 14;
            StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestoreChance] = 100;
            StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore] = 1;
        }
    }
}
