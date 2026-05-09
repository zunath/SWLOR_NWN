using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack2StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Attack II";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterAttack3StatusEffect),
            typeof(BolsterAttack4StatusEffect),
            typeof(BolsterAttack5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterAttack1StatusEffect)
        };

        public BolsterAttack2StatusEffect()
            : base(StatType.Attack, 10)
        {
        }
    }
}
