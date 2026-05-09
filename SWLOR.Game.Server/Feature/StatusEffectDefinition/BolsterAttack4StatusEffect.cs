using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack4StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Attack IV";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterAttack5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterAttack1StatusEffect),
            typeof(BolsterAttack2StatusEffect),
            typeof(BolsterAttack3StatusEffect)
        };

        public BolsterAttack4StatusEffect()
            : base(StatType.Attack, 20)
        {
        }
    }
}
