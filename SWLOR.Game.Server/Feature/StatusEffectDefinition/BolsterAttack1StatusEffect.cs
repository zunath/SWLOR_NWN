using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack1StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack I";
        public override EffectIconType Icon => EffectIconType.BolsterAttack1StatusEffect;

        public BolsterAttack1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 5;
            StatGroup.Stats[StatType.BolsterAttackRank] = 1;
        }
    }
}
