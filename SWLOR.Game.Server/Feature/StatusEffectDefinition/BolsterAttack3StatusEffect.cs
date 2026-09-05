using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack3StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack III";
        public override EffectIconType Icon => EffectIconType.BolsterAttack3StatusEffect;

        public BolsterAttack3StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(BolsterAttack1StatusEffect));
            LessPowerfulEffectTypes.Add(typeof(BolsterAttack2StatusEffect));
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 12;
        }
    }
}
