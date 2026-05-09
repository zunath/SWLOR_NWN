using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterArmor1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Armor I";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterArmor2StatusEffect),
            typeof(BolsterArmor3StatusEffect),
            typeof(BolsterArmor4StatusEffect),
            typeof(BolsterArmor5StatusEffect)
        };

        public BolsterArmor1StatusEffect()
            : base(StatType.PhysicalDefense, 5)
        {
        }
    }
}
