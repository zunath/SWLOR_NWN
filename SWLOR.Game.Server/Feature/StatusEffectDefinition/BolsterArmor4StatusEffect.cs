using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterArmor4StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Bolster Armor IV";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(BolsterArmor5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(BolsterArmor1StatusEffect),
            typeof(BolsterArmor2StatusEffect),
            typeof(BolsterArmor3StatusEffect)
        };

        public BolsterArmor4StatusEffect()
            : base(StatType.PhysicalDefense, 20)
        {
        }
    }
}
