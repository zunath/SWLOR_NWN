using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Shielding1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Shielding I";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Shielding2StatusEffect),
            typeof(Shielding3StatusEffect),
            typeof(Shielding4StatusEffect)
        };

        public Shielding1StatusEffect()
            : base(StatType.PhysicalDefense, 5)
        {
        }
    }
}
