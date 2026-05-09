using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterArmor5StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Armor V";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterArmor1StatusEffect),
            typeof(BolsterArmor2StatusEffect),
            typeof(BolsterArmor3StatusEffect),
            typeof(BolsterArmor4StatusEffect)
        };

        public BolsterArmor5StatusEffect()
            : base(StatType.PhysicalDefense, 25)
        {
        }
    }
}
