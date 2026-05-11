using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack3StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Attack III";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterAttack1StatusEffect),
            typeof(BolsterAttack2StatusEffect)
        };

        public BolsterAttack3StatusEffect()
            : base(StatType.Attack, 15)
        {
        }
    }
}
