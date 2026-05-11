using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Attack I";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterAttack2StatusEffect),
            typeof(BolsterAttack3StatusEffect),
        };

        public BolsterAttack1StatusEffect()
            : base(StatType.Attack, 5)
        {
        }
    }
}
