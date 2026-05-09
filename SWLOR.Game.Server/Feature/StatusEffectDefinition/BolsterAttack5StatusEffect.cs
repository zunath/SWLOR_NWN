using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack5StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Attack V";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterAttack1StatusEffect),
            typeof(BolsterAttack2StatusEffect),
            typeof(BolsterAttack3StatusEffect),
            typeof(BolsterAttack4StatusEffect)
        };

        public BolsterAttack5StatusEffect()
            : base(StatType.Attack, 25)
        {
        }
    }
}
