using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Shielding3StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Shielding III";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Shielding4StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Shielding1StatusEffect),
            typeof(Shielding2StatusEffect)
        };

        public Shielding3StatusEffect()
            : base(StatType.PhysicalDefense, 15)
        {
        }
    }
}
