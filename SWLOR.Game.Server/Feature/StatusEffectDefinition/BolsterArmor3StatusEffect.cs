using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterArmor3StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Armor III";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterArmor4StatusEffect),
            typeof(BolsterArmor5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterArmor1StatusEffect),
            typeof(BolsterArmor2StatusEffect)
        };

        public BolsterArmor3StatusEffect()
            : base(StatType.PhysicalDefense, 15)
        {
        }
    }
}
