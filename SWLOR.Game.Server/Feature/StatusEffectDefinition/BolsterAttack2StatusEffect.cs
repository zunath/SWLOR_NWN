using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack2StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack II";
        public override EffectIconType Icon => EffectIconType.BolsterAttack2StatusEffect;

        public BolsterAttack2StatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 8;
            StatGroup.Stats[StatType.BolsterAttackRank] = 2;
        }
    }
}
